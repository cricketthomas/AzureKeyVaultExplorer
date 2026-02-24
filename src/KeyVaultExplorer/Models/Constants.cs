using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Collections.Generic;
using System.IO;

namespace KeyVaultExplorer.Models;

public static class Constants
{
    // database password file name
    public const string EncryptedSecretFileName = "keyvaultexplorerforazure_database_password.txt";

    public const string KeychainSecretName = "keyvaultexplorerforazure_database_password";
    public const string KeychainServiceName = "keyvaultexplorerforazure";
    public const string ProtectedKeyFileName = "keyvaultexplorerforazure_database_key.bin";
    public const string DeviceFileTokenName = "keyvaultexplorerforazure_database_device-token.txt";

    //The Application or Client ID will be generated while registering the app in the Azure portal. This can also be set in settings of the app rather than recompiling.
    public static readonly string ClientId = "fdc1e6da-d735-4627-af3e-d40377f55713";

    //Leaving the scope to its default values.
    public static readonly string[] Scopes = ["openid", "offline_access", "profile", "email",];

    public static readonly string[] AzureRMScope = ["https://management.core.windows.net//.default"];

    public static readonly string[] KvScope = ["https://vault.azure.net/.default"];

    public static readonly string[] AzureScopes = ["https://management.core.windows.net//.default", "https://vault.azure.net//.default", "user_impersonation"];

    // Cache settings
    public const string CacheFileName = "keyvaultexplorer_msal_cache.txt";

    public static readonly string LocalAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\KeyVaultExplorerForAzure";

    public static readonly string DatabaseFilePath = LocalAppDataFolder + "\\KeyVaultExplorerForAzure.db";

    public const string KeyChainServiceName = "keyvaultexplorerforazure_msal_service";
    public const string KeyChainAccountName = "keyvaultexplorerforazure_msal_account";
    public static readonly string DatabasePasswordFilePath = Path.Combine(LocalAppDataFolder, EncryptedSecretFileName);

    public const string LinuxKeyRingSchema = "us.cricketthomas.keyvaultexplorer.tokencache";
    public const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
    public const string LinuxKeyRingLabel = "MSAL token cache for Key Vault Explorer for Azure.";
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");
}