using System.IO;
namespace TodoApp.Data;

/// <summary>
/// Central registry of every file path the app reads and writes.
/// All data lives under %AppData%/TodoApp/ as required.
/// Call <see cref="EnsureDirectoriesExist"/> once at startup before any I/O.
/// </summary>
public static class DataPaths
{
    // ── Root ─────────────────────────────────────────────────────────────────
    public static string AppDataRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoApp");

    // ── Data files ───────────────────────────────────────────────────────────
    public static string TodosFile       => Path.Combine(AppDataRoot, "todos.json");
    public static string StickyNotesFile => Path.Combine(AppDataRoot, "stickynotes.json");
    public static string WatchLaterFile  => Path.Combine(AppDataRoot, "watchlater.json");
    public static string LabelsFile      => Path.Combine(AppDataRoot, "labels.json");
    public static string SettingsFile    => Path.Combine(AppDataRoot, "settings.json");
    public static string GoalsFile       => Path.Combine(AppDataRoot, "goals.json");

    // ── Sub-directories ───────────────────────────────────────────────────────
    public static string BackupsDirectory => Path.Combine(AppDataRoot, "Backups");
    public static string LogsDirectory    => Path.Combine(AppDataRoot, "Logs");

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates all required directories if they do not exist.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(BackupsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }

    /// <summary>Generates a timestamped backup filename.</summary>
    public static string GenerateBackupFileName(string prefix = "backup") =>
        Path.Combine(BackupsDirectory, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
}
