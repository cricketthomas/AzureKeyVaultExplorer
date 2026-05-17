namespace AzureKeyVaultStudio.Presentation;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    public SettingsPage()
    {
        this.InitializeComponent();
    }

}
