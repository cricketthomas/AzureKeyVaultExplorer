using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeyVaultExplorer.Views;
using KeyVaultExplorer.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Azure.Security.KeyVault.Secrets;
using System;
using Azure.Security.KeyVault.Keys;

namespace KeyVaultExplorer.ViewModels;

public partial class CreateNewKeyVersionViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isBusy = false;

    [ObservableProperty]
    private bool isEdit = false;

    public bool HasActivationDate => KeyVaultKeyModel is not null && KeyVaultKeyModel.NotBefore.HasValue;
    public bool HasExpirationDate => KeyVaultKeyModel is not null && KeyVaultKeyModel.ExpiresOn.HasValue;

    [ObservableProperty]
    private string value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Location))]
    [NotifyPropertyChangedFor(nameof(HasActivationDate))]
    [NotifyPropertyChangedFor(nameof(HasExpirationDate))]
    private KeyProperties keyVaultKeyModel;

    [ObservableProperty]
    private TimeSpan? expiresOnTimespan;

    [ObservableProperty]
    private TimeSpan? notBeforeTimespan;

    public string? Location => KeyVaultKeyModel?.VaultUri.ToString();
    public string? Identifier => KeyVaultKeyModel?.Id.ToString();

    private readonly AuthService _authService;
    private readonly VaultService _vaultService;
    private NotificationViewModel _notificationViewModel;

    public CreateNewKeyVersionViewModel()
    {
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
    }

    [RelayCommand]
    public async Task EditDetails()
    {
        if (KeyVaultKeyModel.NotBefore.HasValue)
            KeyVaultKeyModel.NotBefore = KeyVaultKeyModel.NotBefore.Value.Date + (NotBeforeTimespan.HasValue ? NotBeforeTimespan.Value : TimeSpan.Zero);

        if (KeyVaultKeyModel.ExpiresOn.HasValue)
            KeyVaultKeyModel.ExpiresOn = KeyVaultKeyModel.ExpiresOn.Value.Date + (ExpiresOnTimespan.HasValue ? ExpiresOnTimespan.Value : TimeSpan.Zero);

        //var updatedProps = await _vaultService.UpdateSecret(KeyVaultKeyModel, KeyVaultKeyModel.VaultUri);
        //KeyVaultKeyModel = updatedProps;
    }

    [RelayCommand]
    public async Task NewVersion()
    {
        var newSecret = new KeyVaultSecret(KeyVaultKeyModel.Name, Value);
        if (KeyVaultKeyModel.NotBefore.HasValue)
            newSecret.Properties.NotBefore = KeyVaultKeyModel.NotBefore.Value.Date + (NotBeforeTimespan.HasValue ? NotBeforeTimespan.Value : TimeSpan.Zero);

        if (KeyVaultKeyModel.ExpiresOn.HasValue)
            newSecret.Properties.ExpiresOn = KeyVaultKeyModel.ExpiresOn.Value.Date + (ExpiresOnTimespan.HasValue ? ExpiresOnTimespan.Value : TimeSpan.Zero);


        var newVersion = await _vaultService.CreateSecret(newSecret, KeyVaultKeyModel.VaultUri);
        var properties = (await _vaultService.GetSecretProperties(newVersion.Properties.VaultUri, newVersion.Name)).First();
        //KeyVaultKeyModel = properties;
    }

    partial void OnKeyVaultKeyModelChanging(KeyProperties model)
    {
        ExpiresOnTimespan = model is not null && model.ExpiresOn.HasValue ? model?.ExpiresOn.Value.LocalDateTime.TimeOfDay : null;
        NotBeforeTimespan = model is not null && model.NotBefore.HasValue ? model?.NotBefore.Value.LocalDateTime.TimeOfDay : null;
    }
}