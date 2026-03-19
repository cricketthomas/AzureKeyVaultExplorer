using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyVaultExplorer.ViewModels;

public partial class ToolBarViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isBusy;

    public ToolBarViewModel()
    {
    }
}