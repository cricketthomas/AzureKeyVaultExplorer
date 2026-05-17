using AzureKeyVaultStudio.Services;

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
    }

    private async void LoggedOut(object? sender, EventArgs e)
    {
        await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
}
