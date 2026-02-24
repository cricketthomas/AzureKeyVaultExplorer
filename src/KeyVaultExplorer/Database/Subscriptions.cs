using System;

namespace KeyVaultExplorer.Database;

public class Subscriptions
{
    public string DisplayName { get; set; }
    public string SubscriptionId { get; set; }
    public Guid TenantId { get; set; }
}