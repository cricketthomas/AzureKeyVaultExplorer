using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation.ViewModels;

public partial class ExternalVaultItemDialogViewModel : ObservableValidator
{
    private readonly VaultService _vaultService;
    private readonly IStringLocalizer _localizer;
    public Guid MessengerToken { get; } = Guid.NewGuid();

    public ExternalVaultItemDialogViewModel()
    {
        _vaultService = (Application.Current as App).Host?.Services?.GetRequiredService<VaultService>();
        _localizer = (Application.Current as App).Host?.Services?.GetRequiredService<IStringLocalizer>();

        ItemTypeOption Key = new() { Label = _localizer?["KeyName"] ?? "Key", Type = KeyVaultItemType.Key };
        ItemTypeOption Secret = new() { Label = _localizer?["SecretName"] ?? "Secret", Type = KeyVaultItemType.Secret };
        ItemTypeOption Certificate = new() { Label = _localizer?["CertificateName"] ?? "Certificate", Type = KeyVaultItemType.Certificate };

        ItemTypeOptions = [Secret, Key, Certificate];

        SelectedItemType = Secret;
    }

    [Required]
    [ObservableProperty]
    public partial string ExternalItemVaultName { get; set; } = "";

    public List<ItemTypeOption> ItemTypeOptions { get; init; }

    [Required]
    [ObservableProperty]
    public partial ItemTypeOption SelectedItemType { get; set; }

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
            if (SelectedItemType.Type == KeyVaultItemType.Secret)
                item = KeyVaultItemProperties.FromSecretProperties(await _vaultService.GetSecretProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else if (SelectedItemType.Type == KeyVaultItemType.Key)
                item = KeyVaultItemProperties.FromKeyProperties(await _vaultService.GetKeyProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else if (SelectedItemType.Type == KeyVaultItemType.Certificate)
                item = KeyVaultItemProperties.FromCertificateProperties(await _vaultService.GetCertificateProperties(name: ExternalItemVaultName, keyVaultUri: itemUri));
            else
                throw new InvalidOperationException("Invalid item type selected");


            
            if (item is null|| item.Count == 0)
                throw new InvalidOperationException(_localizer["ExternalItemNotRetrievedExceptionMessage"] ?? "The item could not be retrieved.");

            var latestItem = item.FirstOrDefault();
            var kvAmalgamation = new KeyVaultItemProperties
            {
                VaultUri = latestItem.VaultUri,
                Type = SelectedItemType.Type,
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
