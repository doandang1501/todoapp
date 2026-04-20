namespace TodoApp.Core.Models.Settings;

public class CleanupSettings
{
    public bool AutoCleanupEnabled        { get; set; } = false;

    /// <summary>Delete Done tasks older than this many days. 0 = disabled.</summary>
    public int  DeleteDoneAfterDays       { get; set; } = 30;

    /// <summary>Delete Archived tasks older than this many days. 0 = disabled.</summary>
    public int  DeleteArchivedAfterDays   { get; set; } = 7;

    /// <summary>Show confirmation dialog before auto-delete runs.</summary>
    public bool PromptBeforeDelete        { get; set; } = true;
}
