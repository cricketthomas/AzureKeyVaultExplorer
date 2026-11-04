using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation;

public class ShellViewModel
{
    private readonly IAuthenticationService _authentication;


    private readonly INavigator _navigator;
    private readonly ILocalSettingsService _settings;

    public ShellViewModel(
        IAuthenticationService authentication,
        ILocalSettingsService settings,
        INavigator navigator)
    {
        _navigator = navigator;
        _authentication = authentication;
        _settings = settings;
        _authentication.LoggedOut += LoggedOut;
        WeakReferenceMessenger.Default.Register<NavigationBackRequestedMessage>(this, async (r, m) => await GoBack());

     

    }

    private async void LoggedOut(object? sender, EventArgs e)
    {
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
        WeakReferenceMessenger.Default.Send(new BackButtonIsHiddenMessage());

    }


    private async Task GoBack()
    {

        if (_navigator == null)
            return;
        try
        {
            if (await _navigator.CanGoBack())
                await _navigator.NavigateBackAsync(this);
            WeakReferenceMessenger.Default.Send(new BackButtonIsHiddenMessage());

        }
        catch (Exception ex)
        {
            Debug.Write($"Failed to navigate back {ex.Message}", ex.StackTrace);
        }
    }
}
