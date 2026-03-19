namespace KeyVaultExplorer.Database;

public enum SettingType
{
    BackgroundTransparency,
    ClipboardTimeout
}

public class Settings
{
    public SettingType Name { get; set; }
    public object Value { get; set; }
}