namespace TodoApp.Core.Models.Settings;

public class FocusModeSettings
{
    /// <summary>Persisted: was focus mode active when the app last closed?</summary>
    public bool IsActive { get; set; } = false;

    public int   DefaultDurationMinutes   { get; set; } = 25;
    public int[] DurationOptions          { get; set; } = { 15, 25, 45, 60, 90, 120 };

    public bool SuppressToastNotifications { get; set; } = true;
    public bool SuppressEmailNotifications { get; set; } = true;
    public bool SuppressSounds             { get; set; } = false;

    /// <summary>Play an end-of-session sound when focus period expires.</summary>
    public bool PlayEndSound               { get; set; } = true;
}
