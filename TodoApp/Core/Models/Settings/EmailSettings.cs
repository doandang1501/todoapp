namespace TodoApp.Core.Models.Settings;

/// <summary>
/// Email notification configuration.
/// Supports Brevo (primary) or generic SMTP (fallback).
/// The active provider is selected by <see cref="Provider"/>.
/// </summary>
public class EmailSettings
{
    public bool Enabled { get; set; } = false;

    // ── Provider selection ───────────────────────────────────────────────────
    /// <summary>"Brevo" or "Smtp"</summary>
    public string Provider { get; set; } = "Brevo";

    // ── Brevo ────────────────────────────────────────────────────────────────
    public string BrevoApiKey    { get; set; } = string.Empty;
    public string SenderEmail    { get; set; } = string.Empty;
    public string SenderName     { get; set; } = "TodoApp";

    // ── SMTP fallback ────────────────────────────────────────────────────────
    public string SmtpHost     { get; set; } = string.Empty;
    public int    SmtpPort     { get; set; } = 587;
    public string SmtpUser     { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool   SmtpUseSsl   { get; set; } = true;

    // ── Recipient ────────────────────────────────────────────────────────────
    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName  { get; set; } = string.Empty;

    // ── Which timings trigger an email ───────────────────────────────────────
    public bool SendOneDayBefore       { get; set; } = true;
    public bool SendOneHourBefore      { get; set; } = true;
    public bool SendFiveMinutesBefore  { get; set; } = false;
}
