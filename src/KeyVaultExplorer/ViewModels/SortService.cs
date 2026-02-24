using Azure.ResourceManager.KeyVault;
using KeyVaultExplorer.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KeyVaultExplorer.ViewModels;

public static class SortService
{
    /// <summary>
    /// Sorts key vaults alphabetically by name
    /// </summary>
    public static ObservableCollection<KeyVaultResource> SortKeyVaults(IEnumerable<KeyVaultResource> keyVaults)
    {
        var sorted = keyVaults.OrderBy(kv => kv.HasData ? kv.Data.Name : string.Empty);
        return new ObservableCollection<KeyVaultResource>(sorted);
    }

    /// <summary>
    /// Sorts resource groups alphabetically by display name
    /// </summary>
    public static ObservableCollection<KvResourceGroupModel> SortResourceGroups(IEnumerable<KvResourceGroupModel> resourceGroups)
    {
        var sorted = resourceGroups.OrderBy(rg => rg.ResourceGroupDisplayName ?? string.Empty);
        return new ObservableCollection<KvResourceGroupModel>(sorted);
    }

    /// <summary>
    /// Sorts subscriptions alphabetically by display name, keeping "Quick Access" at the top
    /// </summary>
    public static ObservableCollection<KvSubscriptionModel> SortSubscriptions(IEnumerable<KvSubscriptionModel> subscriptions)
    {
        var subscriptionsList = subscriptions.ToList();

        var quickAccess = subscriptionsList.Where(s => s.SubscriptionDisplayName == "Quick Access").ToList();
        var regularSubscriptions = subscriptionsList.Where(s => s.SubscriptionDisplayName != "Quick Access").ToList();

        var sortedRegular = regularSubscriptions.OrderBy(s => s.SubscriptionDisplayName ?? string.Empty);

        var result = new List<KvSubscriptionModel>();
        result.AddRange(quickAccess);
        result.AddRange(sortedRegular);

        return new ObservableCollection<KvSubscriptionModel>(result);
    }
    /// <summary>
    /// Sorts an entire subscription model tree, including all nested collections
    /// </summary>
    public static void SortSubscriptionTree(KvSubscriptionModel subscription)
    {
        subscription.ResourceGroups = SortResourceGroups(subscription.ResourceGroups);
        foreach (var resourceGroup in subscription.ResourceGroups)
        {
            resourceGroup.KeyVaultResources = SortKeyVaults(resourceGroup.KeyVaultResources);
        }
    }
}