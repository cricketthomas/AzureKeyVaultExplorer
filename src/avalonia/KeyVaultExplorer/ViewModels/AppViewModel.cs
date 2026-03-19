using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Views;

namespace KeyVaultExplorer.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    // private readonly AuthService _authService;

    [ObservableProperty]
    private string email;

    [ObservableProperty]
    private bool isAuthenticated = false;

    public AppViewModel()
    {
        // _authService = Defaults.Locator.GetRequiredService<AuthService>();
    }

    [RelayCommand]
    public void About()
    {
        var aboutWindow = new AboutPageWindow()
        {
            Title = "About Key Vault Explorer",
            Width = 380,
            Height = 200,
            CanResize = false,
            SizeToContent = SizeToContent.Manual,
            WindowStartupLocation = WindowStartupLocation.Manual,
        };

        var top = Avalonia.Application.Current.GetTopLevel() as MainWindow;
        aboutWindow.ShowDialog(top);
    }
}