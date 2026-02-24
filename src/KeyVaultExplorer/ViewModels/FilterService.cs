using KeyVaultExplorer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KeyVaultExplorer.ViewModels;

public static class FilterService
{
    public static IList<KvSubscriptionModel> Filter(IList<KvSubscriptionModel> allSubscriptions, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return allSubscriptions;
        }

        var filteredSubscriptions = new List<KvSubscriptionModel>();

        foreach (var subscription in allSubscriptions)
        {
            var filteredResourceGroups = subscription.ResourceGroups
                .Where(resourceGroup =>
                    resourceGroup.ResourceGroupDisplayName != null
                    && resourceGroup.ResourceGroupDisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || resourceGroup.KeyVaultResources.Any(keyVault =>
                        keyVault.HasData
                        && keyVault.Data.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    )
                ).ToList();

            if (filteredResourceGroups.Count != 0)
            {
                var filteredSubscription = new KvSubscriptionModel
                {
                    Subscription = subscription.Subscription,
                    SubscriptionDisplayName = subscription.SubscriptionDisplayName,
                    SubscriptionId = subscription.SubscriptionId,
                    IsExpanded = true, // Expand to show child nodes
                    ResourceGroups = new ObservableCollection<KvResourceGroupModel>(filteredResourceGroups)
                };

                filteredSubscriptions.Add(filteredSubscription);
            }
        }

        // Sort the filtered results alphabetically
        var sortedSubscriptions = SortService.SortSubscriptions(filteredSubscriptions);

        // Sort the nested collections within each subscription
        foreach (var subscription in sortedSubscriptions)
        {
            SortService.SortSubscriptionTree(subscription);
        }

        return sortedSubscriptions;
    }
}