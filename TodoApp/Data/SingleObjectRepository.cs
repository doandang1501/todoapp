using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TodoApp.Data;

/// <summary>
/// Thread-safe JSON repository for a single serialised object (e.g. AppSettings).
/// Returns a fresh default instance when the file does not exist yet.
/// </summary>
public sealed class SingleObjectRepository<T> where T : class, new()
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

    public SingleObjectRepository(string filePath, ILogger logger)
    {
        _filePath = filePath;
        _logger   = logger;
    }

    public async Task<T> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return new T();

            var json = await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(json))
                return new T();

            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {File}", _filePath);
            return new T();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(T data, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var json    = JsonSerializer.Serialize(data, JsonOptions);
            var tmpPath = _filePath + ".tmp";

            await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);

            if (File.Exists(_filePath)) File.Delete(_filePath);
            File.Move(tmpPath, _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save {File}", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
