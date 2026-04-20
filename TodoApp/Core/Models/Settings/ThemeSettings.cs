namespace TodoApp.Core.Models.Settings;

public class ThemeSettings
{
    /// <summary>"Light" or "Dark"</summary>
    public string Mode   { get; set; } = "Light";

    /// <summary>"Pink" | "Purple" | "Blue" | "Custom"</summary>
    public string Preset { get; set; } = "Pink";

    // ── Custom colour overrides (hex strings) ────────────────────────────────
    public string PrimaryColor       { get; set; } = "#E91E63";
    public string PrimaryLightColor  { get; set; } = "#F48FB1";
    public string PrimaryDarkColor   { get; set; } = "#C2185B";
    public string AccentColor        { get; set; } = "#FF4081";
    public string BackgroundColor    { get; set; } = "#FFF5F9";
    public string SurfaceColor       { get; set; } = "#FFFFFF";
    public string SidebarColor       { get; set; } = "#FFFFFF";
    public string TextPrimaryColor   { get; set; } = "#212121";
    public string TextSecondaryColor { get; set; } = "#757575";

    // ── UX Options ───────────────────────────────────────────────────────────
    public bool   UseAnimations  { get; set; } = true;
    public bool   ShowConfetti   { get; set; } = true;
    public double CornerRadius   { get; set; } = 12;
    public double ShadowDepth    { get; set; } = 4;

    // ── Auto Dark Mode ────────────────────────────────────────────────────────
    /// <summary>Automatically switch between Light/Dark based on time of day.</summary>
    public bool AutoDarkMode       { get; set; } = false;
    /// <summary>Hour (0–23) when Dark mode activates. Default 18 = 6 PM.</summary>
    public int  DarkModeStartHour  { get; set; } = 18;
    /// <summary>Hour (0–23) when Light mode resumes. Default 6 = 6 AM.</summary>
    public int  LightModeStartHour { get; set; } = 6;
}
