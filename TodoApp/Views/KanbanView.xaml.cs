using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TodoApp.ViewModels;

namespace TodoApp.Views;

public partial class KanbanView : UserControl
{
    private Point   _dragStartPoint;
    private bool    _isDragging;
    private TaskRowViewModel? _draggedTask;

    // Drop zone highlight colour
    private static readonly SolidColorBrush HighlightBrush =
        new(Color.FromArgb(40, 233, 30, 99));   // semi-transparent pink

    public KanbanView()
    {
        InitializeComponent();
    }

    // ── Drag start ────────────────────────────────────────────────────────────

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _dragStartPoint = e.GetPosition(null);
        _isDragging     = false;

        // Resolve the TaskRowViewModel from the card's Tag
        if (sender is FrameworkElement el)
            _draggedTask = el.Tag as TaskRowViewModel;
    }

    private void OnCardMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedTask == null) return;
        if (_isDragging) return;

        var pos  = e.GetPosition(null);
        var diff = _dragStartPoint - pos;

        bool beyondThreshold =
            Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

        if (!beyondThreshold) return;

        _isDragging = true;

        var data = new DataObject("KanbanTask", _draggedTask.Id);
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, data, DragDropEffects.Move);

        _isDragging  = false;
        _draggedTask = null;
    }

    // ── Drop zone visual feedback ─────────────────────────────────────────────

    private void OnColumnDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("KanbanTask"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        if (sender is Border border)
            border.Background = HighlightBrush;
    }

    private void OnColumnDragLeave(object sender, DragEventArgs e)
    {
        if (sender is Border border)
            ClearColumnHighlight(border);
    }

    // ── Drop ──────────────────────────────────────────────────────────────────

    private void OnColumnDrop(object sender, DragEventArgs e)
    {
        if (sender is Border border)
            ClearColumnHighlight(border);

        if (!e.Data.GetDataPresent("KanbanTask")) return;

        var taskId       = (Guid)e.Data.GetData("KanbanTask");
        var targetColumn = (sender as FrameworkElement)?.Tag as string;

        if (targetColumn is null) return;

        if (DataContext is KanbanViewModel vm)
            vm.MoveTaskCommand.Execute((taskId, targetColumn));

        e.Handled = true;
    }

    // ── Click empty area to add task ──────────────────────────────────────────

    private void OnDropZoneMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        // Only act when the user was not dragging a card
        if (_isDragging) return;

        // Don't open add-dialog if the click landed on or inside a card
        var target   = (DependencyObject?)sender;
        var hitObj   = e.OriginalSource as DependencyObject;
        while (hitObj != null && !ReferenceEquals(hitObj, target))
        {
            if (hitObj is FrameworkElement { DataContext: TaskRowViewModel })
                return; // released over a card — don't add new task
            hitObj = VisualTreeHelper.GetParent(hitObj);
        }

        if (sender is not FrameworkElement el) return;
        var tag = el.Tag as string;

        if (DataContext is KanbanViewModel vm)
            vm.AddTaskCommand.Execute(tag);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ClearColumnHighlight(Border border)
    {
        // Restore the original AppBackground; use transparent so the DynamicResource applies
        border.Background = null;
        // Rebind to resource
        border.SetResourceReference(BackgroundProperty, "AppBackground");
    }
}
