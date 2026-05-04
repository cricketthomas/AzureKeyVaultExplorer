namespace AzureKeyVaultStudio.Database;

public class Subscriptions
{
    public required string DisplayName { get; set; }
    public required string SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
}
