using System.ComponentModel.DataAnnotations;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;

namespace AzureKeyVaultStudio.UserControls.ViewModels;

public partial class NewVersionViewModel : ObservableValidator
{
    private readonly AuthService _authService;

    private readonly VaultService _vaultService;

    private readonly IStringLocalizer _localizer;
    public Guid MessengerToken { get; } = Guid.NewGuid();

    [ObservableProperty]
    public partial TimeSpan ExpiresOnTimespan { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = false;

    [ObservableProperty]
    public partial bool IsEdit { get; set; } = false;

    [ObservableProperty]
    public partial bool IsNew { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Location))]
    public partial KeyVaultItemProperties ItemPropertiesModel { get; set; } = new();

    [ObservableProperty]
    public partial TimeSpan NotBeforeTimespan { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(SecretNameError))]
    [Length(1, 127, ErrorMessage = "The field {0} must be between {1} and {2} characters long.")]
    [RegularExpression(@"^[a-zA-Z0-9\-]+$", ErrorMessage = "The field {0} is not a valid name")]
    public partial string SecretName { get; set; } = "";

    [Required]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyPropertyChangedFor(nameof(SecretValueError))]
    public partial string SecretValue { get; set; } = "";

    [ObservableProperty]
    public partial Uri VaultUri { get; set; }

    public bool SecretNameError => GetErrors(nameof(SecretName)).Any();
    public bool SecretValueError => GetErrors(nameof(SecretValue)).Any();

    public NewVersionViewModel()
    {
        var services = (Application.Current as App)?.Host?.Services!;
        _authService = services.GetRequiredService<AuthService>();
        _vaultService = services.GetRequiredService<VaultService>();
        _localizer = services.GetRequiredService<IStringLocalizer>();
    }

    [ObservableProperty]
    public partial bool HasActivationDateChecked { get; set; }

    [ObservableProperty]
    public partial bool HasExpirationDateChecked { get; set; }

    public string? Identifier => ItemPropertiesModel?.Id?.ToString();
    public string? Location => ItemPropertiesModel?.VaultUri.ToString();
    public IEnumerable<ValidationResult> ErrorText => this.GetErrors();

    public bool ValidateForSubmit()
    {
        ValidateAllProperties();
        OnPropertyChanged(nameof(SecretNameError));
        OnPropertyChanged(nameof(SecretValueError));
        return !HasErrors;
    }

    [RelayCommand]
    private async Task ShowErrors(XamlRoot xamlRoot)
    {
        string message = string.Join(Environment.NewLine, GetErrors().Select(e => e.ErrorMessage));
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            XamlRoot = xamlRoot,
            CloseButtonText = "Cancel",
            RequestedTheme = (xamlRoot.Content as Control)?.RequestedTheme ?? ElementTheme.Default
        };
        await dialog.ShowAsync();
    }

    [RelayCommand]
    public async Task SaveSecretDetailsChanges()
    {
        if (ItemPropertiesModel.NotBefore.HasValue && HasActivationDateChecked)
            ItemPropertiesModel.NotBefore = ItemPropertiesModel.NotBefore.Value.Date + (NotBeforeTimespan);
        else
            ItemPropertiesModel.NotBefore = null;

        if (ItemPropertiesModel.ExpiresOn.HasValue && HasExpirationDateChecked)
            ItemPropertiesModel.ExpiresOn = ItemPropertiesModel.ExpiresOn.Value.Date + (ExpiresOnTimespan);
        else
            ItemPropertiesModel.ExpiresOn = null;

        try
        {
            var updatedProps = await _vaultService.UpdateSecret(ItemPropertiesModel.ToSecretProperties(), ItemPropertiesModel.VaultUri);
            ItemPropertiesModel = KeyVaultItemProperties.FromSecretProperties(updatedProps);

            WeakReferenceMessenger.Default.Send(new ShowSuccessOperationMessage(ItemPropertiesModel.Name, true), MessengerToken);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ShowValidationErrorMessage(ex.Message), MessengerToken);
        }
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = false, IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task NewSecretVersion(CancellationToken token)
    {
        if (!ValidateForSubmit())
        {
            return;
        }
        try
        {
            var newSecret = new KeyVaultSecret(SecretName ?? ItemPropertiesModel.Name, SecretValue);
            if (ItemPropertiesModel.NotBefore.HasValue)
                newSecret.Properties.NotBefore = ItemPropertiesModel.NotBefore.Value.Date + (NotBeforeTimespan);

            if (ItemPropertiesModel.ExpiresOn.HasValue)
                newSecret.Properties.ExpiresOn = ItemPropertiesModel.ExpiresOn.Value.Date + (ExpiresOnTimespan);

            newSecret.Properties.ContentType = ItemPropertiesModel.ContentType;


            ItemPropertiesModel.ApplyEditableTags(newSecret.Properties.Tags);

            token.ThrowIfCancellationRequested();

            var newVersion = await _vaultService.CreateSecret(newSecret, !IsNew ? ItemPropertiesModel.VaultUri : VaultUri);
            var properties = (await _vaultService.GetSecretProperties(newVersion.Properties.VaultUri, newVersion.Name)).First();
            ItemPropertiesModel = KeyVaultItemProperties.FromSecretProperties(properties);
            WeakReferenceMessenger.Default.Send(new ShowSuccessOperationMessage(newSecret.Name, false), MessengerToken);
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Success,
                Message = $"The secret '{newVersion.Name}' has been created.",
                Title = "Success"
            }));
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ShowValidationErrorMessage(ex.Message), MessengerToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnItemPropertiesModelChanging(KeyVaultItemProperties value)
    {
        SecretName = value.Name;
        HasActivationDateChecked = value.NotBefore.HasValue;
        HasExpirationDateChecked = value.ExpiresOn.HasValue;
        ExpiresOnTimespan = value is not null && value.ExpiresOn.HasValue ? value.ExpiresOn.Value.LocalDateTime.TimeOfDay : TimeSpan.Zero;
        NotBeforeTimespan = value is not null && value.NotBefore.HasValue ? value.NotBefore.Value.LocalDateTime.TimeOfDay : TimeSpan.Zero;
    }

    partial void OnHasActivationDateCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue is false)
        {
            ItemPropertiesModel.NotBefore = null;
        }
    }

    partial void OnHasExpirationDateCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue is false)
        {
            ItemPropertiesModel.ExpiresOn = null;
        }
    }
}
