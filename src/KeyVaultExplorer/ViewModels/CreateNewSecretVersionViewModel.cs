using Azure.Security.KeyVault.Secrets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Services;
using KeyVaultExplorer.Validations;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class CreateNewSecretVersionViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    private readonly VaultService _vaultService;

    private NotificationViewModel _notificationViewModel;

    [ObservableProperty]
    private TimeSpan? expiresOnTimespan;

    [ObservableProperty]
    private bool isBusy = false;

    [ObservableProperty]
    private bool isEdit = false;

    [ObservableProperty]
    private bool isNew = false;

    //[ObservableProperty]
    //private ObservableCollection<KeyVaultResource> keyVaultResources = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Location))]
    private SecretProperties keyVaultSecretModel;

    [ObservableProperty]
    private TimeSpan? notBeforeTimespan;

    //[ObservableProperty]
    //private List<KvResourceGroupModel> resourceGroupItems;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(SecretNameValidationAttribute), nameof(SecretNameValidationAttribute.ValidateName))]
    private string secretName;

    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    private string secretValue;

    [ObservableProperty]
    private Uri vaultUri;

    public CreateNewSecretVersionViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
        ValidateAllProperties();
    }

    [ObservableProperty]
    public bool hasActivationDateChecked;

    [ObservableProperty]
    public bool hasExpirationDateChecked;

    public string? Identifier => KeyVaultSecretModel?.Id?.ToString();
    public string? Location => KeyVaultSecretModel?.VaultUri.ToString();

    [RelayCommand]
    public async Task EditDetails()
    {
        if (KeyVaultSecretModel.NotBefore.HasValue && HasActivationDateChecked)
            KeyVaultSecretModel.NotBefore = KeyVaultSecretModel.NotBefore.Value.Date + (NotBeforeTimespan ?? TimeSpan.Zero);
        else
            KeyVaultSecretModel.NotBefore = null;

        if (KeyVaultSecretModel.ExpiresOn.HasValue && HasExpirationDateChecked)
            KeyVaultSecretModel.ExpiresOn = KeyVaultSecretModel.ExpiresOn.Value.Date + (ExpiresOnTimespan ?? TimeSpan.Zero);
        else
            KeyVaultSecretModel.ExpiresOn = null;

        var updatedProps = await _vaultService.UpdateSecret(KeyVaultSecretModel, KeyVaultSecretModel.VaultUri);
        KeyVaultSecretModel = updatedProps;
    }

    [RelayCommand]
    private async Task NewVersion()
    {
        var newSecret = new KeyVaultSecret(SecretName ?? KeyVaultSecretModel.Name, SecretValue);
        if (KeyVaultSecretModel.NotBefore.HasValue)
            newSecret.Properties.NotBefore = KeyVaultSecretModel.NotBefore.Value.Date + (NotBeforeTimespan ?? TimeSpan.Zero);

        if (KeyVaultSecretModel.ExpiresOn.HasValue)
            newSecret.Properties.ExpiresOn = KeyVaultSecretModel.ExpiresOn.Value.Date + (ExpiresOnTimespan ?? TimeSpan.Zero);

        newSecret.Properties.ContentType = KeyVaultSecretModel.ContentType;

        foreach (var tag in KeyVaultSecretModel.Tags)
            newSecret.Properties.Tags.Add(tag.Key, tag.Value);

        var newVersion = await _vaultService.CreateSecret(newSecret, !IsNew ? KeyVaultSecretModel.VaultUri : VaultUri);
        var properties = (await _vaultService.GetSecretProperties(newVersion.Properties.VaultUri, newVersion.Name)).First();
        KeyVaultSecretModel = properties;
    }

  

    partial void OnKeyVaultSecretModelChanging(SecretProperties value)
    {
        SecretName = value.Name;
        HasActivationDateChecked = value.NotBefore.HasValue;
        HasExpirationDateChecked = value.ExpiresOn.HasValue;
        ExpiresOnTimespan = value is not null && value.ExpiresOn.HasValue ? value?.ExpiresOn.Value.LocalDateTime.TimeOfDay : null;
        NotBeforeTimespan = value is not null && value.NotBefore.HasValue ? value?.NotBefore.Value.LocalDateTime.TimeOfDay : null;
    }

    partial void OnHasActivationDateCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue is false)
        {
            KeyVaultSecretModel.NotBefore = null;
        }
    }

    partial void OnHasExpirationDateCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue is false)
        {
            KeyVaultSecretModel.ExpiresOn = null;
        }
    }

    //public async Task<ObservableCollection<SubscriptionDataItem>> GetAvailableSubscriptions()
    //{
    //    var subscriptions = new List<SubscriptionDataItem>();
    //    await foreach (var item in _vaultService.GetAllSubscriptions())
    //    {
    //        subscriptions.Add(new SubscriptionDataItem
    //        {
    //            Data = item.SubscriptionResource.Data,
    //            Resource = item.SubscriptionResource
    //        });
    //    }
    //    return new ObservableCollection<SubscriptionDataItem>(subscriptions);
    //}
    //private readonly string[] _seenSubscriptions = [];
    //[RelayCommand]
    //private void SelectedSubscriptionChanged(SubscriptionDataItem value)
    //{
    //    if (value is not null && !_seenSubscriptions.Contains(value.Data.SubscriptionId))
    //    {
    //        Dispatcher.UIThread.Post(async () =>
    //        {
    //            var keyVaults = _vaultService.GetKeyVaultsBySubscription(new KvSubscriptionModel { Subscription = value.Resource });
    //            await foreach (var item in keyVaults)
    //            {
    //                KeyVaultResources.Add(item);
    //            }
    //            _seenSubscriptions.Append(value.Data.SubscriptionId);
    //        }, DispatcherPriority.ApplicationIdle);
    //    }
    //}
}