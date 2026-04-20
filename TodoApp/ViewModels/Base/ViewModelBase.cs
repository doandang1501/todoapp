using CommunityToolkit.Mvvm.ComponentModel;

namespace TodoApp.ViewModels.Base;

/// <summary>
/// Common base class for all ViewModels.
/// Extends CommunityToolkit's <see cref="ObservableObject"/> which implements
/// INotifyPropertyChanged, INotifyPropertyChanging, and the SetProperty helpers.
///
/// Adds a shared IsBusy / BusyMessage pattern used by every loading operation.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool   _isBusy;
    private string _busyMessage = string.Empty;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>Convenience method to flip the busy flag with an optional message.</summary>
    protected void SetBusy(bool busy, string message = "Loading…")
    {
        IsBusy      = busy;
        BusyMessage = message;
    }

    /// <summary>
    /// Called once after the ViewModel is created and its dependencies are resolved.
    /// Override in derived classes to load initial data.
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Wraps an async operation in a try/finally that guarantees
    /// IsBusy is reset even if the delegate throws.
    /// </summary>
    protected async Task RunBusyAsync(Func<Task> action, string message = "Loading…")
    {
        SetBusy(true, message);
        try   { await action(); }
        finally { SetBusy(false); }
    }
}
