using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Core.Models.Settings;

namespace TodoApp.Services;

/// <summary>
/// Sends notification emails via Brevo (sendinblue) API or SMTP fallback.
/// </summary>
public sealed class EmailService : IEmailService, IDisposable
{
    private readonly HttpClient               _http;
    private readonly ILogger<EmailService>    _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _http   = new HttpClient();
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<bool> SendNotificationAsync(
        TodoItem task,
        NotificationEventArgs args,
        EmailSettings settings)
    {
        if (!settings.Enabled) return false;

        var subject = $"[TodoApp] Reminder: {task.Title}";
        var body    = BuildNotificationBody(task, args);

        return await SendAsync(settings, subject, body);
    }

    public async Task<bool> SendTestEmailAsync(EmailSettings settings)
    {
        var subject = "[TodoApp] Test email — configuration OK";
        var body    = "<h2>It works!</h2><p>Your TodoApp email settings are configured correctly.</p>";
        return await SendAsync(settings, subject, body);
    }

    // ── Routing ───────────────────────────────────────────────────────────────

    private async Task<bool> SendAsync(EmailSettings cfg, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(cfg.RecipientEmail)) return false;

        return cfg.Provider == "Brevo"
            ? await SendBrevoAsync(cfg, subject, htmlBody)
            : await SendSmtpAsync(cfg, subject, htmlBody);
    }

    // ── Brevo ─────────────────────────────────────────────────────────────────

    private async Task<bool> SendBrevoAsync(EmailSettings cfg, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(cfg.BrevoApiKey))
        {
            _logger.LogWarning("Brevo API key is empty.");
            return false;
        }

        var payload = new
        {
            sender      = new { name  = cfg.SenderName,    email = cfg.SenderEmail },
            to          = new[] { new { name = cfg.RecipientName, email = cfg.RecipientEmail } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", cfg.BrevoApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Brevo email sent to {Email}", cfg.RecipientEmail);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Brevo returned {Code}: {Error}", (int)response.StatusCode, err);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo send failed.");
            return false;
        }
    }

    // ── SMTP fallback ─────────────────────────────────────────────────────────

    private async Task<bool> SendSmtpAsync(EmailSettings cfg, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(cfg.SmtpHost))
        {
            _logger.LogWarning("SMTP host is empty.");
            return false;
        }

        try
        {
            using var client = new SmtpClient(cfg.SmtpHost, cfg.SmtpPort)
            {
                EnableSsl   = cfg.SmtpUseSsl,
                Credentials = new NetworkCredential(cfg.SmtpUser, cfg.SmtpPassword),
                Timeout     = 15_000
            };

            var msg = new MailMessage(
                new MailAddress(cfg.SenderEmail, cfg.SenderName),
                new MailAddress(cfg.RecipientEmail, cfg.RecipientName))
            {
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };

            await client.SendMailAsync(msg);
            _logger.LogInformation("SMTP email sent to {Email}", cfg.RecipientEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed.");
            return false;
        }
    }

    // ── Body builder ──────────────────────────────────────────────────────────

    private static string BuildNotificationBody(TodoItem task, NotificationEventArgs args)
    {
        var timingText = args.Timing switch
        {
            Core.Enums.NotificationTiming.OneDayBefore      => "is due <strong>tomorrow</strong>",
            Core.Enums.NotificationTiming.OneHourBefore     => "is due in <strong>1 hour</strong>",
            Core.Enums.NotificationTiming.FiveMinutesBefore => "is due in <strong>5 minutes</strong>",
            Core.Enums.NotificationTiming.AtDeadline        => "<strong>deadline reached</strong>",
            Core.Enums.NotificationTiming.OneDayAfter       => "is <strong>1 day overdue</strong>",
            _                                               => "has a reminder"
        };

        var deadline = task.Deadline.HasValue
            ? $"<p><strong>Deadline:</strong> {task.Deadline.Value:dddd, d MMMM yyyy, HH:mm}</p>"
            : "";

        var desc = !string.IsNullOrWhiteSpace(task.Description)
            ? $"<p style='color:#555'>{System.Net.WebUtility.HtmlEncode(task.Description)}</p>"
            : "";

        return $"""
            <div style="font-family:sans-serif;max-width:480px">
                <h2 style="color:#E91E63">TodoApp Reminder</h2>
                <p>Your task <strong>{System.Net.WebUtility.HtmlEncode(task.Title)}</strong> {timingText}.</p>
                {deadline}
                {desc}
                <p style="color:#9e9e9e;font-size:12px">
                    Priority: {task.Priority} &bull; Sent by TodoApp
                </p>
            </div>
            """;
    }

    public void Dispose() => _http.Dispose();
}
