using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace TodoApp.Behaviors;

/// <summary>
/// Attaches to a ListBox and enables drag-to-reorder via a command.
/// The command receives a (object draggedItem, object targetItem) tuple.
/// Clicks on Button, CheckBox, or TextBox children are ignored so those
/// controls still receive their normal input events.
/// </summary>
public sealed class ListBoxReorderBehavior : Behavior<ListBox>
{
    private Point      _startPoint;
    private ListBoxItem? _draggedContainer;
    private bool       _capturing;

    // ── Dependency property: command ─────────────────────────────────────────

    public static readonly DependencyProperty ReorderCommandProperty =
        DependencyProperty.Register(
            nameof(ReorderCommand),
            typeof(ICommand),
            typeof(ListBoxReorderBehavior));

    public ICommand? ReorderCommand
    {
        get => (ICommand?)GetValue(ReorderCommandProperty);
        set => SetValue(ReorderCommandProperty, value);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override void OnAttached()
    {
        AssociatedObject.AllowDrop = true;
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
        AssociatedObject.PreviewMouseMove           += OnMouseMove;
        AssociatedObject.DragOver                   += OnDragOver;
        AssociatedObject.Drop                       += OnDrop;
        AssociatedObject.PreviewMouseLeftButtonUp   += OnMouseUp;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
        AssociatedObject.PreviewMouseMove           -= OnMouseMove;
        AssociatedObject.DragOver                   -= OnDragOver;
        AssociatedObject.Drop                       -= OnDrop;
        AssociatedObject.PreviewMouseLeftButtonUp   -= OnMouseUp;
    }

    // ── Mouse handlers ────────────────────────────────────────────────────────

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Ignore clicks that originate from interactive child controls
        if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;

        _startPoint       = e.GetPosition(null);
        _draggedContainer = null;
        _capturing        = true;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _capturing        = false;
        _draggedContainer = null;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_capturing || e.LeftButton != MouseButtonState.Pressed) return;

        var pos  = e.GetPosition(null);
        var diff = _startPoint - pos;

        if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            return;

        // Don't start drag from interactive controls
        if (IsInteractiveElement(e.OriginalSource as DependencyObject)) return;

        var container = FindListBoxItem(e.OriginalSource as DependencyObject);
        if (container is null) return;

        _capturing        = false;
        _draggedContainer = container;
        DragDrop.DoDragDrop(container, container.DataContext!, DragDropEffects.Move);
        _draggedContainer = null;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = _draggedContainer is not null ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (_draggedContainer is null) return;

        var target = FindListBoxItem(e.OriginalSource as DependencyObject);
        if (target is null || ReferenceEquals(target, _draggedContainer)) return;

        var cmd = ReorderCommand;
        if (cmd?.CanExecute(null) == true)
            cmd.Execute((_draggedContainer.DataContext, target.DataContext));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ListBoxItem? FindListBoxItem(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is ListBoxItem item &&
                AssociatedObject.ItemContainerGenerator.IndexFromContainer(item) >= 0)
                return item;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private static bool IsInteractiveElement(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is Button or CheckBox or TextBox or ComboBox or Slider or RadioButton)
                return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }
}
