using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using KeyVaultExplorer.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private AuthenticatedUserClaims authenticatedUserClaims;

    [ObservableProperty]
    private bool isAuthenticated = false;

    private readonly AuthService _authService;

    public NavigationFactory NavigationFactory { get; }

    partial void OnIsAuthenticatedChanged(bool value)
    {
        AuthenticatedUserClaims = _authService.AuthenticatedUserClaims;
    }

    public MainViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        NavigationFactory = new NavigationFactory();
    }

    public async Task RefreshTokenAndGetAccountInformation()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.RefreshTokenAsync(cancellation);

        if (account is null)
            account = await _authService.LoginAsync(cancellation);
        //.ClaimsPrincipal.Identities.First().FindFirst("email").Value.ToLowerInvariant();
        var identity = account.ClaimsPrincipal.Identities.First();
        var email = identity.FindAll("preferred_username").First().Value ?? account.Account.Username;

        AuthenticatedUserClaims = _authService.AuthenticatedUserClaims;

        IsAuthenticated = _authService.IsAuthenticated;
    }

    [RelayCommand]
    private async Task ForceSignIn()
    {
        var cancellation = new CancellationToken();
        var account = await _authService.LoginAsync(cancellation);
        AuthenticatedUserClaims = _authService.AuthenticatedUserClaims;
        IsAuthenticated = _authService.IsAuthenticated;
    }

    [RelayCommand]
    private async Task SignOut()
    {
        await _authService.RemoveAccount();
        AuthenticatedUserClaims = null;
    }
}
