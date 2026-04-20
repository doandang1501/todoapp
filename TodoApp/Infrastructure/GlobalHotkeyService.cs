using System.Windows.Input;
using Microsoft.Extensions.Logging;
using NHotkey;
using NHotkey.Wpf;

namespace TodoApp.Infrastructure;

/// <summary>
/// Registers global hotkeys via NHotkey.Wpf.
/// Must be initialised on the WPF UI thread.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private readonly ILogger<GlobalHotkeyService> _logger;
    private bool _registered;

    /// <summary>Fired when Ctrl+Alt+T is pressed anywhere.</summary>
    public event EventHandler? ShowWindowRequested;

    /// <summary>Fired when Ctrl+Alt+N is pressed anywhere (Quick Add).</summary>
    public event EventHandler? QuickAddRequested;

    /// <summary>Fired when Ctrl+Alt+Q is pressed anywhere (Quick Note).</summary>
    public event EventHandler? QuickNoteRequested;

    public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger)
    {
        _logger = logger;
    }

    /// <summary>Register all hotkeys.  Call once from the UI thread.</summary>
    public void Register()
    {
        if (_registered) return;
        try
        {
            HotkeyManager.Current.AddOrReplace(
                "ShowWindow",
                Key.T,
                ModifierKeys.Control | ModifierKeys.Alt,
                OnShowWindow);

            HotkeyManager.Current.AddOrReplace(
                "QuickAdd",
                Key.N,
                ModifierKeys.Control | ModifierKeys.Alt,
                OnQuickAdd);

            HotkeyManager.Current.AddOrReplace(
                "QuickNote",
                Key.Q,
                ModifierKeys.Control | ModifierKeys.Alt,
                OnQuickNote);

            _registered = true;
            _logger.LogInformation("Global hotkeys registered (Ctrl+Alt+T / Ctrl+Alt+N / Ctrl+Alt+Q).");
        }
        catch (Exception ex)
        {
            // Hotkey might already be taken by another app — non-fatal
            _logger.LogWarning(ex, "Failed to register global hotkeys.");
        }
    }

    public void Dispose()
    {
        if (!_registered) return;
        try
        {
            HotkeyManager.Current.Remove("ShowWindow");
            HotkeyManager.Current.Remove("QuickAdd");
            HotkeyManager.Current.Remove("QuickNote");
        }
        catch { }
    }

    private void OnShowWindow(object? sender, HotkeyEventArgs e)
    {
        e.Handled = true;
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnQuickAdd(object? sender, HotkeyEventArgs e)
    {
        e.Handled = true;
        QuickAddRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnQuickNote(object? sender, HotkeyEventArgs e)
    {
        e.Handled = true;
        QuickNoteRequested?.Invoke(this, EventArgs.Empty);
    }
}
