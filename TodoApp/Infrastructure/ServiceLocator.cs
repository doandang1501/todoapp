using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Infrastructure;

/// <summary>
/// Provides static access to the DI container for code that cannot use constructor injection
/// (e.g. IValueConverter implementations, XAML markup extensions).
///
/// Call <see cref="Initialize"/> exactly once from App.OnStartup before any view is shown.
/// Prefer constructor injection everywhere else.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    /// <summary>Called once from App.xaml.cs after the IHost is built.</summary>
    public static void Initialize(IServiceProvider provider)
        => _provider = provider;

    public static T Get<T>() where T : notnull
    {
        if (_provider is null)
            throw new InvalidOperationException(
                "ServiceLocator.Initialize() must be called before Get<T>().");
        return _provider.GetRequiredService<T>();
    }

    public static T? TryGet<T>() where T : class
        => _provider?.GetService<T>();
}
