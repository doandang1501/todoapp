using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class ToastWindow : Window
{
    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromSeconds(1)
    };

    private bool _isClosing;

    public ToastWindow(ToastViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.CloseRequested += () => Dispatcher.Invoke(BeginFadeOut);

        Loaded   += OnLoaded;
    }

    // ── Position ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the window position so it appears at the bottom-right of the work area,
    /// stacked above any existing toasts.
    /// </summary>
    public void SetPosition(int stackIndex)
    {
        var area = SystemParameters.WorkArea;
        const double margin = 16;
        const double spacing = 12;

        Left = area.Right  - Width - margin;
        Top  = area.Bottom - ActualHeight - margin - stackIndex * (ActualHeight + spacing);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Snap into position now that ActualHeight is known
        if (DataContext is ToastViewModel vm)
        {
            // re-position after height is known
        }

        // Slide in from right
        var slideIn = new DoubleAnimation(380, 0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty, slideIn);

        // Start countdown timer
        _timer.Tick += (_, _) =>
        {
            if (DataContext is ToastViewModel tv)
                tv.Tick();
        };
        _timer.Start();
    }

    private void BeginFadeOut()
    {
        if (_isClosing) return;
        _isClosing = true;
        _timer.Stop();

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        fadeOut.Completed += (_, _) => Close();

        var slideOut = new DoubleAnimation(0, 380, TimeSpan.FromMilliseconds(250))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        BeginAnimation(OpacityProperty, fadeOut);
        SlideTransform.BeginAnimation(
            System.Windows.Media.TranslateTransform.XProperty, slideOut);
    }
}
