using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using KeyVaultExplorer.Database;
using KeyVaultExplorer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KeyVaultExplorer.Services;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        collection.AddMemoryCache();
        collection.AddSingleton<AuthService>();
        collection.AddSingleton<VaultService>();
        collection.AddSingleton<TabViewPageViewModel>();
        //collection.AddSingleton<ToolBarViewModel>();
        collection.AddSingleton<KeyVaultTreeListViewModel>();
        collection.AddSingleton<SettingsPageViewModel>();
        collection.AddSingleton<MainViewModel>();
        collection.AddSingleton<SubscriptionsPageViewModel>();
        collection.AddSingleton<NotificationViewModel>();
        collection.AddSingleton<KvExplorerDb>();
        collection.AddTransient<AppSettingReader>();
        collection.AddSingleton<ClipboardService>();
        collection.AddSingleton<StorageProviderService>();
    }
}