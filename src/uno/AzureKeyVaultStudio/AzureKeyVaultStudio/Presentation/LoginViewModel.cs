using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation;

[Microsoft.UI.Xaml.Data.Bindable]
public partial class LoginViewModel : ObservableObject
{
    private IAuthenticationService _authentication;

    private INavigator _navigator;

    private IDispatcher _dispatcher;

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; } = false;

    [ObservableProperty]
    public partial AuthenticatedUserClaims? Claims { get; set; }

    public LoginViewModel(IDispatcher dispatcher, INavigator navigator, AuthService authService, IAuthenticationService authentication)
    {
        _dispatcher = dispatcher;
        _navigator = navigator;
        _authentication = authentication;
        WeakReferenceMessenger.Default.Register<LoginViewModel, AuthenticationStateChangedMessage>(this, (r, m) =>
        {
            r.Claims = m.Value;
        });
        WeakReferenceMessenger.Default.Register<LoginViewModel, AuthenticationRemovedStateChangedMessage>(this, (r, m) =>
        {
            Claims = new();
            IsAuthenticated = false;
        });
    }

    [RelayCommand]
    private async Task GoToSettingsView()
    {
        await _navigator.NavigateViewModelAsync<SettingsViewModel>(this);
    }

    [RelayCommand]
    private async Task GoToSubscriptionsPage()
    {
        await _navigator.NavigateViewModelAsync<SubscriptionViewModel>(this);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task GoToMainLayoutPage(CancellationToken token)
    {
        try
        {
            var success = await _authentication.LoginAsync(_dispatcher, new Dictionary<string, string> { { "Username", string.Empty }, { "Password", string.Empty } }, cancellationToken: token);
            token.ThrowIfCancellationRequested();
#if DEBUG
            await Task.Delay(4000, token);
#endif
            if (success)
            {
                await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack, cancellation: token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Loading canceled.");
            return;
        }
    }
}
