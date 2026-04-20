namespace TodoApp.Services;

public interface IBackupService
{
    /// <summary>Create a zip backup of all data files. Returns the path of the created file.</summary>
    Task<string> CreateBackupAsync();

    /// <summary>Restore data from a previously created zip backup.</summary>
    Task RestoreBackupAsync(string zipPath);

    /// <summary>Delete oldest backup files, keeping at most <paramref name="max"/>.</summary>
    Task PruneBackupsAsync(int max);

    /// <summary>Export all tasks to a JSON file. Returns the path written.</summary>
    Task<string> ExportTasksAsync(string filePath);

    /// <summary>
    /// Import tasks from a JSON file.
    /// If <paramref name="merge"/> is true, imported tasks are merged with existing ones (duplicates by Id are skipped).
    /// If false, existing tasks are replaced.
    /// Returns the number of tasks imported.
    /// </summary>
    Task<int> ImportTasksAsync(string filePath, bool merge = true);
}
