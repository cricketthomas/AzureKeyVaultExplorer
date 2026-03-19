using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Uno.UI.MSAL;

namespace AzureKeyVaultStudio.Services;

public class AuthService
{
    public IPublicClientApplication authenticationClient;
    public MsalCacheHelper msalCacheHelper;
    private readonly ILocalSettingsService _localSettings;
    private readonly ILogger<AuthService> _logger;
    public AzureCloudInstance CloudInstance { get; private set; }

    public AuthService(ILocalSettingsService localSettings, ILogger<AuthService> logger)
    {
        _localSettings = localSettings;
        _logger = logger;
        string customClientId = _localSettings.GetValue(Constants.CustomClientIdName, "");
        var isCustomClientIdWanted = _localSettings.GetValue(Constants.SettingsPageClientIdCheckbox, false);
        string clientId = isCustomClientIdWanted && !string.IsNullOrEmpty(customClientId) ? customClientId : Constants.ClientId;

        string customTenantId = _localSettings.GetValue(Constants.CustomTenantIdName, "");
        var isCustomTenantWanted = _localSettings.GetValue(Constants.SettingsPageTenantIdCheckbox, false);
        string tenantId = isCustomClientIdWanted && !string.IsNullOrEmpty(customTenantId) ? customTenantId : string.Empty;

        int savedCloudId = _localSettings.GetValue<int>(Constants.SelectedCloudEnvironmentName, (int)AzureCloudInstance.AzurePublic);
        CloudInstance = (AzureCloudInstance)savedCloudId;

        var builder = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri($"msal{clientId}://auth")
            .WithRedirectUri("http://localhost")
            .WithIosKeychainSecurityGroup(Constants.LinuxKeyRingSchema)
            .WithUnoHelpers();

        if (!string.IsNullOrWhiteSpace(customTenantId) && isCustomTenantWanted)
            builder = builder.WithAuthority(CloudInstance, customTenantId);
        else
            builder = builder.WithAuthority(CloudInstance, AadAuthorityAudience.AzureAdMultipleOrgs);

        authenticationClient = builder.Build();
        _ = AttachTokenCache();
    }

    public IAccount? Account { get; private set; }
    public AuthenticatedUserClaims? AuthenticatedUserClaims { get; private set; }
    public bool IsAuthenticated { get; private set; } = false;
    public string TenantId { get; private set; }
    public string TenantName { get; private set; }
    public DateTimeOffset Expiry { get; private set; }

    private async Task<IAccount?> GetPrimaryAccountAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await authenticationClient.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is not null)
            return account;

        var refreshed = await RefreshTokenAsync(cancellationToken);
        if (refreshed?.Account is not null)
            return refreshed.Account;
        // todo.. add account picker
        accounts = await authenticationClient.GetAccountsAsync();
        return accounts.FirstOrDefault();
    }

    public async Task<AuthenticationResult?> GetAzureArmTokenSilent()
    {
        var account = await GetPrimaryAccountAsync();
        if (account is null)
        {
            _logger.LogInformationMessage("No logged-in accounts were found.");
            return null;
        }

        return await authenticationClient
            .AcquireTokenSilent(Constants.AzureRMScope, account)
            .ExecuteAsync();
    }

    public async Task<AuthenticationResult?> GetAzureKeyVaultTokenSilent()
    {
        var account = await GetPrimaryAccountAsync();
        if (account is null)
        {
            _logger.LogInformationMessage("No logged-in accounts were found.");
            return null;    
        }

        return await authenticationClient
            .AcquireTokenSilent(Constants.KvScope, account)
            .ExecuteAsync();
    }


    public async Task<AuthenticationResult?> LoginAsync(CancellationToken cancellationToken)
    {
        //await AttachTokenCache();
        AuthenticationResult authenticationResult;
        try
        {
            var options = new SystemWebViewOptions()
            {
                HtmlMessageError = "<p> An error occurred: {0}. Details {1}</p>",
                BrowserRedirectSuccess = new Uri("https://www.microsoft.com")
            };
            //.WithPrompt(Prompt.ForceLogin) //This is optional. If provided, on each execution, the username and the password must be entered.
            //#if MACCATALYST
            //.WithUseEmbeddedWebView(false)
            //.WithSystemWebViewOptions(options)
            //#endif
            authenticationResult = await authenticationClient.AcquireTokenInteractive(Constants.Scopes)
                //.WithExtraScopesToConsent(Constants.AzureRMScope)
                /*
                 * Not including extra scopes allows personal accounts to sign in, however, this will be thrown.
                 (Windows Azure Service Management API) is configured for use by Azure Active Directory users only.
                    Please do not use the /consumers endpoint to serve this request. T

                https://stackoverflow.com/questions/66470333/error-azure-key-vault-is-configured-for-use-by-azure-active-directory-users-on
                 */

                .ExecuteAsync(cancellationToken);

            IsAuthenticated = true;
            TenantName = authenticationResult.Account.Username.Split("@").TakeLast(1).Single();
            TenantId = authenticationResult.TenantId;
            AuthenticatedUserClaims = new AuthenticatedUserClaims()
            {
                Username = authenticationResult.Account.Username,
                TenantId = authenticationResult.TenantId,
                Name = authenticationResult.ClaimsPrincipal.Identities.First().FindFirst("name").Value,
                Email = authenticationResult.ClaimsPrincipal.Identities.First().FindFirst("preferred_username").Value
            };

            // set the preferences/settings of the signed in account
            //IAccount cachedUserAccount = Task.Run(async () => await PublicClientSingleton.Instance.MSALClientHelper.FetchSignedInUserFromCache()).Result;
            //Preferences.Default.Set("auth_account_id", JsonSerializer.Serialize(result.UniqueId));
            Expiry = authenticationResult.ExpiresOn;
            WeakReferenceMessenger.Default.Send(new AuthenticationStateChangedMessage(AuthenticatedUserClaims));

            return authenticationResult;
        }
        catch (MsalClientException ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    public async Task<AuthenticationResult?> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        //await AttachTokenCache();
        AuthenticationResult authenticationResult;
        var accounts = await authenticationClient.GetAccountsAsync();
        if (!accounts.Any())
            return null;

        Account = accounts.First();
        authenticationResult = await authenticationClient.AcquireTokenSilent(Constants.Scopes, accounts.FirstOrDefault()).WithForceRefresh(true).ExecuteAsync(cancellationToken);
        IsAuthenticated = true;
        TenantName = Account.Username.Split("@").TakeLast(1).Single();
        TenantId = authenticationResult.TenantId;
        Expiry = authenticationResult.ExpiresOn;

        TransformClaims(authenticationResult);

        WeakReferenceMessenger.Default.Send(new AuthenticationStateChangedMessage(AuthenticatedUserClaims));
        return authenticationResult;

       
    }
    private void TransformClaims(AuthenticationResult authenticationResult)
    {
        AuthenticatedUserClaims = new AuthenticatedUserClaims()
        {
            Username = authenticationResult.Account.Username,
            TenantId = authenticationResult.TenantId,
            Name = authenticationResult.ClaimsPrincipal?.Identities?.FirstOrDefault()?.FindFirst("name")?.Value,
            Email = authenticationResult.ClaimsPrincipal?.Identities?.FirstOrDefault()?.FindFirst("preferred_username")?.Value ?? string.Empty
        };
    }
    private async Task RemoveAccount()
    {
        var accounts = await authenticationClient.GetAccountsAsync();
        Account = null;
        IsAuthenticated = false;
        AuthenticatedUserClaims = null;
        foreach (var account in accounts)
            await authenticationClient.RemoveAsync(account);
    }

    /// <summary>
    /// Attempts silent refresh first, falls back to interactive login.
    /// </summary>
    public async Task<bool> LoginOrRefreshAsync(CancellationToken cancellationToken)
    {
        var result = await RefreshTokenAsync(cancellationToken);
        if (result is null)
        {
            result = await LoginAsync(cancellationToken);
            TransformClaims(result);
            WeakReferenceMessenger.Default.Send(new AuthenticationStateChangedMessage(AuthenticatedUserClaims));
        }

        return IsAuthenticated;
    }

    public async Task SignOutAsync()
    {
        await RemoveAccount();
        WeakReferenceMessenger.Default.Send(new AuthenticationRemovedStateChangedMessage());
    }

    private async Task<IEnumerable<IAccount>> AttachTokenCache()
    {
        // Cache configuration and hook-up to public application. Refer to https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache#configuring-the-token-cache
        var storageProperties =
             new StorageCreationPropertiesBuilder(Constants.CacheFileName, Constants.LocalAppDataFolder)
               .WithLinuxKeyring(Constants.LinuxKeyRingSchema, Constants.LinuxKeyRingCollection, Constants.LinuxKeyRingLabel, Constants.LinuxKeyRingAttr1, Constants.LinuxKeyRingAttr2)
               .WithMacKeyChain(Constants.KeyChainServiceName, Constants.KeyChainAccountName)
               .Build();

        msalCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        msalCacheHelper.RegisterCache(authenticationClient.UserTokenCache);

        //msalCacheHelper.CacheChanged += (object sender, CacheChangedEventArgs args) =>
        //{
        //    Console.WriteLine($"Cache Changed, Added: {args.AccountsAdded.Count()} Removed: {args.AccountsRemoved.Count()}");
        //};

        // If the cache file is being reused, we'd find some already-signed-in accounts

        return await authenticationClient.GetAccountsAsync().ConfigureAwait(false);
    }
}
