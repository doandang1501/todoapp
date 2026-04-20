using System.Windows;
using System.Windows.Input;
using TodoApp.Services;
using TodoApp.ViewModels;

namespace TodoApp.Views;

/// <summary>
/// Lightweight floating quick-add window triggered by Ctrl+Alt+N global hotkey
/// or the tray "Quick Add" menu item.
/// </summary>
public partial class QuickAddWindow : Window
{
    private readonly QuickAddViewModel _vm;

    public QuickAddWindow(ITodoService todoService)
    {
        InitializeComponent();

        _vm = new QuickAddViewModel(todoService);
        _vm.CloseRequested += () => Close();
        DataContext = _vm;

        // Close on click outside the window
        Deactivated += (_, _) => Close();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Auto-focus the title box
        Dispatcher.BeginInvoke(() => TitleBox.Focus(), System.Windows.Threading.DispatcherPriority.Input);
    }

    // Allow dragging the borderless window
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
