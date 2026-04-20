using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TodoApp.Core.Models;
using TodoApp.Data;

namespace TodoApp.Services;

/// <summary>
/// Creates and restores zip backups that include all three data files:
/// todos.json, stickynotes.json, settings.json.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IAppDataStore         _store;
    private readonly ILogger<BackupService> _logger;

    private static readonly string[] DataFiles =
    {
        DataPaths.TodosFile,
        DataPaths.StickyNotesFile,
        DataPaths.WatchLaterFile,
        DataPaths.LabelsFile,
        DataPaths.GoalsFile,
        DataPaths.SettingsFile
    };

    public BackupService(IAppDataStore store, ILogger<BackupService> logger)
    {
        _store  = store;
        _logger = logger;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<string> CreateBackupAsync()
    {
        var settings = await _store.GetSettingsAsync();

        var dir = string.IsNullOrWhiteSpace(settings.Backup.BackupDirectory)
            ? DataPaths.BackupsDirectory
            : settings.Backup.BackupDirectory;

        Directory.CreateDirectory(dir);

        var zipPath = Path.Combine(dir, $"todoapp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        await Task.Run(() =>
        {
            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var file in DataFiles)
            {
                if (File.Exists(file))
                    zip.CreateEntryFromFile(file, Path.GetFileName(file),
                        CompressionLevel.Optimal);
            }
        });

        _logger.LogInformation("Backup created: {Path}", zipPath);

        await PruneBackupsAsync(settings.Backup.MaxBackupFiles, dir);

        return zipPath;
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    public async Task RestoreBackupAsync(string zipPath)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException("Backup file not found.", zipPath);

        await Task.Run(() =>
        {
            using var zip = ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                var dest = Path.Combine(DataPaths.AppDataRoot, entry.Name);
                // Backup the current file before overwriting
                if (File.Exists(dest))
                    File.Copy(dest, dest + ".pre-restore", overwrite: true);

                entry.ExtractToFile(dest, overwrite: true);
            }
        });

        _logger.LogInformation("Restore complete from: {Path}", zipPath);
    }

    // ── Export / Import ───────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        Converters             = { new JsonStringEnumConverter() },
        AllowTrailingCommas    = true,
        ReadCommentHandling    = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> ExportTasksAsync(string filePath)
    {
        var todos = await _store.GetTodosAsync();
        var json  = JsonSerializer.Serialize(todos, _jsonOpts);

        await File.WriteAllTextAsync(filePath, json);
        _logger.LogInformation("Exported {Count} tasks to {Path}", todos.Count, filePath);
        return filePath;
    }

    public async Task<int> ImportTasksAsync(string filePath, bool merge = true)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Import file not found.", filePath);

        var json    = await File.ReadAllTextAsync(filePath);
        var imported = JsonSerializer.Deserialize<List<TodoItem>>(json, _jsonOpts)
                       ?? new List<TodoItem>();

        if (merge)
        {
            var existing = await _store.GetTodosAsync();
            var existingIds = new HashSet<Guid>(existing.Select(t => t.Id));
            var newOnes = imported.Where(t => !existingIds.Contains(t.Id)).ToList();
            existing.AddRange(newOnes);
            await _store.SaveTodosAsync(existing);
            _logger.LogInformation("Merged {Count} new tasks from {Path}", newOnes.Count, filePath);
            return newOnes.Count;
        }
        else
        {
            await _store.SaveTodosAsync(imported);
            _logger.LogInformation("Replaced tasks with {Count} imported from {Path}", imported.Count, filePath);
            return imported.Count;
        }
    }

    // ── Prune ─────────────────────────────────────────────────────────────────

    public Task PruneBackupsAsync(int max) =>
        PruneBackupsAsync(max, DataPaths.BackupsDirectory);

    private Task PruneBackupsAsync(int max, string dir)
    {
        if (max <= 0) return Task.CompletedTask;

        return Task.Run(() =>
        {
            var files = Directory.GetFiles(dir, "todoapp_backup_*.zip")
                .OrderByDescending(f => f)
                .Skip(max)
                .ToList();

            foreach (var old in files)
            {
                try   { File.Delete(old); _logger.LogDebug("Pruned backup: {F}", old); }
                catch { /* non-fatal */ }
            }
        });
    }
}
