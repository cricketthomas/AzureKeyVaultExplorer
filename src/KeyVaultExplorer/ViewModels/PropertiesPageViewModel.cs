using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using KeyVaultExplorer.Exceptions;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class PropertiesPageViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly ClipboardService _clipboardService;
    private readonly NotificationViewModel _notificationViewModel;
    private readonly StorageProviderService _storageService;
    private readonly VaultService _vaultService;
    private SettingsPageViewModel _settingsPageViewModel;

    [ObservableProperty]
    private ObservableCollection<CertificateProperties> certificatePropertiesList;

    [ObservableProperty]
    private bool isCertificate = false;

    [ObservableProperty]
    private bool isEnabled = false;

    [ObservableProperty]
    private bool isKey = false;

    [ObservableProperty]
    private bool isManaged = false;

    [ObservableProperty]
    private bool isSecret = false;

    [ObservableProperty]
    private ObservableCollection<KeyProperties> keyPropertiesList;

    [ObservableProperty]
    private KeyVaultContentsAmalgamation openedItem;

    [ObservableProperty]
    private string secretHidden = new('*', 20);

    [ObservableProperty]
    private string secretPlainText = "";

    [ObservableProperty]
    private ObservableCollection<SecretProperties> secretPropertiesList;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShouldShowValueCommand))]
    private bool showValue = false;

    [ObservableProperty]
    private string title = "Properties";

    public PropertiesPageViewModel()
    {
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _clipboardService = Defaults.Locator.GetRequiredService<ClipboardService>();
        _storageService = Defaults.Locator.GetRequiredService<StorageProviderService>();
    }

    public PropertiesPageViewModel(KeyVaultContentsAmalgamation model)
    {
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _settingsPageViewModel = Defaults.Locator.GetRequiredService<SettingsPageViewModel>();
        _clipboardService = Defaults.Locator.GetRequiredService<ClipboardService>();
        _storageService = Defaults.Locator.GetRequiredService<StorageProviderService>();  
        _notificationViewModel = new NotificationViewModel();// Defaults.Locator.GetRequiredService<NotificationViewModel>();
        OpenedItem = model;
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await GetPropertiesForKeyVaultValue(model);
        }, priority: DispatcherPriority.Normal);
    }

    public async Task ClearClipboardAsync()
    {
        await Task.Delay(_settingsPageViewModel.ClearClipboardTimeout * 1000); // convert to seconds
        await _clipboardService.ClearAsync();
    }

    [RelayCommand]
    private async Task Copy()
    {
        if (OpenedItem is null || IsCertificate) return;
        try
        {
            string value = string.Empty;
            if (IsKey)
            {
                var key = await _vaultService.GetKey(OpenedItem.KeyProperties.VaultUri, OpenedItem.KeyProperties.Name);
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
                var sv = await _vaultService.GetSecret(OpenedItem.SecretProperties.VaultUri, OpenedItem.SecretProperties.Name);
                value = sv.Value;
            }

            // TODO: figure out why set data object async fails here.
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, value);
            await _clipboardService.SetTextAsync(value);
            _ = Task.Run(async () => await ClearClipboardAsync().ConfigureAwait(false));
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
        catch (Exception ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
        }
    }

    [RelayCommand]
    private async Task Download(string exportType)
    {
        try
        {
            if (exportType == "Key")
            {
                var key = await _vaultService.GetKey(OpenedItem.KeyProperties.VaultUri, OpenedItem.KeyProperties.Name);
                using var rsa = key.Key.ToRSA();
                var publicKey = rsa.ExportRSAPublicKey();
                string pem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(publicKey) + "\n-----END PUBLIC KEY-----";
                SaveFile(OpenedItem.KeyProperties.Name, content: pem, ext: "pem");
            }
            else
            {
                var certificateWithPolicy = await _vaultService.GetCertificate(OpenedItem.CertificateProperties.VaultUri, OpenedItem.CertificateProperties.Name);
                // Create X.509 certificate from bytes
                var certificate = new X509Certificate2(certificateWithPolicy.Cer);
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
                SaveFile(OpenedItem.CertificateProperties.Name, content: sb.ToString(), ext: ext);
            }
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
        catch (Exception ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
        }
    }

    [RelayCommand]
    private async Task EditVersion()
    {
        try
        {
            var dialog = new ContentDialog()
            {
                Title = "Edit " + (IsKey ? "Key" : IsSecret ? "Secret" : "Certificate"),
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = "Apply Changes",
                DefaultButton = ContentDialogButton.Primary,
                CloseButtonText = "Cancel",
                MinWidth = 650
            };

            if (IsSecret)
            {
                var currentItem = SecretPropertiesList.OrderByDescending(x => x.CreatedOn).First();
                var viewModel = new CreateNewSecretVersionViewModel();
                bool? isEnabledSecret = currentItem.Enabled;

                viewModel.KeyVaultSecretModel = currentItem;
                viewModel.IsEdit = true;
                dialog.PrimaryButtonClick += async (sender, args) =>
                {
                    var def = args.GetDeferral();
                    try
                    {
                        await viewModel.EditDetailsCommand.ExecuteAsync(null);
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification("Success", "The properties have been updated."));
                    }
                    catch (KeyVaultInsufficientPrivilegesException ex)
                    {
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
                    }
                    catch (Exception ex)
                    {
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
                    }
                    finally
                    {
                        def.Complete();
                    }
                };

                dialog.Content = new CreateNewSecretVersion()
                {
                    DataContext = viewModel
                };
            }
            var result = await dialog.ShowAsync();
        }
        catch (KeyVaultItemNotFoundException ex)
        {
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
    }

    private async Task GetPropertiesForKeyVaultValue(KeyVaultContentsAmalgamation model)
    {
        switch (model.Type)
        {
            case KeyVaultItemType.Certificate:
                var certificateProperties = await _vaultService.GetCertificateProperties(model.VaultUri, model.Name);
                CertificatePropertiesList = new ObservableCollection<CertificateProperties>(certificateProperties);
                IsEnabled = certificateProperties.First().Enabled ?? false;
                IsCertificate = true;
                break;

            case KeyVaultItemType.Key:
                var keyPropertiesList = new ObservableCollection<KeyProperties>(await _vaultService.GetKeyProperties(model.VaultUri, model.Name));
                IsManaged = keyPropertiesList.First().Managed;
                IsEnabled = keyPropertiesList.First().Enabled ?? false;
                KeyPropertiesList = new ObservableCollection<KeyProperties>(keyPropertiesList);
                IsKey = true;
                break;

            case KeyVaultItemType.Secret:
                var secretPropertiesList = new ObservableCollection<SecretProperties>(await _vaultService.GetSecretProperties(model.VaultUri, model.Name));
                IsManaged = secretPropertiesList.First().Managed;
                IsEnabled = secretPropertiesList.First().Enabled ?? false;
                SecretPropertiesList = new ObservableCollection<SecretProperties>(secretPropertiesList);
                IsSecret = true;
                break;

            default:
                IsSecret = false;
                IsCertificate = false;
                IsKey = false;
                break;
        }
        Title = $"{model.Type} {model.Name} Properties";
    }

    [RelayCommand]
    private async Task NewVersion()
    {
        try
        {
            var lifetime = App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

            if (IsSecret)
            {
                var currentItem = SecretPropertiesList.OrderByDescending(x => x.CreatedOn).First();
                var newVersion = new SecretProperties(currentItem.Id)
                {
                    Enabled = true,
                    ContentType = currentItem.ContentType,
                };

                foreach (var tag in currentItem.Tags)
                    newVersion.Tags.Add(tag.Key, tag.Value);

                var vm = new CreateNewSecretVersionViewModel();
                vm.KeyVaultSecretModel = newVersion;
                vm.SecretValue = (await _vaultService.GetSecret(kvUri: OpenedItem.SecretProperties.VaultUri, secretName: OpenedItem.SecretProperties.Name)).Value;

                var newVersionBtn = new TaskDialogButton("Create Secret", "CreateSecretButtonResult") { IsDefault = true, };
                newVersionBtn.Bind(TaskDialogButton.IsEnabledProperty, new Binding { Path = "!HasErrors", Mode = BindingMode.OneWay, FallbackValue = false, Source = vm, });

                var dialog = new TaskDialog()
                {
                    Title = "New Version",
                    XamlRoot = lifetime?.Windows.Last() as AppWindow,
                    Buttons = { newVersionBtn, TaskDialogButton.CancelButton, },
                    MinWidth = 650,
                    MinHeight = 500,
                    Content = new CreateNewSecretVersion() { DataContext = vm, },
                };

                newVersionBtn.Click += async (sender, args) =>
                {
                    try
                    {
                        await vm.NewVersionCommand.ExecuteAsync(null);
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification("Success", "The secret version has been created."));
                    }
                    catch (KeyVaultInsufficientPrivilegesException ex)
                    {
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
                    }
                    catch (Exception ex)
                    {
                        _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
                    }
                    finally
                    {
                    }
                };

                dialog.Content = new CreateNewSecretVersion() { DataContext = vm };
                var result = await dialog.ShowAsync(true);
            }
        }
        catch (KeyVaultItemNotFoundException ex)
        {
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
    }

    [RelayCommand]
    private void OpenInAzure()
    {
        if (OpenedItem is null) return;
        var uri = $"https://portal.azure.com/#@{_authService.TenantName}/asset/Microsoft_Azure_KeyVault/{OpenedItem.Type}/{OpenedItem.Id}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true, Verb = "open" });
    }

    private async void SaveFile(string fileName, string ext, string content)
    {
        var desktopFolder = await _storageService.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
        var file = await _storageService.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"Save {fileName}",
            SuggestedFileName = fileName,
            SuggestedStartLocation = desktopFolder,
            DefaultExtension = ext
        });

        if (file is not null)
        {
            await using var stream = await file.OpenWriteAsync();

            using var streamWriter = new StreamWriter(stream);
            await streamWriter.WriteLineAsync(content);
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
                    return await _vaultService.GetSecret(kvUri: OpenedItem.SecretProperties.VaultUri, secretName: OpenedItem.SecretProperties.Name).ConfigureAwait(false);
                });
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SecretPlainText = s.Value;
                });
            }
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Insufficient Privileges" });
        }
        catch (Exception ex)
        {
            _notificationViewModel.ShowPopup(new Avalonia.Controls.Notifications.Notification { Message = ex.Message, Title = "Error" });
        }
    }
}