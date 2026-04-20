using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TodoApp.Data;

/// <summary>
/// Generic, thread-safe JSON repository for a list of items.
/// Uses an atomic write pattern (temp file + rename) to prevent data corruption
/// if the process is killed mid-write.
/// </summary>
public sealed class JsonRepository<T> where T : class
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented             = true,
        PropertyNamingPolicy      = JsonNamingPolicy.CamelCase,
        Converters                = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition    = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas       = true,
        ReadCommentHandling       = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true
    };

    public JsonRepository(string filePath, ILogger logger)
    {
        _filePath = filePath;
        _logger   = logger;
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return new List<T>();

            var json = await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions)
                   ?? new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read {File}", _filePath);
            return new List<T>();
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Write ────────────────────────────────────────────────────────────────

    public async Task SaveAllAsync(IEnumerable<T> items, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json    = JsonSerializer.Serialize(items, JsonOptions);
            var tmpPath = _filePath + ".tmp";

            await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);

            // Atomic replace
            if (File.Exists(_filePath)) File.Delete(_filePath);
            File.Move(tmpPath, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write {File}", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
