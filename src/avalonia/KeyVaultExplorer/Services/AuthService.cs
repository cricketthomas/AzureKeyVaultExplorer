using KeyVaultExplorer.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KeyVaultExplorer.Services;

public class AuthService
{
    public IPublicClientApplication authenticationClient;
    public MsalCacheHelper msalCacheHelper;

    // Providing the RedirectionUri to receive the token based on success or failure.
    public bool IsAuthenticated { get; private set; } = false;

    public AuthenticatedUserClaims AuthenticatedUserClaims { get; private set; }

    public string TenantName { get; private set; }

    public string TenantId { get; private set; }


    public IAccount Account { get; private set; }

    public AuthService()
    {

        var settings = Defaults.Locator.GetRequiredService<AppSettingReader>();
        var customClientId = (string?)settings.AppSettings.CustomClientId ?? Constants.ClientId;
        var settingsPageClientIdCheckbox = (bool?)settings.AppSettings.SettingsPageClientIdCheckbox ?? false;
        string clientId = settingsPageClientIdCheckbox && !string.IsNullOrEmpty(customClientId) ? customClientId : Constants.ClientId;

        authenticationClient = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri($"msal{clientId}://auth")
            .WithRedirectUri("http://localhost")
            .WithIosKeychainSecurityGroup("us.cricketthomas.keyvaultexplorer")
            .Build();
    }

    // Propagates notification that the operation should be cancelled.
    public async Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken)
    {
        await AttachTokenCache();
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
            return authenticationResult;
        }
        catch (MsalClientException ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    /// <summary>
    /// returns the authenticated account with a refreshed token
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AuthenticationResult> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        await AttachTokenCache();
        AuthenticationResult authenticationResult;
        var accounts = await authenticationClient.GetAccountsAsync();
        if (!accounts.Any())
            return null;

        Account = accounts.First();
        authenticationResult = await authenticationClient.AcquireTokenSilent(Constants.Scopes, accounts.FirstOrDefault()).WithForceRefresh(true).ExecuteAsync();
        IsAuthenticated = true;
        TenantName = Account.Username.Split("@").TakeLast(1).Single();
        TenantId = authenticationResult.TenantId;
        AuthenticatedUserClaims = new AuthenticatedUserClaims()
        {
            Username = authenticationResult.Account.Username,
            TenantId = authenticationResult.TenantId,
            Name = authenticationResult.ClaimsPrincipal.Identities.First().FindFirst("name").Value,
            Email = authenticationResult.ClaimsPrincipal.Identities.First().FindFirst("preferred_username").Value
        };

        return authenticationResult;
    }

    private async Task<System.Collections.Generic.IEnumerable<IAccount>> AttachTokenCache()
    {
        // Cache configuration and hook-up to public application. Refer to https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache#configuring-the-token-cache
        //var storageProperties = new StorageCreationPropertiesBuilder("netcore_maui_broker_cache.txt", "C:/temp")

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

    public async Task RemoveAccount()
    {
        await AttachTokenCache();
        var accounts = await authenticationClient.GetAccountsAsync();
        Account = null;
        IsAuthenticated = false;
        AuthenticatedUserClaims = null;
        await authenticationClient.RemoveAsync(accounts.FirstOrDefault());
    }

    public async Task<AuthenticationResult> GetAzureArmTokenSilent()
    {
        await AttachTokenCache();
        var accounts = await authenticationClient.GetAccountsAsync();
        if (!accounts.Any())
        {
            await LoginAsync(CancellationToken.None);
            accounts = await authenticationClient.GetAccountsAsync();
            Account = accounts.First();
        }
        return await authenticationClient.AcquireTokenSilent(Constants.AzureRMScope, accounts.First()).ExecuteAsync();
    }

    public async Task<AuthenticationResult> GetAzureKeyVaultTokenSilent()
    {
        await AttachTokenCache();
        var accounts = await authenticationClient.GetAccountsAsync();
        return await authenticationClient.AcquireTokenSilent(Constants.KvScope, accounts.First()).ExecuteAsync();
    }
}