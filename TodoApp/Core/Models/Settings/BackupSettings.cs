namespace TodoApp.Core.Models.Settings;

public class BackupSettings
{
    public bool   AutoBackupEnabled       { get; set; } = true;

    /// <summary>Run automatic backup every N hours.</summary>
    public int    AutoBackupIntervalHours { get; set; } = 24;

    /// <summary>Keep at most this many backup files; oldest are pruned.</summary>
    public int    MaxBackupFiles          { get; set; } = 10;

    /// <summary>
    /// Absolute path for backup output.
    /// Empty string = default: %AppData%/TodoApp/Backups/
    /// </summary>
    public string BackupDirectory         { get; set; } = string.Empty;

    /// <summary>Create a backup automatically when the app closes.</summary>
    public bool   BackupOnExit            { get; set; } = false;
}
