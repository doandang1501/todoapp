using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TodoApp.Core.Models;
using TodoApp.Services;

namespace TodoApp.Views;

public partial class StickyNoteWindow : Window
{
    private readonly IStickyNoteService _service;
    private StickyNote                  _note;

    // Debounce auto-save on text change
    private readonly DispatcherTimer _saveTimer = new()
    {
        Interval = TimeSpan.FromSeconds(1.5)
    };

    public StickyNoteWindow(StickyNote note, IStickyNoteService service)
    {
        InitializeComponent();

        _note    = note;
        _service = service;

        // Apply persisted state
        Left   = note.Left;
        Top    = note.Top;
        Width  = note.Width;
        Height = note.Height;
        Topmost = note.IsAlwaysOnTop;
        ApplyColour(note.BackgroundColor);

        ContentBox.Text = note.Content;
        ContentBox.CaretIndex = ContentBox.Text.Length;

        _saveTimer.Tick += async (_, _) =>
        {
            _saveTimer.Stop();
            await PersistAsync();
        };

        SizeChanged     += OnSizeChanged;
        LocationChanged += OnLocationChanged;
    }

    // ── Drag ──────────────────────────────────────────────────────────────────

    private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    // ── Colour ────────────────────────────────────────────────────────────────

    private void OnColourBtnClick(object sender, RoutedEventArgs e)
        => ColourPopup.IsOpen = true;

    private void OnColourSelected(object sender, MouseButtonEventArgs e)
    {
        ColourPopup.IsOpen = false;
        if (sender is FrameworkElement el && el.Tag is string hex)
        {
            _note.BackgroundColor = hex;
            ApplyColour(hex);
            _ = PersistAsync();
        }
    }

    private void ApplyColour(string hex)
    {
        try
        {
            var colour = (Color)ColorConverter.ConvertFromString(hex);
            RootBorder.Background = new SolidColorBrush(colour);

            // Header is slightly darker
            var h = Color.FromRgb(
                (byte)(colour.R * 0.88),
                (byte)(colour.G * 0.88),
                (byte)(colour.B * 0.88));
            HeaderBrush.Color = h;
        }
        catch { /* invalid hex — keep current */ }
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show("Delete this note?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        _ = _service.DeleteAsync(_note.Id);
        Close();
    }

    // ── Geometry persistence ──────────────────────────────────────────────────

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _note.Width  = Width;
        _note.Height = Height;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _note.Left = Left;
        _note.Top  = Top;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async Task PersistAsync()
    {
        _note.Content = ContentBox.Text;
        try { await _service.UpdateAsync(_note); }
        catch { /* non-fatal */ }
    }
}
