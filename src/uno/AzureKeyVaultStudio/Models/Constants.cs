using Microsoft.Identity.Client.Extensions.Msal;

namespace AzureKeyVaultStudio.Models;

public static class Constants
{
    /// constants for values stored in local settings
    public static bool IsAppPackaged => IsPackaged();
    public const string AppName = "KeyVaultExplorerForAzure";

    // database password file name
    public const string EncryptedSecretFileName = $"{AppName}_database_password.txt";
    public const string KeychainSecretName =  $"{AppName}_database_password";
    public const string KeychainServiceName =  $"{AppName}";
    public const string ProtectedKeyFileName =  $"{AppName}_database_key.bin";
    public const string DeviceFileTokenName =  $"{AppName}_database_device-token.txt";
    public const string CustomClientIdName = "CustomClientId";
    public const string CustomTenantIdName = "CustomTenantId";
    public const string SettingsPageClientIdCheckbox = "SettingsPageClientIdCheckbox";
    public const string SettingsPageTenantIdCheckbox = "SettingsPageTenantIdCheckbox";
    public const string SelectedCloudEnvironmentName = "AzureCloudInstance";

    //The Application or Client ID will be generated while registering the app in the Azure portal. This can also be set in settings of the app rather than recompiling.
    public static readonly string ClientId = "fdc1e6da-d735-4627-af3e-d40377f55713";

    //Leaving the scope to its default values.f
    public static readonly string[] Scopes = ["openid", "offline_access", "profile", "email",];

    public static readonly string[] AzureRMScope = ["https://management.core.windows.net//.default"];

    public static readonly string[] KvScope = ["https://vault.azure.net/.default"];

    public static readonly string[] AzureScopes = ["https://management.core.windows.net//.default", "https://vault.azure.net//.default", "user_impersonation"];

    // Cache settings
    public const string CacheFileName =  $"{AppName}_msal_cache.txt";

    public static readonly string LocalAppDataFolder = Path.Combine(IsAppPackaged ? ApplicationData.Current.LocalFolder.Path : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(AzureKeyVaultStudio));


    public static readonly string DatabaseFilePath = Path.Combine(LocalAppDataFolder, $"{AppName}.db");
    public static readonly string DatabasePasswordFilePath = Path.Combine(LocalAppDataFolder, EncryptedSecretFileName);

    public const string KeyChainServiceName = $"{AppName}_msal_service";
    public const string KeyChainAccountName = $"{AppName}_msal_account";
    public const string LinuxKeyRingSchema = $"io.github.cricketthomas.{AppName}.tokencache";
    public const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public const string LinuxKeyRingLabel = "MSAL token cache for Key Vault Explorer for Azure.";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "2");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");

    private static bool IsPackaged()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            return Windows.ApplicationModel.Package.Current != null;
        }
        catch
        {
            return false;
        }
    }
}
