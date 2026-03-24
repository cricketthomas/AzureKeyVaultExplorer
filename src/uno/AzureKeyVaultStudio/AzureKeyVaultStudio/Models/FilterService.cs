using System.Collections.ObjectModel;

namespace AzureKeyVaultStudio.Models;

public static class FilterService
{
    public static IList<KvSubscriptionModel> Filter(IList<KvSubscriptionModel> allSubscriptions, string query)
    {
        if (allSubscriptions == null || allSubscriptions.Count == 0)
        {
            return new List<KvSubscriptionModel>();
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            return allSubscriptions;
        }

        var querySpan = query.AsSpan();
        var results = new List<KvSubscriptionModel>(allSubscriptions.Count);

        static bool ContainsQuery(string? value, ReadOnlySpan<char> querySpan)
            => !string.IsNullOrEmpty(value) && value.AsSpan().Contains(querySpan, StringComparison.OrdinalIgnoreCase);

        static void SetSubscriptionExpanded(KvSubscriptionModel model, bool value)
        {
            if (model.IsExpanded != value)
            {
                model.IsExpanded = value;
            }
        }

        static void SetResourceGroupExpanded(KvResourceGroupModel model, bool value)
        {
            if (model.IsExpanded != value)
            {
                model.IsExpanded = value;
            }
        }

        foreach (var subscription in allSubscriptions)
        {
            if (subscription.Type == KvSubscriptionModel.ExplorerItemType.QuickAccess)
            {
                SetSubscriptionExpanded(subscription, true);
                results.Add(subscription);
                continue;
            }

            bool isMatch = false;

            if (ContainsQuery(subscription.DisplayName, querySpan))
            {
                SetSubscriptionExpanded(subscription, true);
                isMatch = true;
            }
            else
            {
                SetSubscriptionExpanded(subscription, true);
            }

            foreach (var resourceGroup in subscription.ResourceGroups)
            {
                bool resourceGroupMatch = false;

                if (ContainsQuery(resourceGroup.DisplayName, querySpan))
                {
                    SetResourceGroupExpanded(resourceGroup, true);
                    SetSubscriptionExpanded(subscription, true);
                    resourceGroupMatch = true;
                    isMatch = true;
                }
                else
                {
                    SetResourceGroupExpanded(resourceGroup, true);
                }

                if (!resourceGroupMatch)
                {
                    foreach (var keyVault in resourceGroup.KeyVaultResources)
                    {
                        if (keyVault.HasData && ContainsQuery(keyVault.Data.Name, querySpan))
                        {
                            SetResourceGroupExpanded(resourceGroup, true);
                            SetSubscriptionExpanded(subscription, true);
                            resourceGroupMatch = true;
                            isMatch = true;
                            break;
                        }
                    }
                }

                SetResourceGroupExpanded(resourceGroup, true);
            }

            if (!isMatch)
            {
                foreach (var pinnedItem in subscription.PinnedItems)
                {
                    if (pinnedItem.HasData && ContainsQuery(pinnedItem.Data.Name, querySpan))
                    {
                        SetSubscriptionExpanded(subscription, true);
                        isMatch = true;
                        break;
                    }
                }
            }

            if (isMatch)
            {
                results.Add(subscription);
            }
            else
            {
                SetSubscriptionExpanded(subscription, true);
            }
        }

        return results;
    }

    public static ObservableCollection<T> FilterByQuery<T>(
        IEnumerable<T> source,
        string query,
        Func<T, string> nameSelector,
        Func<T, IDictionary<string, string>> tagsSelector,
        Func<T, string> contentTypeSelector)
    {
        if (string.IsNullOrEmpty(query))
        {
            return new ObservableCollection<T>(source);
        }

        var filteredItems = source.Where(item =>
            (nameSelector(item)?.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase) ?? false)
            || (contentTypeSelector(item)?.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase) ?? false)
            || (tagsSelector(item)?.Any(tag =>
                tag.Key.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)
                || tag.Value.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)) ?? false));

        return new ObservableCollection<T>(filteredItems);
    }
}
