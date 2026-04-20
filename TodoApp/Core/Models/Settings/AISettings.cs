namespace TodoApp.Core.Models.Settings;

public class AISettings
{
    public bool   Enabled      { get; set; } = false;
    public string GeminiApiKey { get; set; } = "";
    public string ModelName    { get; set; } = "gemini-2.0-flash";
}
