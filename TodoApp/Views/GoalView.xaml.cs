using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class GoalView : UserControl
{
    private GoalViewModel? _vm;

    public GoalView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null) _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = DataContext as GoalViewModel;
        if (_vm != null) _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GoalViewModel.ShowCelebration) && _vm?.ShowCelebration == true)
            Dispatcher.Invoke(LaunchConfetti);
    }

    private void LaunchConfetti()
    {
        ConfettiCanvas.Children.Clear();

        var rng = new Random();
        Color[] palette =
        [
            Color.FromRgb(233, 30,  99),   // pink
            Color.FromRgb(33,  150, 243),  // blue
            Color.FromRgb(76,  175, 80),   // green
            Color.FromRgb(255, 193, 7),    // amber
            Color.FromRgb(156, 39,  176),  // purple
            Color.FromRgb(255, 87,  34),   // orange
            Color.FromRgb(0,   188, 212),  // cyan
            Color.FromRgb(255, 235, 59),   // yellow
        ];

        double w = Math.Max(600, ActualWidth);
        double h = Math.Max(500, ActualHeight);

        for (int i = 0; i < 70; i++)
        {
            var color = palette[rng.Next(palette.Length)];
            FrameworkElement piece = rng.Next(3) switch
            {
                0 => new Rectangle { Width = rng.Next(7, 15), Height = rng.Next(4, 10), Fill = new SolidColorBrush(color) },
                1 => new Ellipse   { Width = rng.Next(6, 12), Height = rng.Next(6, 12), Fill = new SolidColorBrush(color) },
                _ => new Rectangle { Width = rng.Next(4, 8),  Height = rng.Next(4, 8),  Fill = new SolidColorBrush(color) },
            };

            piece.Opacity = 0.92;
            piece.RenderTransformOrigin = new Point(0.5, 0.5);
            piece.RenderTransform = new RotateTransform(rng.NextDouble() * 360);

            double startX = rng.NextDouble() * w;
            double startY = -25 - rng.NextDouble() * 120;
            double endX   = startX + (rng.NextDouble() - 0.5) * 320;
            double endY   = h + 30;
            double delay  = rng.NextDouble() * 0.8;
            double dur    = 2.2 + rng.NextDouble() * 1.8;

            Canvas.SetLeft(piece, startX);
            Canvas.SetTop(piece, startY);
            ConfettiCanvas.Children.Add(piece);

            var fall = new DoubleAnimation(startY, endY, new Duration(TimeSpan.FromSeconds(dur)))
            {
                BeginTime = TimeSpan.FromSeconds(delay),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
            };
            var drift = new DoubleAnimation(startX, endX, new Duration(TimeSpan.FromSeconds(dur)))
            {
                BeginTime = TimeSpan.FromSeconds(delay),
            };
            var spin = new DoubleAnimation(0, (rng.NextDouble() - 0.5) * 800,
                new Duration(TimeSpan.FromSeconds(dur)))
            {
                BeginTime = TimeSpan.FromSeconds(delay),
            };
            var fade = new DoubleAnimation(0.92, 0, new Duration(TimeSpan.FromSeconds(0.7)))
            {
                BeginTime = TimeSpan.FromSeconds(delay + dur - 0.7),
            };

            piece.BeginAnimation(Canvas.TopProperty,  fall);
            piece.BeginAnimation(Canvas.LeftProperty, drift);
            ((RotateTransform)piece.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, spin);
            piece.BeginAnimation(OpacityProperty, fade);
        }
    }
}
