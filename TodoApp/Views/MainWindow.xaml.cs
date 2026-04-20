using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Infrastructure;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel        _vm;
    private readonly SystemTrayService    _tray;
    private readonly GlobalHotkeyService  _hotkey;
    private readonly ToastService         _toast;
    private readonly IServiceProvider     _services;

    public MainWindow(
        MainViewModel       vm,
        SystemTrayService   tray,
        GlobalHotkeyService hotkey,
        ToastService        toast,
        IServiceProvider    services)
    {
        InitializeComponent();
        _vm       = vm;
        _tray     = tray;
        _hotkey   = hotkey;
        _toast    = toast;   // kept alive for the app lifetime; subscribes in ctor
        _services = services;

        DataContext = vm;

        Loaded       += OnLoaded;
        Closing      += OnClosing;
        StateChanged += OnStateChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _vm.InitializeAsync();

        // Restore window geometry
        var s = _vm.Settings;
        if (s.MainWindowWidth > 400 && s.MainWindowHeight > 300)
        {
            Width  = s.MainWindowWidth;
            Height = s.MainWindowHeight;
        }
        if (s.MainWindowLeft > 0 || s.MainWindowTop > 0)
        {
            Left = s.MainWindowLeft;
            Top  = s.MainWindowTop;
        }
        if (s.MainWindowMaximized)
            WindowState = WindowState.Maximized;

        // System tray
        _tray.Initialize();
        _tray.ShowRequested     += (_, _) => RestoreWindow();
        _tray.QuickAddRequested += (_, _) => Dispatcher.Invoke(OpenQuickAddWindow);

        // Global hotkeys
        _hotkey.Register();
        _hotkey.ShowWindowRequested += (_, _) => Dispatcher.Invoke(RestoreWindow);
        _hotkey.QuickAddRequested   += (_, _) => Dispatcher.Invoke(OpenQuickAddWindow);
        _hotkey.QuickNoteRequested  += (_, _) => Dispatcher.Invoke(OpenQuickNoteWindow);
    }

    private void OpenQuickAddWindow()
    {
        // If a QuickAdd window is already open, just activate it
        foreach (Window w in Application.Current.Windows)
        {
            if (w is QuickAddWindow existing)
            {
                existing.Activate();
                return;
            }
        }

        var todoService = _services.GetRequiredService<ITodoService>();
        var win = new QuickAddWindow(todoService) { Owner = this };
        win.Show();
    }

    private void OpenQuickNoteWindow()
    {
        // If a QuickNote window is already open, activate it
        foreach (Window w in Application.Current.Windows)
        {
            if (w is QuickNoteWindow existing)
            {
                existing.Activate();
                return;
            }
        }

        var vm  = _services.GetRequiredService<QuickNoteViewModel>();
        var win = new QuickNoteWindow(vm);
        win.Show();
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Close button always performs a real shutdown — save geometry first
        var s = _vm.Settings;
        s.MainWindowMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal)
        {
            s.MainWindowLeft   = Left;
            s.MainWindowTop    = Top;
            s.MainWindowWidth  = Width;
            s.MainWindowHeight = Height;
        }
        await _vm.SaveSettingsAsync();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Minimize → hide to tray (only when MinimizeToTray is enabled)
        if (WindowState == WindowState.Minimized && _vm.Settings.MinimizeToTray)
        {
            Hide();
            _tray.ShowBalloon("TodoApp", "Ứng dụng vẫn đang chạy trong khay hệ thống. Nhấn vào biểu tượng để mở lại.");
        }
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    // ── Custom title bar button handlers ──────────────────────────────────────

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();
}
