using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace AzureKeyVaultStudio.Models;

public partial class SubscriptionDataItemModel : ObservableObject
{
    public SubscriptionData Data { get; set; } = null!;

    public string? SubscriptionId => Data?.SubscriptionId;
    public Guid? TenantId => Data?.TenantId;
    public SubscriptionState? State => Data?.State;

    public SubscriptionResource Resource { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool? IsUpdated { get; set; }

    partial void OnIsPinnedChanged(bool oldValue, bool newValue)
    {
        if (IsUpdated is null && newValue != true)
            IsUpdated = false;
        else
            IsUpdated = true;
    }
}
