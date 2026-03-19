namespace AzureKeyVaultStudio.UserControls;

public sealed partial class StatusBarControl : UserControl
{
    public StatusBarViewModel? ViewModel => DataContext as StatusBarViewModel;
    public StatusBarControl()
    {
        this.InitializeComponent();
        DataContext = new StatusBarViewModel();
    }
}
