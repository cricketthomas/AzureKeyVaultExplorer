using Avalonia.Controls;
using KeyVaultExplorer.ViewModels;

namespace KeyVaultExplorer;

public partial class PropertiesPage : UserControl
{
    public PropertiesPage()
    {
        InitializeComponent();
        DataContext = new PropertiesPageViewModel();
    }
}