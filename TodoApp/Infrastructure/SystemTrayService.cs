using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Logging;

namespace TodoApp.Infrastructure;

/// <summary>
/// Manages the system-tray icon lifecycle.
/// Must be created and disposed on the UI (dispatcher) thread.
/// </summary>
public sealed class SystemTrayService : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly ILogger<SystemTrayService> _logger;

    public event EventHandler? ShowRequested;
    public event EventHandler? QuickAddRequested;

    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger;
    }

    /// <summary>Call once on the UI thread after the main window has loaded.</summary>
    public void Initialize()
    {
        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText      = "TodoApp — Personal Task Manager",
                Icon             = CreateAppIcon(),
                ContextMenu      = BuildContextMenu(),
            };

            // Single left-click (not double) to show the window
            _trayIcon.TrayLeftMouseUp       += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
            _trayIcon.TrayBalloonTipClicked += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("System tray icon initialised.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create system tray icon (may be running headless).");
        }
    }

    public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        try { _trayIcon?.ShowBalloonTip(title, message, icon); }
        catch { /* ignore if tray not available */ }
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ContextMenu BuildContextMenu()
    {
        var showItem = new MenuItem { Header = "📋  Show TodoApp" };
        showItem.Click += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);

        var quickItem = new MenuItem { Header = "➕  Quick Add Task" };
        quickItem.Click += (_, _) => QuickAddRequested?.Invoke(this, EventArgs.Empty);

        var exitItem = new MenuItem { Header = "✕  Exit" };
        exitItem.Click += (_, _) => System.Windows.Application.Current.Dispatcher.Invoke(
            () => System.Windows.Application.Current.Shutdown());

        var menu = new ContextMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(quickItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        return menu;
    }

    /// <summary>
    /// Load the app icon from embedded PNG resource, falling back to a
    /// generated pink-circle icon if the resource is unavailable.
    /// </summary>
    private static Icon CreateAppIcon()
    {
        try
        {
            var uri        = new Uri("pack://application:,,,/Assets/Icons/Icon-remove-bg.png", UriKind.Absolute);
            var streamInfo = System.Windows.Application.GetResourceStream(uri);
            if (streamInfo?.Stream is not null)
            {
                using var bmp   = new Bitmap(streamInfo.Stream);
                using var sized = new Bitmap(bmp, 32, 32);
                var hIcon = sized.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
        catch { /* fall through to generated icon */ }

        return CreatePinkIcon();
    }

    /// <summary>
    /// Programmatically generate a small pink circle icon as fallback.
    /// </summary>
    private static Icon CreatePinkIcon()
    {
        try
        {
            using var bmp = new Bitmap(32, 32);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var fill = new SolidBrush(Color.FromArgb(233, 30, 99));
            g.FillEllipse(fill, 2, 2, 28, 28);

            using var pen = new Pen(Color.White, 3f) { LineJoin = System.Drawing.Drawing2D.LineJoin.Round };
            g.DrawLines(pen, new PointF[]
            {
                new(9, 16),
                new(14, 22),
                new(23, 10)
            });

            var hicon = bmp.GetHicon();
            return Icon.FromHandle(hicon);
        }
        catch
        {
            return SystemIcons.Application;
        }
    }
}
