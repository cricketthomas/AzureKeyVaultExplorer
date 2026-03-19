namespace AzureKeyVaultStudio.Presentation;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel => DataContext as LoginViewModel;

    public LoginPage()
    {
        this.InitializeComponent();
    }
}
