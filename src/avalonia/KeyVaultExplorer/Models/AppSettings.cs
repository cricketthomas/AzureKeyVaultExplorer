namespace KeyVaultExplorer.Models;

using System.ComponentModel.DataAnnotations;

public class AppSettings
{
    public bool BackgroundTransparency { get; set; } = false;
    public int ClipboardTimeout { get; set; } = 30;

    [AllowedValues("System", "Light", "Dark")]
    public string AppTheme { get; set; } = "System";

    [AllowedValues("Inline", "Overlay")]
    public string SplitViewDisplayMode { get; set; } = "Inline";

    public string PanePlacement { get; set; } = "Left";
    public bool SettingsPageClientIdCheckbox { get; set; } = false;
    public string CustomClientId { get; set; } = string.Empty;



}