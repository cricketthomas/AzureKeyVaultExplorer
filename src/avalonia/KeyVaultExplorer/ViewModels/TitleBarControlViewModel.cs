using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Services;
using System.Threading;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class TitleBarViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    public TitleBarViewModel(AuthService authService, VaultService vaultService)
    {
        _authService = authService;
    }

    [ObservableProperty]
    private string title = "Key Vault Explorer for Azure";

    public TitleBarViewModel()
    {
        _authService = new AuthService();
    }

    [RelayCommand]
    private async void SignIn()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.RefreshTokenAsync(cancellation);
        if (account == null)
            await _authService.LoginAsync(cancellation);
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _authService.RemoveAccount();
    }
}