using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Windowing;
using KeyVaultExplorer.Exceptions;
using KeyVaultExplorer.Models;
using KeyVaultExplorer.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

//#if WINDOWS
//using Windows.Data.Xml.Dom;
//using Windows.UI.Notifications;
//#endif

namespace KeyVaultExplorer.ViewModels;

public partial class VaultPageViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    private readonly ClipboardService _clipboardService;

    private readonly VaultService _vaultService;

    private NotificationViewModel _notificationViewModel;

    private SettingsPageViewModel _settingsPageViewModel;
    public string VaultTotalString => VaultContents.Count == 0 || VaultContents.Count > 1 ? $"{VaultContents.Count} items" : "1 item";

    [ObservableProperty]
    private string authorizationMessage;

    [ObservableProperty]
    private bool hasAuthorizationError = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VaultTotalString))]
    private bool isBusy = false;

    [ObservableProperty]
    private string searchQuery;

    [ObservableProperty]
    private KeyVaultContentsAmalgamation selectedRow;

    [ObservableProperty]
    private TabStripItem selectedTab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VaultTotalString))]
    private ObservableCollection<KeyVaultContentsAmalgamation> vaultContents;

    [ObservableProperty]
    private Uri vaultUri;

    private readonly Lazy<Bitmap> BitmapImage;

    public VaultPageViewModel()
    {
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _authService = Defaults.Locator.GetRequiredService<AuthService>();
        _settingsPageViewModel = Defaults.Locator.GetRequiredService<SettingsPageViewModel>();
        _notificationViewModel = Defaults.Locator.GetRequiredService<NotificationViewModel>();
        _clipboardService = Defaults.Locator.GetRequiredService<ClipboardService>();
        vaultContents = [];
        BitmapImage = new Lazy<Bitmap>(() => LoadImage("avares://KeyVaultExplorer/Assets/AppIcon.ico"));

#if DEBUG
        for (int i = 0; i < 5; i++)
        {
            var sp = (new SecretProperties($"{i}_Demo__Key_Token") { ContentType = "application/json", Enabled = true, ExpiresOn = new System.DateTime(), });
            var item = new KeyVaultContentsAmalgamation
            {
                CreatedOn = new System.DateTime(),
                UpdatedOn = new System.DateTime(),
                Version = "version 1",
                VaultUri = new Uri("https://stackoverflow.com/"),
                ContentType = "application/json",
                Id = new Uri("https://stackoverflow.com/"),
                SecretProperties = sp
            };

            switch (i % 3)
            {
                case 0:
                    item.Name = $"{i}_Secret";
                    item.Type = KeyVaultItemType.Secret;
                    break;

                case 1:

                    item.Name = $"{i}__Key";
                    item.Type = KeyVaultItemType.Key;
                    break;

                case 2:
                    item.Name = $"{i}_Certificate";
                    item.Type = KeyVaultItemType.Key;
                    break;
            }
            VaultContents.Add(item);
        }
        _vaultContents = VaultContents;
#endif
    }

    public Bitmap LazyLoadedImage => BitmapImage.Value.CreateScaledBitmap(new Avalonia.PixelSize(24, 24), BitmapInterpolationMode.HighQuality);

    private static Bitmap LoadImage(string uri)
    {
        var asset = AssetLoader.Open(new Uri(uri));
        return new Bitmap(asset);
    }

    public Dictionary<KeyVaultItemType, bool> LoadedItemTypes { get; set; } = new() { };
    private IEnumerable<KeyVaultContentsAmalgamation> _vaultContents { get; set; } = [];

    public async Task ClearClipboardAsync()
    {
        await Task.Delay(_settingsPageViewModel.ClearClipboardTimeout * 1000); // convert to seconds
        await _clipboardService.ClearAsync();
    }

    public async Task FilterAndLoadVaultValueType(KeyVaultItemType item)
    {
        try
        {
            HasAuthorizationError = false;

            if (!LoadedItemTypes.ContainsKey(item))
            {
                IsBusy = true;

                switch (item)
                {
                    case KeyVaultItemType.Certificate:
                        await LoadAndMarkAsLoaded(GetCertificatesForVault, KeyVaultItemType.Certificate);
                        break;

                    case KeyVaultItemType.Key:
                        await LoadAndMarkAsLoaded(GetKeysForVault, KeyVaultItemType.Key);
                        break;

                    case KeyVaultItemType.Secret:
                        await LoadAndMarkAsLoaded(GetSecretsForVault, KeyVaultItemType.Secret);
                        break;

                    case KeyVaultItemType.All:
                        VaultContents.Clear();
                        var loadTasks = new List<Task>
                            {
                                LoadAndMarkAsLoaded(GetSecretsForVault, KeyVaultItemType.Secret),
                                LoadAndMarkAsLoaded(GetKeysForVault, KeyVaultItemType.Key),
                                LoadAndMarkAsLoaded(GetCertificatesForVault, KeyVaultItemType.Certificate)
                            };
                        await Task.WhenAny(loadTasks);
                        LoadedItemTypes.TryAdd(KeyVaultItemType.All, true);
                        break;

                    default:
                        break;
                }
            }
        }
        catch (Exception ex) when (ex.Message.Contains("403"))
        {
            //_notificationViewModel.AddMessage(new Avalonia.Controls.Notifications.Notification
            //{
            //    Message = string.Concat(ex.Message.AsSpan(0, 90), "..."),
            //    Title = $"Insufficient Privileges on type '{item}'",
            //    Type = NotificationType.Error,
            //});
            if (!item.HasFlag(KeyVaultItemType.All))
            {
                HasAuthorizationError = true;
                AuthorizationMessage = ex.Message;
            }
        }
        catch { }
        finally
        {
            var contents = item == KeyVaultItemType.All ? _vaultContents : _vaultContents.Where(x => item == x.Type);

            VaultContents = KeyVaultFilterHelper.FilterByQuery(contents, SearchQuery, item => item.Name, item => item.Tags, item => item.ContentType);

            await DelaySetIsBusy(false);
        }
    }

    public async Task GetCertificatesForVault(Uri kvUri)
    {
        var certs = _vaultService.GetVaultAssociatedCertificates(kvUri);
        await foreach (var val in certs)
        {
            VaultContents.Add(new KeyVaultContentsAmalgamation
            {
                Name = val.Name,
                Id = val.Id,
                Type = KeyVaultItemType.Certificate,
                VaultUri = val.VaultUri,
                ValueUri = val.Id,
                Version = val.Version,
                CertificateProperties = val,
                Tags = val.Tags,
                UpdatedOn = val.UpdatedOn,
                CreatedOn = val.CreatedOn,
                ExpiresOn = val.ExpiresOn,
                Enabled = val.Enabled,
                NotBefore = val.NotBefore,
                RecoverableDays = val.RecoverableDays,
                RecoveryLevel = val.RecoveryLevel
            });
        }
        _vaultContents = VaultContents;
    }

    public async Task GetKeysForVault(Uri kvUri)
    {
        var keys = _vaultService.GetVaultAssociatedKeys(kvUri);
        await foreach (var val in keys)
        {
            VaultContents.Add(new KeyVaultContentsAmalgamation
            {
                Name = val.Name,
                Id = val.Id,
                Type = KeyVaultItemType.Key,
                VaultUri = val.VaultUri,
                ValueUri = val.Id,
                Version = val.Version,
                KeyProperties = val,
                Tags = val.Tags,
                UpdatedOn = val.UpdatedOn,
                CreatedOn = val.CreatedOn,
                ExpiresOn = val.ExpiresOn,
                Enabled = val.Enabled,
                NotBefore = val.NotBefore,
                RecoverableDays = val.RecoverableDays,
                RecoveryLevel = val.RecoveryLevel
            });
        }
        _vaultContents = VaultContents;
    }

    public async Task GetSecretsForVault(Uri kvUri)
    {
        var values = _vaultService.GetVaultAssociatedSecrets(kvUri);
        await foreach (var val in values)
        {
            VaultContents.Add(new KeyVaultContentsAmalgamation
            {
                Name = val.Name,
                Id = val.Id,
                Type = KeyVaultItemType.Secret,
                ContentType = val.ContentType,
                VaultUri = val.VaultUri,
                ValueUri = val.Id,
                Version = val.Version,
                SecretProperties = val,
                Tags = val.Tags,
                UpdatedOn = val.UpdatedOn,
                CreatedOn = val.CreatedOn,
                ExpiresOn = val.ExpiresOn,
                Enabled = val.Enabled,
                NotBefore = val.NotBefore,
                RecoverableDays = val.RecoverableDays,
                RecoveryLevel = val.RecoveryLevel
            });
        }

        _vaultContents = VaultContents;
    }

    [RelayCommand]
    private void CloseError() => HasAuthorizationError = false;

    [RelayCommand]
    private async Task Copy(KeyVaultContentsAmalgamation keyVaultItem)
    {
        if (keyVaultItem is null) return;

        try
        {
            string value = string.Empty;
            //_ = keyVaultItem.Type switch
            //{
            //    KeyVaultItemType.Key => value = (await _vaultService.GetKey(keyVaultItem.VaultUri, keyVaultItem.Name)).Key.ToRSA().ToXmlString(true),
            //    KeyVaultItemType.Secret => value = (await _vaultService.GetSecret(keyVaultItem.VaultUri, keyVaultItem.Name)).Value,
            //    KeyVaultItemType.Certificate => value = (await _vaultService.GetCertificate(keyVaultItem.VaultUri, keyVaultItem.Name)).Name,
            //    _ => throw new NotImplementedException()
            //};

            if (keyVaultItem.Type == KeyVaultItemType.Key)
            {
                var key = await _vaultService.GetKey(keyVaultItem.VaultUri, keyVaultItem.Name);
                if (key.KeyType == KeyType.Rsa)
                {
                    using var rsa = key.Key.ToRSA();
                    var publicKey = rsa.ExportRSAPublicKey();
                    string pem = "-----BEGIN PUBLIC KEY-----\n" + Convert.ToBase64String(publicKey) + "\n-----END PUBLIC KEY-----";
                    value = pem;
                }
            }

            if (keyVaultItem.Type == KeyVaultItemType.Secret)
            {
                var sv = await _vaultService.GetSecret(keyVaultItem.VaultUri, keyVaultItem.Name);
                value = sv.Value;
            }
            if (keyVaultItem.Type == KeyVaultItemType.Certificate)
            {
                var certValue = await _vaultService.GetCertificate(keyVaultItem.VaultUri, keyVaultItem.Name);
            }

            // TODO: figure out why set data object async fails here.
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, value);
            await _clipboardService.SetTextAsync(value);
            ShowInAppNotification("Copied", $"The value of '{keyVaultItem.Name}' has been copied to the clipboard.", NotificationType.Success);
            _ = Task.Run(async () => await ClearClipboardAsync().ConfigureAwait(false));

        }
        catch (KeyVaultItemNotFoundException ex)
        {
            ShowInAppNotification($"A value was not found for '{keyVaultItem.Name}'", $"The value of was not able to be retrieved.\n {ex.Message}", NotificationType.Error);
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            ShowInAppNotification($"Insufficient Privileges to access '{keyVaultItem.Name}'.", $"The value of was not able to be retrieved.\n {ex.Message}", NotificationType.Error);
        }
        catch (Exception ex)
        {
            ShowInAppNotification($"There was an error attempting to access '{keyVaultItem.Name}'.", $"The value of was not able to be retrieved.\n {ex.Message}", NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task CopyUri(KeyVaultContentsAmalgamation keyVaultItem)
    {
        if (keyVaultItem is null) return;
        await _clipboardService.SetTextAsync(keyVaultItem.Id.ToString());
    }

    private async Task DelaySetIsBusy(bool val)
    {
        await Task.Delay(1000);
        IsBusy = val;
    }

    private async Task LoadAndMarkAsLoaded(Func<Uri, Task> loadFunction, KeyVaultItemType type)
    {
        await loadFunction(VaultUri);
        LoadedItemTypes.TryAdd(type, true);
    }

    partial void OnSearchQueryChanged(string value)
    {
        var isValidEnum = Enum.TryParse(SelectedTab?.Name.ToString(), true, out KeyVaultItemType parsedEnumValue) && Enum.IsDefined(typeof(KeyVaultItemType), parsedEnumValue);
        var item = isValidEnum ? parsedEnumValue : KeyVaultItemType.Secret;
        string? query = value?.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            var contents = _vaultContents;
            if (item != KeyVaultItemType.All)
            {
                contents = contents.Where(k => k.Type == item);
            }
            VaultContents = new ObservableCollection<KeyVaultContentsAmalgamation>(contents);
            return;
        }

        VaultContents = KeyVaultFilterHelper.FilterByQuery(item != KeyVaultItemType.All ? _vaultContents.Where(k => k.Type == item) : _vaultContents, value ?? SearchQuery, item => item.Name, item => item.Tags, item => item.ContentType);
    }

    [RelayCommand]
    private void OpenInAzure(KeyVaultContentsAmalgamation keyVaultItem)
    {
        if (keyVaultItem is null) return;
        var uri = $"https://portal.azure.com/#@{_authService.TenantName}/asset/Microsoft_Azure_KeyVault/{keyVaultItem.Type}/{keyVaultItem.Id}";
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true, Verb = "open" });
    }

    [RelayCommand]
    private async Task Refresh()
    {
        var isValidEnum = Enum.TryParse(SelectedTab?.Name, true, out KeyVaultItemType parsedEnumValue) && Enum.IsDefined(typeof(KeyVaultItemType), parsedEnumValue);
        var item = isValidEnum ? parsedEnumValue : KeyVaultItemType.Secret;
        LoadedItemTypes.Remove(item);
        if (item.HasFlag(KeyVaultItemType.All))
            _vaultContents = [];

        VaultContents = KeyVaultFilterHelper.FilterByQuery(_vaultContents.Where(v => v.Type != item), SearchQuery, item => item.Name, item => item.Tags, item => item.ContentType);

        await FilterAndLoadVaultValueType(item);
    }

    private void ShowInAppNotification(string subject, string message, NotificationType notificationType)
    {
        //TODO: https://github.com/pr8x/DesktopNotifications/issues/26
        var notif = new Avalonia.Controls.Notifications.Notification(subject, message, notificationType);
        _notificationViewModel.AddMessage(notif);

        //#if WINDOWS
        //        var appUserModelId = System.AppDomain.CurrentDomain.FriendlyName;
        //        var toastNotifier = Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier(appUserModelId);
        //        var id = new Random().Next(0, 100);
        //        string toastXml = $"""
        //          <toast activationType="protocol"> // protocol,Background,Foreground
        //            <visual>
        //                <binding template='ToastGeneric'><text id="{id}">{message}</text></binding>
        //            </visual>
        //        </toast>
        //        """;
        //        XmlDocument doc = new XmlDocument();
        //        doc.LoadXml(toastXml);
        //        var toast = new ToastNotification(doc)
        //        {
        //            ExpirationTime = DateTimeOffset.Now.AddSeconds(1),
        //            //Tag = "Copied KV Values",
        //            ExpiresOnReboot = true
        //        };
        //        toastNotifier.Show(toast);
        //#endif
    }

    [RelayCommand]
    private void ShowProperties(KeyVaultContentsAmalgamation model)
    {
        if (model == null) return;

        var taskDialog = new AppWindow
        {
            Title = $"{model.Type} {model.Name} Properties",
            Icon = LazyLoadedImage,
            SizeToContent = SizeToContent.Manual,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowAsDialog = false,
            CanResize = true,
            Content = new PropertiesPage { DataContext = new PropertiesPageViewModel(model) },
            Width = 820,
            Height = 680,
            ExtendClientAreaToDecorationsHint = true,
            // TransparencyLevelHint = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.Mica, WindowTransparencyLevel.AcrylicBlur },
            // Background = null,
        };

        var topLevel = Avalonia.Application.Current.GetTopLevel() as AppWindow;
        taskDialog.Show(topLevel);
    }

    public static class KeyVaultFilterHelper
    {
        public static ObservableCollection<T> FilterByQuery<T>(
            IEnumerable<T> source,
            string query,
            Func<T, string> nameSelector,
            Func<T, IDictionary<string, string>> tagsSelector,
            Func<T, string> contentTypeSelector)
        {
            if (string.IsNullOrEmpty(query))
            {
                return new ObservableCollection<T>(source);
            }

            var filteredItems = source.Where(item =>
                nameSelector(item).AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)
                || contentTypeSelector(item).AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)
                || (tagsSelector(item)?.Any(tag =>
                    tag.Key.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)
                    || tag.Value.AsSpan().Contains(query.AsSpan(), StringComparison.OrdinalIgnoreCase)) ?? false));

            return new ObservableCollection<T>(filteredItems);
        }
    }
}