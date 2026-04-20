using TodoApp.Core.Enums;

namespace TodoApp.Services;

public interface ISoundService
{
    /// <summary>Play a file (mp3/wav). Silently ignored if file missing or sound disabled.</summary>
    Task PlayFileAsync(string filePath, float volume = 1f);

    /// <summary>Play the configured priority-specific sound, falling back to system beep.</summary>
    Task PlayPriorityAsync(Priority priority);

    /// <summary>Play the task-completion jingle.</summary>
    Task PlayCompletionAsync();

    /// <summary>Stop all currently playing sounds.</summary>
    void StopAll();

    bool IsEnabled { get; }
}
