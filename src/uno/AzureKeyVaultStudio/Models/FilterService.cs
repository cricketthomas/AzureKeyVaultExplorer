using System.Collections.ObjectModel;
using System.Linq;
using static AzureKeyVaultStudio.Models.KvTreeNodeModel;

namespace AzureKeyVaultStudio.Models;

public static class FilterService
{
    public static void ResetVisibility(IEnumerable<KvTreeNodeModel> nodes, bool isVisible = true)
    {
        foreach (var node in nodes)
        {
            node.IsVisible = isVisible;
            ResetVisibility(node.Children, isVisible);
        }
    }
    public static IList<KvSubscriptionModel> Filter(IList<KvSubscriptionModel> allSubscriptions, string query)
    {
        if (allSubscriptions == null || allSubscriptions.Count == 0)
        {
            return new List<KvSubscriptionModel>();
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            ResetVisibility(allSubscriptions);
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
        static void SetResourceGroupVisible(KvResourceGroupModel model, bool value)
        {
            if (model.IsVisible != value)
            {
                model.IsVisible = value;
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
            foreach (var resourceGroup in subscription.Children.OfType<KvResourceGroupModel>())
            {
                bool resourceGroupMatch = false;

                if (ContainsQuery(resourceGroup.DisplayName, querySpan))
                {
                    SetResourceGroupExpanded(resourceGroup, true);
                    SetSubscriptionExpanded(subscription, true);
                    resourceGroupMatch = true;
                    isMatch = true;
                    resourceGroup.Children.ForEach(x => x.IsVisible = true);
                }
                else
                {
                    SetResourceGroupExpanded(resourceGroup, true);
                }

                if (!resourceGroupMatch)
                {
                    foreach (var keyVault in resourceGroup.Children.OfType<KvKeyVaultResourceModel>())
                    {
                        if (keyVault.VaultResource?.HasData == true && ContainsQuery(keyVault.VaultResource.Data.Name, querySpan))
                        {
                            SetResourceGroupExpanded(resourceGroup, true);
                            SetSubscriptionExpanded(subscription, true);
                            SetResourceGroupVisible(resourceGroup, true);
                            resourceGroupMatch = true;
                            isMatch = true;
                            keyVault.IsVisible = true;
                            break;
                        }
                        else
                        {
                            SetResourceGroupVisible(resourceGroup, value: false);
                            keyVault.IsVisible = false;
                        }
                    }
                }

                SetResourceGroupExpanded(resourceGroup, resourceGroupMatch);
                SetResourceGroupVisible(resourceGroup, value: resourceGroupMatch);
            }

            if (isMatch)
            {
                results.Add(subscription);
            }
            else
            {
                SetSubscriptionExpanded(subscription, true);
                ResetVisibility(subscription.Children, true);
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
