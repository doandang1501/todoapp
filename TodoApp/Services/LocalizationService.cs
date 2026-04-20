using System.Windows;

namespace TodoApp.Services;

/// <summary>
/// Swaps the active language ResourceDictionary at runtime so all
/// {DynamicResource Str_xxx} bindings update without a restart.
/// </summary>
public sealed class LocalizationService
{
    private const string LangDictUri_Vi = "/Languages/vi.xaml";
    private const string LangDictUri_En = "/Languages/en.xaml";

    private string _currentLanguage = "vi";
    public  string CurrentLanguage  => _currentLanguage;

    /// <summary>
    /// Apply <paramref name="language"/> ("vi" or "en").
    /// Must be called on the UI thread.
    /// </summary>
    public void Apply(string language)
    {
        var uri = language == "en" ? LangDictUri_En : LangDictUri_Vi;
        _currentLanguage = language == "en" ? "en" : "vi";

        var resources = Application.Current.Resources.MergedDictionaries;

        // Remove the old language dict (if any)
        var old = resources.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("/Languages/") == true);
        if (old is not null)
            resources.Remove(old);

        // Add the new one
        resources.Add(new ResourceDictionary
        {
            Source = new Uri(uri, UriKind.Relative)
        });
    }

    /// <summary>Look up a localised string by key (fallback = key name).</summary>
    public string Translate(string key)
    {
        return Application.Current.TryFindResource(key) as string ?? key;
    }
}
