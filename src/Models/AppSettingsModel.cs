namespace Ernastie.Models;

public class AppSettingsModel
{
    public string Theme { get; set; } = "Dark"; // "Dark" (ROG Cyberpunk) or "Light" (White Modern)
    public string Language { get; set; } = "vi"; // "vi" (Tiếng Việt) or "en" (English)
    public string AppFolder { get; set; } = string.Empty;
    public string DriverFolder { get; set; } = string.Empty;
}
