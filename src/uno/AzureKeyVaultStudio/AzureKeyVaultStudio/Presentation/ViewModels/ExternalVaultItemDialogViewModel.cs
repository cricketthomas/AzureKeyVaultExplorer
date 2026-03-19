using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation.ViewModels;

public partial class ExternalVaultItemDialogViewModel : ObservableValidator
{
    private readonly VaultService _vaultService;
    public Guid MessengerToken { get; } = Guid.NewGuid();

    public ExternalVaultItemDialogViewModel()
    {
        _vaultService = (Application.Current as App).Host?.Services?.GetRequiredService<VaultService>();
    }

    [Required]
    [ObservableProperty]
    public partial string ExternalItemVaultName { get; set; } = "";

    // todo use localiozation
    public List<KeyValuePair<string, KeyVaultItemType>> ItemTypeOptions { get; } =
[
        new("Certificate", KeyVaultItemType.Certificate),
        new("Secret", KeyVaultItemType.Secret),
        new("Key", KeyVaultItemType.Key),
];

    [Required]
    [ObservableProperty]
    public partial KeyValuePair<string, KeyVaultItemType> SelectedItemType { get; set; } = new("Secret", KeyVaultItemType.Secret);

    [Required]
    [ObservableProperty]
    [Url(ErrorMessage = "Invalid URL format")]
    public partial string ExternalItemVaultUri { get; set; }

    [RelayCommand]
    private void Validate()
    {
        this.ValidateAllProperties();
    }

    [RelayCommand]
    private async Task Submit()
    {
        var itemUri = new Uri(ExternalItemVaultUri);
        IReadOnlyList<KeyVaultItemProperties> item;
        try
        {
            if (SelectedItemType.Value == KeyVaultItemType.Secret)
                item = KeyVaultItemProperties.FromSecretProperties(await _vaultService.GetSecretProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else if (SelectedItemType.Value == KeyVaultItemType.Key)
                item = KeyVaultItemProperties.FromKeyProperties(await _vaultService.GetKeyProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else if (SelectedItemType.Value == KeyVaultItemType.Certificate)
                item = KeyVaultItemProperties.FromCertificateProperties(await _vaultService.GetCertificateProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else
                throw new InvalidOperationException("Invalid item type selected");

            var latestItem = item.FirstOrDefault();
            var kvAmalgamation = new KeyVaultItemProperties
            {
                VaultUri = latestItem.VaultUri,
                Type = SelectedItemType.Value,
                Name = ExternalItemVaultName,
                Id = latestItem.Id,
                Version = latestItem.Version,
                ContentType = latestItem.ContentType,
                CreatedOn = latestItem.CreatedOn,
                UpdatedOn = latestItem.UpdatedOn,
                Enabled = latestItem.Enabled,
                ExpiresOn = latestItem.ExpiresOn,
                NotBefore = latestItem.NotBefore,
                RecoverableDays = latestItem.RecoverableDays,
                RecoveryLevel = latestItem.RecoveryLevel,
                Tags = latestItem.Tags,
                KeyProperties = latestItem.KeyProperties,
                SecretProperties = latestItem.SecretProperties,
                CertificateProperties = latestItem.CertificateProperties
            };

            WeakReferenceMessenger.Default.Send(new OpenItemDetailsWindowMessage(kvAmalgamation));
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ShowValidationErrorMessage(ex.Message), MessengerToken);
            Debug.WriteLine($"Error fetching Key Vault resource: {ex.Message}");
        }
    }
}
