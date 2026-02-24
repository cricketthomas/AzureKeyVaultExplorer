namespace KeyVaultExplorer.Database;

public partial class QuickAccess
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string VaultUri { get; set; } = null!;

    public string KeyVaultId { get; set; } = null!;

    public string? SubscriptionDisplayName { get; set; }

    public string? SubscriptionId { get; set; }

    public string TenantId { get; set; } = null!;

    public string Location { get; set; } = null!;
}