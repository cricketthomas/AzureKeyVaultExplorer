using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Humanizer;

namespace AzureKeyVaultStudio.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class KeyVaultItemProperties
{
    public Uri Id { get; set; } = null!;
    public Uri VaultUri { get; set; } = null!;
    public Uri ValueUri { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedOn { get; set; }
    public DateTimeOffset? CreatedOn { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset? NotBefore { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public int? RecoverableDays { get; set; }
    public string? RecoveryLevel { get; set; }
    public bool? Managed { get; set; }
    public KeyVaultItemType Type { get; set; }

    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    public SecretProperties SecretProperties { get; set; } = null!;
    public KeyProperties KeyProperties { get; set; } = null!;
    public CertificateProperties? CertificateProperties { get; set; } = null!;

    public string[] TagValues => Tags is not null ? [.. Tags.Values] : [];
    public string[] TagKeys => Tags is not null ? [.. Tags.Keys] : [];
    public string TagValuesString => string.Join(", ", Tags?.Values ?? []);
    public ObservableCollection<TagItem> EditableTags { get; set; } = [];

    public DateTimeOffset? LastModifiedDate => UpdatedOn.HasValue ? UpdatedOn.Value.ToLocalTime() : CreatedOn?.ToLocalTime();
    public string? WhenLastModified => LastModifiedDate.HasValue ? LastModifiedDate.Value.Humanize() : null;
    public string? WhenExpires => ExpiresOn.HasValue ? ExpiresOn.Value.Humanize() : null;

    public bool IsExpired => ExpiresOn.HasValue && (ExpiresOn.Value < DateTimeOffset.Now);

    public static KeyVaultItemProperties FromSecretProperties(SecretProperties properties)
      => Create(
          properties.Id,
          properties.VaultUri,
          properties.Name,
          properties.Version,
          properties.ContentType,
          properties.Tags,
          properties.UpdatedOn,
          properties.CreatedOn,
          properties.ExpiresOn,
          properties.NotBefore,
          properties.Enabled,
          properties.RecoverableDays,
          properties.RecoveryLevel,
          KeyVaultItemType.Secret,
          properties.Managed
          );

    public static IReadOnlyList<KeyVaultItemProperties> FromSecretProperties(IEnumerable<SecretProperties>? properties)
        => properties is null ? [] : [.. properties.Select(FromSecretProperties)];

    public static KeyVaultItemProperties FromKeyProperties(KeyProperties properties)
        => Create(
            properties.Id,
            properties.VaultUri,
            properties.Name,
            properties.Version,
            string.Empty,
            properties.Tags,
            properties.UpdatedOn,
            properties.CreatedOn,
            properties.ExpiresOn,
            properties.NotBefore,
            properties.Enabled,
            properties.RecoverableDays,
            properties.RecoveryLevel,
            KeyVaultItemType.Key,
            properties.Managed);

    public static IReadOnlyList<KeyVaultItemProperties> FromKeyProperties(IEnumerable<KeyProperties>? properties)
        => properties is null ? [] : [.. properties.Select(FromKeyProperties)];

    public static KeyVaultItemProperties FromCertificateProperties(CertificateProperties properties)
        => Create(
            properties.Id,
            properties.VaultUri,
            properties.Name,
            properties.Version,
            string.Empty,
            properties.Tags,
            properties.UpdatedOn,
            properties.CreatedOn,
            properties.ExpiresOn,
            properties.NotBefore,
            properties.Enabled,
            properties.RecoverableDays,
            properties.RecoveryLevel,
            KeyVaultItemType.Certificate);

    public static IReadOnlyList<KeyVaultItemProperties> FromCertificateProperties(IEnumerable<CertificateProperties>? properties)
        => properties is null ? [] : [.. properties.Select(FromCertificateProperties)];

    public SecretProperties ToSecretProperties()
    {
        var properties = new SecretProperties(Id);
        properties.ContentType = ContentType;
        properties.Enabled = Enabled;
        properties.NotBefore = NotBefore;
        properties.ExpiresOn = ExpiresOn;
        ApplyEditableTags(properties.Tags);
        return properties;
    }

    public KeyProperties ToKeyProperties()
    {
        var properties = new KeyProperties(Id);
        properties.Enabled = Enabled;
        properties.NotBefore = NotBefore;
        properties.ExpiresOn = ExpiresOn;
        ApplyEditableTags(properties.Tags);
        return properties;
    }

    public CertificateProperties ToCertificateProperties()
    {
        var properties = new CertificateProperties(Id);
        properties.Enabled = Enabled;
        ApplyEditableTags(properties.Tags);
        return properties;
    }

    private static KeyVaultItemProperties Create(
        Uri id,
        Uri vaultUri,
        string name,
        string version,
        string contentType,
        IDictionary<string, string>? tags,
        DateTimeOffset? updatedOn,
        DateTimeOffset? createdOn,
        DateTimeOffset? expiresOn,
        DateTimeOffset? notBefore,
        bool? enabled,
        int? recoverableDays,
        string? recoveryLevel,
        KeyVaultItemType type,
        bool? managed = null
        )
    {
        return new KeyVaultItemProperties
        {
            Id = id,
            VaultUri = vaultUri,
            ValueUri = id,
            Name = name,
            Version = version,
            ContentType = contentType,
            UpdatedOn = updatedOn,
            CreatedOn = createdOn,
            ExpiresOn = expiresOn,
            NotBefore = notBefore,
            Enabled = enabled ?? true,
            RecoverableDays = recoverableDays,
            RecoveryLevel = recoveryLevel,
            Managed = managed,
            Tags = tags is null ? new Dictionary<string, string>() : new Dictionary<string, string>(tags),
            Type = type,
            EditableTags = tags is null ? []: new ObservableCollection<TagItem>(tags.Select(t => new TagItem { Key = t.Key, Value = t.Value }))
        };
    }

    public void ApplyEditableTags(IDictionary<string, string> targetTags)
    {
        targetTags.Clear();

        if (EditableTags is null || EditableTags.Count == 0)
            return;

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in EditableTags)
        {
            var key = tag.Key?.Trim();

            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (!seenKeys.Add(key))
                throw new InvalidOperationException("Duplicate tag keys are not allowed.");

            targetTags[key] = tag.Value;
        }
    }
}

public enum KeyVaultItemType
{
    Certificate = 0,
    Secret = 1,
    Key = 2,
    All = 3
}

public partial class TagItem : ObservableObject
{
    [ObservableProperty]
    public partial string Key { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
public sealed class ItemTypeOption
{
    public required string Label { get; init; }
    public required KeyVaultItemType Type { get; init; }
}
