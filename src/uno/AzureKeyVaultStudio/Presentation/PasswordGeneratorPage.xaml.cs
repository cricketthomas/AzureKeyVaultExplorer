using AzureKeyVaultStudio.Presentation.TabsModels;
using AzureKeyVaultStudio.Presentation.ViewModels;

namespace AzureKeyVaultStudio.Presentation;

public sealed partial class PasswordGeneratorPage : Page
{
    public PasswordGeneratorViewModel ViewModel => (DataContext as PasswordGeneratorTab).ViewModel;

    public PasswordGeneratorPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is PasswordGeneratorTab passwordTab)
        {
            DataContext = passwordTab;
        }
    }
}
