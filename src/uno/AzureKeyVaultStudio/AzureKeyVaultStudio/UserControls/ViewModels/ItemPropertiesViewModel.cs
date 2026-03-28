using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultStudio.Exceptions;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace AzureKeyVaultStudio.Presentation;

public partial class ItemPropertiesViewModel : ObservableObject
{
    private readonly AuthService _authService;
    public Window _currentWindow;
    private readonly INavigator _navigator;
    private readonly VaultService _vaultService;
    private readonly ILocalSettingsService _localSettings;
    private DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private DispatcherQueueTimer _debounceTimerClipboard = DispatcherQueue.GetForCurrentThread().CreateTimer();

    public INavigator Navigator => _navigator;
    public VaultService VaultService => _vaultService;
    public Guid MessengerToken { get; } = Guid.NewGuid();

    public ItemPropertiesViewModel(AuthService authService, VaultService vaultService, ILocalSettingsService localSettings, INavigator navigator)
    {
        _authService = authService;
        _localSettings = localSettings;
        _vaultService = vaultService;
        _navigator = navigator;
    }


    [ObservableProperty]
    public partial ObservableCollection<KeyVaultItemProperties> ItemPropertiesList { get; set; } = [];


    [ObservableProperty]
    public partial KeyVaultItemProperties SelectedVersionRow { get; set; }

    [ObservableProperty]
    public partial bool IsCertificate { get; set; } = false;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsKey { get; set; } = false;

    [ObservableProperty]
    public partial bool IsManaged { get; set; } = false;

    [ObservableProperty]
    public partial bool IsSecret { get; set; } = false;

    [ObservableProperty]
    public partial KeyVaultItemProperties OpenedItem { get; set; }

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SecretHidden { get; set; } = new('●', 20);

    [ObservableProperty]
    public partial string SecretPlainText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShouldShowValueCommand))]
    public partial bool ShowValue { get; set; } = false;

    [ObservableProperty]
    public partial string Title { get; set; } = "Properties";

    [ObservableProperty]
    public partial bool ShowCopyButton { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowEditVersionButton { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowNewVersionButton { get; set; } = false;

    [ObservableProperty]
    public partial bool EditEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDownloadKeyButton { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDownloadCertButton { get; set; } = false;

    [ObservableProperty]
    public partial bool ShowDownloadPfxButton { get; set; } = false;



    private void UpdateVisibilityFlags()
    {
        ShowCopyButton = (IsKey || IsSecret) && !IsCertificate;
        ShowEditVersionButton = IsSecret && !IsManaged;
        ShowNewVersionButton = IsSecret;
        EditEnabled = !IsManaged;
        ShowDownloadKeyButton = IsKey;
        ShowDownloadCertButton = IsCertificate;
        ShowDownloadPfxButton = IsCertificate;
    }

    private void ClearClipboard()
    {
        if (_localSettings?.GetValue(nameof(SettingsViewModel.ClearClipboardTimeout), 60) is int timeoutInSeconds)
        {
            _debounceTimerClipboard.Debounce(() =>
            {
                Clipboard.Clear();
            },
            interval: TimeSpan.FromSeconds(timeoutInSeconds));
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task Refresh()
    {
        if (OpenedItem is null) return;

        await GetPropertiesForKeyVaultValue(OpenedItem);
    }

    [RelayCommand]
    private async Task Copy(string? version = null)
    {
        if (OpenedItem is null || IsCertificate) return;
        try
        {
            string value = string.Empty;
            if (IsKey)
            {
                var key = await _vaultService.GetKey(OpenedItem.VaultUri, OpenedItem.Name);
                if (key.KeyType == KeyType.Rsa)
                {
                    using var rsa = key.Key.ToRSA();
                    var publicKey = rsa.ExportRSAPublicKey();
                    string pem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(publicKey) + "\n-----END PUBLIC KEY-----";
                    value = pem;
                }
            }

            if (IsSecret)
            {
                KeyVaultSecret? sv = await _vaultService.GetSecret(OpenedItem.VaultUri, OpenedItem.Name, version);
                value = sv.Value;
            }

            var dataPackage = new DataPackage();
            dataPackage.SetText(value);
            Clipboard.SetContent(dataPackage);

            ClearClipboard();
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Warning,
                Message = ex.Message,
                Title = "Insufficient Privileges"
            }));
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Error,
                Message = ex.Message,
                Title = "Unknown Error"
            }));
        }
    }


    [RelayCommand]
    private async Task CopyVersionUrl(KeyVaultItemProperties item)
    {
        if (OpenedItem is null) return;
        try
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(item.Id.ToString());
            Clipboard.SetContent(dataPackage);
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Error,
                Message = ex.Message,
                Title = "Unknown Error"
            }));
        }
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = true, AllowConcurrentExecutions = false)]
    private async Task Download(string exportType)
    {
        try
        {
            if (exportType == "Key")
            {
                var key = await _vaultService.GetKey(OpenedItem.VaultUri, OpenedItem.Name);
                using var rsa = key.Key.ToRSA();
                var publicKey = rsa.ExportSubjectPublicKeyInfo();
                string pem =
                    "-----BEGIN PUBLIC KEY-----\n" +
                    Convert.ToBase64String(publicKey, Base64FormattingOptions.None) +
                    "\n-----END PUBLIC KEY-----";
                await SaveFile(OpenedItem.Name, content: pem, ext: "pem");
            }
            else
            {
                var certificateWithPolicy = await _vaultService.GetCertificate(OpenedItem.VaultUri, OpenedItem.Name);
                // Create X.509 certificate from bytes
                var certificate = X509CertificateLoader.LoadCertificate(certificateWithPolicy.Cer);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("-----BEGIN CERTIFICATE-----");
                var ext = "cer";
                if (exportType == nameof(X509ContentType.Cert))
                {
                    sb.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Cert), Base64FormattingOptions.None));
                }
                else if (exportType == nameof(X509ContentType.Pfx))
                {
                    ext = "pfx";
                    sb.AppendLine(Convert.ToBase64String(certificate.Export(X509ContentType.Pfx), Base64FormattingOptions.None));
                }
                sb.AppendLine("-----END CERTIFICATE-----");
                await SaveFile(OpenedItem.Name, content: sb.ToString(), ext: ext);
            }
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Warning,
                Message = ex.Message,
                Title = "Insufficient Privileges"
            }));
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
            {
                Severity = InfoBarSeverity.Error,
                Message = ex.Message,
                Title = "Unknown Error"
            }));
        }
    }


    public async Task GetPropertiesForKeyVaultValue(KeyVaultItemProperties model)
    {
        switch (model.Type)
        {
            case KeyVaultItemType.Certificate:
                var certificateProperties = await _vaultService.GetCertificateProperties(model.VaultUri, model.Name);
                var latestCert = Enumerable.MaxBy(certificateProperties, x => x.UpdatedOn)!;
                ItemPropertiesList = new ObservableCollection<KeyVaultItemProperties>(KeyVaultItemProperties.FromCertificateProperties(certificateProperties));
                IsEnabled = latestCert.Enabled ?? false;
                IsCertificate = true;
                model = KeyVaultItemProperties.FromCertificateProperties(latestCert);
                break;

            case KeyVaultItemType.Key:
                var keyPropertiesList = await _vaultService.GetKeyProperties(model.VaultUri, model.Name);
                var latestKey = Enumerable.MaxBy(keyPropertiesList, x => x.UpdatedOn)!;
                IsManaged = latestKey.Managed;
                IsEnabled = latestKey.Enabled ?? false;
                ItemPropertiesList = new ObservableCollection<KeyVaultItemProperties>(KeyVaultItemProperties.FromKeyProperties(keyPropertiesList));
                IsKey = true;
                model = KeyVaultItemProperties.FromKeyProperties(latestKey);
                break;

            case KeyVaultItemType.Secret:
                var secretPropertiesList = await _vaultService.GetSecretProperties(model.VaultUri, model.Name);
                var latestSecret = Enumerable.MaxBy(secretPropertiesList, x => x.UpdatedOn)!;
                IsManaged = latestSecret.Managed;
                IsEnabled = latestSecret.Enabled ?? false;
                ItemPropertiesList = new ObservableCollection<KeyVaultItemProperties>(KeyVaultItemProperties.FromSecretProperties(secretPropertiesList));
                IsSecret = true;
                model = KeyVaultItemProperties.FromSecretProperties(latestSecret);
                break;

            default:
                IsSecret = false;
                IsCertificate = false;
                IsKey = false;
                break;
        }
        OpenedItem = model;
        UpdateVisibilityFlags();

        Title = $"{model.Type} {model.Name} Properties";
    }

    [RelayCommand]
    private async Task OpenInAzure()
    {
        if (OpenedItem is null) return;
        var uri = new Uri($"https://portal.azure.com/#@{_authService.TenantName}/asset/Microsoft_Azure_KeyVault/{OpenedItem.Type}/{OpenedItem.Id}");
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    private async Task SaveFile(string fileName, string ext, string content)
    {
        var fileSavePicker = new FileSavePicker();
        fileSavePicker.SuggestedStartLocation = PickerLocationId.Downloads;
        fileSavePicker.SuggestedFileName = $"{fileName}.{ext}";
        fileSavePicker.FileTypeChoices.AddRange(new Dictionary<string, IList<string>>
        {
            { "PEM File", new List<string> { ".pem" } },
            { "CER File", new List<string> { ".cer" } },
            { "PFX File", new List<string> { ".pfx" } },
            { "Plain Text", new List<string> { ".txt", ".text" } }
        });

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_currentWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);

        StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
        if (saveFile != null)
        {
            try
            {

                await File.WriteAllTextAsync(saveFile.Path, content, Encoding.UTF8);

                WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
                {
                    Severity = InfoBarSeverity.Success,
                    Message = $"File saved successfully to {saveFile.Name}",
                    Title = "Export Complete"
                }));
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
                {
                    Severity = InfoBarSeverity.Error,
                    Message = $"Failed to save file: {exception.Message}",
                    Title = "Export Failed"
                }));
            }

        }
        else
        {
            Debug.WriteLine("No file was picked or the dialog was cancelled.");
        }
    }

    [RelayCommand]
    private async Task ShouldShowValue(bool val)
    {
        try
        {
            if (IsSecret && val && IsEnabled)
            {
                var s = await Task.Run(async () =>
                {
                    return await _vaultService.GetSecret(keyVaultUri: OpenedItem.VaultUri, name: OpenedItem.Name).ConfigureAwait(false);
                });
                _dispatcherQueue.TryEnqueue(() =>
               {
                   SecretPlainText = s.Value;
               });
            }
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            //_notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
        catch (Exception ex)
        {
            //_notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
        }
    }
}
