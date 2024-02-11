﻿using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using System.Diagnostics;
using System.Linq;

namespace kvexplorer.shared.Models;

public class KeyVaultContentsAmalgamation
{
    public KeyVaultItemType Type { get; set; }
    public Uri Id { get; set; }
    public Uri VaultUri { get; set; }
    public Uri ValueUri { get; set; }

    public string Name { get; set; }
    public string Version { get; set; }
    public string ContentType { get; set; }

    public DateTimeOffset? LastModifiedDate { get; set; }

    public SecretProperties SecretProperties { get; set; } = null!;

    public KeyProperties KeyProperties { get; set; } = null!;

    public CertificateProperties? CertificateProperties { get; set; } = null!;

    public IDictionary<string, string> Tags { get; set; } = null!;

    public string[] TagValues => Tags is not null ? [.. Tags.Values] : [];

    public string[] TagKeys => Tags is not null ? [.. Tags.Keys] : [];

    public string TagValuesString => string.Join(", ",Tags?.Values ?? []);


}

public enum KeyVaultItemType
{
    Certificate,
    Secret,
    Key,
    All
}