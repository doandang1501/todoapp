using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using TodoApp.Core.Models.Settings;

namespace TodoApp.Services;

/// <summary>
/// Applies <see cref="ThemeSettings"/> to the running WPF application at runtime.
/// Swaps MaterialDesign base theme (Light/Dark) via PaletteHelper and updates
/// all custom colour brushes in Application.Resources directly.
/// Must be called on the UI thread.
/// </summary>
public sealed class ThemeService
{
    private static readonly PaletteHelper _palette = new();

    // Preset → primary hex
    private static readonly Dictionary<string, string> Presets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pink"]   = "#E91E63",
        ["Purple"] = "#9C27B0",
        ["Blue"]   = "#2196F3",
        ["Green"]  = "#4CAF50",
        ["Orange"] = "#FF5722",
    };

    // Preset → background hex (Light mode)
    private static readonly Dictionary<string, string> PresetBg = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pink"]   = "#FFF5F9",
        ["Purple"] = "#F9F5FF",
        ["Blue"]   = "#F5F8FF",
        ["Green"]  = "#F5FFF7",
        ["Orange"] = "#FFF8F5",
    };

    public void Apply(ThemeSettings settings)
    {
        try
        {
            // ── 1. MaterialDesign base theme (Light / Dark) ───────────────────
            var mdTheme = _palette.GetTheme();

            if (settings.Mode == "Dark")
                mdTheme.SetBaseTheme(BaseTheme.Dark);
            else
                mdTheme.SetBaseTheme(BaseTheme.Light);

            // ── 2. Primary colour ─────────────────────────────────────────────
            var primaryHex = settings.Preset != "Custom"
                ? Presets.GetValueOrDefault(settings.Preset, "#E91E63")
                : settings.PrimaryColor;

            var primary = ParseColor(primaryHex, Color.FromRgb(0xE9, 0x1E, 0x63));
            mdTheme.SetPrimaryColor(primary);
            mdTheme.SetSecondaryColor(primary);

            _palette.SetTheme(mdTheme);

            // ── 3. Compute derived colours ────────────────────────────────────
            var primaryLight = Lighten(primary, 0.25f);
            var primaryDark  = Darken(primary,  0.15f);
            var accent       = primary; // same hue, different lightness works fine

            string bgHex;
            if (settings.Mode == "Dark")
                bgHex = "#1E1E2E";
            else if (settings.Preset == "Custom")
                bgHex = settings.BackgroundColor;
            else
                bgHex = PresetBg.GetValueOrDefault(settings.Preset, "#FFF5F9");

            var bg      = ParseColor(bgHex,      Color.FromRgb(0xFF, 0xF5, 0xF9));
            var surface = settings.Mode == "Dark"
                ? Color.FromRgb(0x2A, 0x2A, 0x3E)
                : Color.FromRgb(0xFF, 0xFF, 0xFF);
            var textPrimary   = settings.Mode == "Dark"
                ? Color.FromRgb(0xE0, 0xE0, 0xE0)
                : Color.FromRgb(0x21, 0x21, 0x21);
            var textSecondary = settings.Mode == "Dark"
                ? Color.FromRgb(0xA0, 0xA0, 0xA0)
                : Color.FromRgb(0x75, 0x75, 0x75);
            var divider = settings.Mode == "Dark"
                ? Color.FromRgb(0x38, 0x38, 0x50)
                : Color.FromRgb(0xEE, 0xEE, 0xEE);

            // ── 4. Update App.Resources brushes ──────────────────────────────
            var res = Application.Current.Resources;
            res["PrimaryBrush"]       = Brush(primary);
            res["PrimaryLightBrush"]  = Brush(primaryLight);
            res["PrimaryDarkBrush"]   = Brush(primaryDark);
            res["AccentBrush"]        = Brush(accent);
            res["AccentLightBrush"]   = Brush(primaryLight);
            res["AppBackground"]      = Brush(bg);
            res["SidebarBackground"]  = Brush(surface);
            res["SurfaceBrush"]       = Brush(surface);
            res["CardBackgroundBrush"]= Brush(surface);
            res["TextPrimaryBrush"]   = Brush(textPrimary);
            res["TextSecondaryBrush"] = Brush(textSecondary);
            res["DividerBrush"]       = Brush(divider);
            res["BorderBrush2"]       = Brush(divider);

            // Primary-50 (very light tint for hover backgrounds)
            res["Primary50Brush"]  = Brush(Lighten(primary, 0.75f));
            res["Primary100Brush"] = Brush(Lighten(primary, 0.55f));
            res["Primary200Brush"] = Brush(primaryLight);
            res["Primary300Brush"] = Brush(Lighten(primary, 0.10f));
        }
        catch
        {
            // Non-fatal — keep previous theme if anything goes wrong
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SolidColorBrush Brush(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return fallback; }
    }

    private static Color Lighten(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromRgb(
            (byte)(c.R + (255 - c.R) * amount),
            (byte)(c.G + (255 - c.G) * amount),
            (byte)(c.B + (255 - c.B) * amount));
    }

    private static Color Darken(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromRgb(
            (byte)(c.R * (1 - amount)),
            (byte)(c.G * (1 - amount)),
            (byte)(c.B * (1 - amount)));
    }
}
