using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Azure.ResourceManager.Resources.Models;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.UI.Xaml.XamlTypeInfo;

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
    public ObservableCollection<TagItem> EditableTags { get; set; } = new ObservableCollection<TagItem>();

    public DateTimeOffset? LastModifiedDate => UpdatedOn.HasValue ? UpdatedOn.Value.ToLocalTime() : CreatedOn?.ToLocalTime();
    public string? WhenLastModified => LastModifiedDate.HasValue ? FormatRelativeDate(LastModifiedDate.Value, true) : null;
    public string? WhenExpires => ExpiresOn.HasValue ? FormatRelativeDate(ExpiresOn.Value) : null;

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

        if (EditableTags != null)
        {
            foreach (var tag in EditableTags)
            {
                properties.Tags[tag.Key] = tag.Value;
            }
        }

        return properties;
    }

    public KeyProperties ToKeyProperties()
    {
        var properties = new KeyProperties(Id);
        properties.Enabled = Enabled;
        properties.NotBefore = NotBefore;
        properties.ExpiresOn = ExpiresOn;

        if (EditableTags != null)
        {
            foreach (var tag in EditableTags)
            {
                properties.Tags[tag.Key] = tag.Value;
            }
        }

        return properties;
    }

    public CertificateProperties ToCertificateProperties()
    {
        var properties = new CertificateProperties(Id);
        properties.Enabled = Enabled;
        if (EditableTags != null)
        {
            foreach (var tag in EditableTags)
            {
                properties.Tags[tag.Key] = tag.Value;
            }
        }

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

    internal static string? FormatRelativeDate(DateTimeOffset dateTimeOffset, bool isPast = false)
    {
        DateTimeOffset now = DateTimeOffset.Now;

        if (dateTimeOffset < now && !isPast)
        {
            return "Expired";
        }

        TimeSpan timeSpan = isPast ? now.Subtract(dateTimeOffset) : dateTimeOffset.Subtract(now);
        int dayDifference = (int)timeSpan.TotalDays;
        int secondDifference = (int)timeSpan.TotalSeconds;
        var weeks = Math.Ceiling((double)dayDifference / 7);
        var months = Math.Ceiling((double)dayDifference / 30);
        var years = Math.Round((double)dayDifference / 365);

        if (dayDifference < 0 || dayDifference >= 5000) return null;

        return (dayDifference, secondDifference) switch
        {
            (0, < 60) when isPast => "just now",
            (0, < 120) when isPast => "1 minute ago",
            (0, < 3600) when isPast => $"{Math.Floor((double)secondDifference / 60)} minutes ago",
            (0, < 7200) when isPast => "1 hour ago",
            (0, < 86400) when isPast => $"{Math.Floor((double)secondDifference / 3600)} hours ago",
            (0, < 86400) when !isPast => "in less than a day",
            (1, _) when isPast => "yesterday",
            (1, _) when !isPast => "tomorrow",
            ( < 7, _) => $"{(isPast ? string.Empty : "in ")}{dayDifference} days{(isPast ? " ago" : string.Empty)}",
            ( < 30, _) => $"{(isPast ? string.Empty : "in ")}{weeks} {(weeks == 1 ? "week" : "weeks")}{(isPast ? " ago" : string.Empty)}",
            ( < 366, _) => $"{(isPast ? string.Empty : "in ")}{months} {(months == 1 ? "month" : "months")}{(isPast ? " ago" : string.Empty)}",
            (_, _) => $"{(isPast ? string.Empty : "in ")}{years} {(years == 1 ? "year" : "years")}{(isPast ? " ago" : string.Empty)}"
        };


        
    }

    internal  ObservableCollection<TagItem> FromTagsToEditableTags()
    {
        var editableTags = new ObservableCollection<TagItem>();
        if (Tags?.Count > 0)
            foreach (var item in Tags)
            {
                editableTags.Add(new TagItem { Key = item.Key, Value = item.Value });
            }
        return editableTags;
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
