using Azure.ResourceManager.Resources;
using CommunityToolkit.Mvvm.ComponentModel;

namespace KeyVaultExplorer.Models;

public partial class SubscriptionDataItem : ObservableObject
{
    public SubscriptionData Data { get; set; } = null!;

    public SubscriptionResource Resource { get; set; } = null!;

    [ObservableProperty]
    private bool isPinned;

    [ObservableProperty]
    private bool? isUpdated;

    // this sets the default value to make sure we're not tracking initially loaded items, only changed.
    partial void OnIsPinnedChanging(bool oldValue, bool newValue)
    {
        if (IsUpdated is null)
            IsUpdated = false;
        else
            IsUpdated = true;
    }
}