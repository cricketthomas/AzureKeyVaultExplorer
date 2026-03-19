using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;

namespace KeyVaultExplorer.Models;

public class KeyVaultContentsAmalgamation
{
    public KeyVaultItemType Type { get; set; }
    public Uri Id { get; set; }
    public Uri VaultUri { get; set; }
    public Uri ValueUri { get; set; }

    public string Name { get; set; }
    public string Version { get; set; }
    public string ContentType { get; set; }

    public DateTimeOffset? UpdatedOn { get; set; }

    public DateTimeOffset? CreatedOn { get; set; }

    public virtual bool? Enabled { get; set; }
    public virtual DateTimeOffset? NotBefore { get; set; }
    public virtual DateTimeOffset? ExpiresOn { get; set; }
    public virtual int? RecoverableDays { get; set; }
    public virtual string? RecoveryLevel { get; set; }

    public DateTimeOffset? LastModifiedDate => UpdatedOn.HasValue ? UpdatedOn.Value.ToLocalTime() : CreatedOn!.Value.ToLocalTime();

    public string? WhenLastModified => GetRelativeDateString(LastModifiedDate!.Value, true);
    public string? WhenExpires => ExpiresOn.HasValue ? GetRelativeDateString(ExpiresOn!.Value) : "";

    public SecretProperties SecretProperties { get; set; } = null!;
    public KeyProperties KeyProperties { get; set; } = null!;
    public CertificateProperties? CertificateProperties { get; set; } = null!;

    public IDictionary<string, string> Tags { get; set; } = null!;//new Dictionary<string, string>();

    public string[] TagValues => Tags is not null ? [.. Tags.Values] : [];

    public string[] TagKeys => Tags is not null ? [.. Tags.Keys] : [];

    public string TagValuesString => string.Join(", ", Tags?.Values ?? []);

    private string? GetRelativeDateString(DateTimeOffset dateTimeOffset, bool isPast = false)
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
            ( < 7, _) => $"{(isPast ? "" : "in ")}{dayDifference} days{(isPast ? " ago" : "")}",
            ( < 30, _) => $"{(isPast ? "" : "in ")}{weeks} {(weeks == 1 ? "week" : "weeks")}{(isPast ? " ago" : "")}",
            ( < 366, _) => $"{(isPast ? "" : "in ")}{months} {(months == 1 ? "month" : "months")}{(isPast ? " ago" : "")}",
            (_, _) => $"{(isPast ? "" : "in ")}{years} {(years == 1 ? "year" : "years")}{(isPast ? " ago" : "")}"
        };
    }
}

public enum KeyVaultItemType
{
    Certificate,
    Secret,
    Key,
    All
}