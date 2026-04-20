using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Enums;
using TodoApp.Data;

namespace TodoApp.Services;

public sealed class AIService : IAIService
{
    private readonly IAppDataStore      _store;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AIService> _logger;

    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    public AIService(IAppDataStore store, IHttpClientFactory httpFactory, ILogger<AIService> logger)
    {
        _store       = store;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        var s = await _store.GetSettingsAsync();
        return s.AI.Enabled && !string.IsNullOrWhiteSpace(s.AI.GeminiApiKey);
    }

    // ── SuggestTags ───────────────────────────────────────────────────────────

    public async Task<AITagSuggestion> SuggestTagsAsync(
        string title, string description, CancellationToken ct = default)
    {
        try
        {
            var settings = await _store.GetSettingsAsync();
            if (!settings.AI.Enabled || string.IsNullOrWhiteSpace(settings.AI.GeminiApiKey))
                return new AITagSuggestion([], false, "AI chưa được cấu hình.");

            var desc   = string.IsNullOrWhiteSpace(description) ? "(không có)" : description;
            var prompt = $$"""
                Bạn là trợ lý quản lý công việc. Phân tích công việc sau và gợi ý 3-5 thẻ tag phù hợp.
                Tên công việc: {{title}}
                Mô tả: {{desc}}

                Trả về JSON (không có markdown, không có giải thích):
                {"tags":["tag1","tag2","tag3"]}

                Tag ngắn gọn (1-2 từ), chữ thường, cùng ngôn ngữ với nội dung công việc.
                """;

            var json = await CallGeminiAsync(prompt, settings.AI, ct);
            if (json is null) return new AITagSuggestion([], false, "Không nhận được phản hồi từ AI.");

            var node = JsonNode.Parse(json);
            var tags = node?["tags"]?.AsArray()
                           .Select(n => n?.GetValue<string>() ?? "")
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .ToList() ?? [];

            return new AITagSuggestion(tags, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI tag suggestion failed for title={Title}", title);
            return new AITagSuggestion([], false, ex.Message);
        }
    }

    // ── ParseTask ─────────────────────────────────────────────────────────────

    public async Task<AITaskParseResult> ParseTaskAsync(string input, CancellationToken ct = default)
    {
        try
        {
            var settings = await _store.GetSettingsAsync();
            if (!settings.AI.Enabled || string.IsNullOrWhiteSpace(settings.AI.GeminiApiKey))
                return new AITaskParseResult(input, null, null, [], false, "AI chưa được cấu hình.");

            var now       = DateTime.Now;
            var viCulture = new CultureInfo("vi-VN");
            var dayOfWeek = now.ToString("dddd", viCulture);

            var prompt = $$"""
                Bạn là trợ lý quản lý công việc. Phân tích câu đầu vào và trả về thông tin công việc có cấu trúc.
                Đầu vào: "{{input}}"
                Ngày giờ hiện tại: {{now:yyyy-MM-dd HH:mm}} ({{dayOfWeek}})

                Trả về JSON (không có markdown, không có giải thích):
                {"title":"tên công việc","deadline":"2025-04-18T17:00:00","priority":"Medium","tags":["tag1","tag2"]}

                Quy tắc:
                - title: tên công việc chính đã được làm sạch, bỏ thông tin thời gian
                - deadline: ISO 8601 hoặc null nếu không có thời gian
                  · "ngày mai" → ngày tiếp theo
                  · "hôm nay" / "hôm nay lúc X" → hôm nay
                  · "X giờ sáng/chiều/tối" → giờ tương ứng (chiều/tối +12 nếu < 12)
                  · "X giờ" không có buổi → nếu X <= 12 và hiện tại >= X thì là X+12:00, ngược lại X:00
                  · "thứ X" → ngày thứ X tiếp theo trong tuần (từ ngày mai trở đi)
                  · "tuần sau" → 7 ngày từ hôm nay, "tháng sau" → 30 ngày
                - priority: "Low"|"Medium"|"High"|"Critical" hoặc null
                  · gấp/khẩn/urgent/ngay/quan trọng → "High"
                  · bình thường/không gấp → "Low"
                  · mặc định nếu không rõ → null
                - tags: 2-4 tag ngắn cùng ngôn ngữ với đầu vào
                """;

            var json = await CallGeminiAsync(prompt, settings.AI, ct);
            if (json is null)
                return new AITaskParseResult(input, null, null, [], false, "Không nhận được phản hồi từ AI.");

            var node = JsonNode.Parse(json);
            if (node is null)
                return new AITaskParseResult(input, null, null, [], false, "Phản hồi AI không hợp lệ.");

            var parsedTitle = node["title"]?.GetValue<string>() ?? input;

            DateTime? deadline      = null;
            var       deadlineRaw   = node["deadline"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(deadlineRaw) && deadlineRaw != "null"
                && DateTime.TryParse(deadlineRaw, out var dt))
                deadline = dt;

            Priority? priority     = null;
            var       priorityRaw  = node["priority"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(priorityRaw) && priorityRaw != "null"
                && Enum.TryParse<Priority>(priorityRaw, ignoreCase: true, out var p))
                priority = p;

            var tags = node["tags"]?.AsArray()
                           .Select(n => n?.GetValue<string>() ?? "")
                           .Where(t => !string.IsNullOrWhiteSpace(t))
                           .ToList() ?? [];

            return new AITaskParseResult(parsedTitle, deadline, priority, tags, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI task parse failed for input={Input}", input);
            return new AITaskParseResult(input, null, null, [], false, ex.Message);
        }
    }

    // ── Core HTTP call ────────────────────────────────────────────────────────

    private async Task<string?> CallGeminiAsync(
        string prompt, Core.Models.Settings.AISettings ai, CancellationToken ct)
    {
        var url = $"{ApiBase}/{ai.ModelName}:generateContent?key={ai.GeminiApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature      = 0.1,
                maxOutputTokens  = 512,
                responseMimeType = "application/json"
            }
        };

        var bodyJson = JsonSerializer.Serialize(requestBody);
        using var http    = _httpFactory.CreateClient("Gemini");
        using var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        var responseStr = await response.Content.ReadAsStringAsync(ct);
        var doc         = JsonDocument.Parse(responseStr);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text is null ? null : StripMarkdownJson(text);
    }

    private static string StripMarkdownJson(string text)
    {
        var s = text.Trim();
        if (!s.StartsWith("```")) return s;
        var start = s.IndexOf('\n') + 1;
        var end   = s.LastIndexOf("```");
        return end > start ? s[start..end].Trim() : s;
    }
}
