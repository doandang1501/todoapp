using TodoApp.Core.Enums;

namespace TodoApp.Core.Models.Settings;

public class SoundSettings
{
    public bool  Enabled { get; set; } = true;

    /// <summary>Master volume 0.0 – 1.0.</summary>
    public float Volume  { get; set; } = 0.7f;

    // ── Per-priority custom sounds ────────────────────────────────────────────
    /// <summary>
    /// Absolute path to an mp3/wav file for each priority.
    /// Null means "use the app default".
    /// </summary>
    public Dictionary<Priority, string?> PrioritySounds { get; set; } = new()
    {
        { Priority.Low,      null },
        { Priority.Medium,   null },
        { Priority.High,     null },
        { Priority.Critical, null }
    };

    // ── Completion sound ─────────────────────────────────────────────────────
    public bool   PlayCompletionSound    { get; set; } = true;
    public string? CompletionSoundPath   { get; set; } = null; // null = bundled default

    // ── Bundled default paths (relative to executable) ───────────────────────
    public string DefaultNotificationSound { get; set; } = "Assets/Sounds/notification.wav";
    public string DefaultCompletionSound   { get; set; } = "Assets/Sounds/complete.wav";
}
