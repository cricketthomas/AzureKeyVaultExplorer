using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Azure.ResourceManager.KeyVault;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultStudio.Exceptions;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Services;
using AzureKeyVaultStudio.UserControls;
using AzureKeyVaultStudio.UserControls.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Graphics.Display;

namespace AzureKeyVaultStudio.Presentation.ViewModels;

public partial class VaultViewModel : ObservableRecipient, IDisposable
{
    private VaultService? _vaultService;
    private IDispatcher? _dispatcher;
    private ILocalSettingsService? _localSettings;
    private INavigator? _navigator;
    private AuthService? _authService;
    private IServiceProvider? _serviceProvider;
    private IStringLocalizer _localizer;
    public DispatcherQueueTimer _debounceTimerClipboard = DispatcherQueue.GetForCurrentThread().CreateTimer();
    public DispatcherQueueTimer _debounceTimerSearch = DispatcherQueue.GetForCurrentThread().CreateTimer();

    private readonly object _vaultContentsLock = new();
    private readonly HashSet<string> _addedItems = new();
    private IList<KeyVaultItemProperties> _vaultContents = [];

    public ConcurrentDictionary<KeyVaultItemType, bool> LoadedItemTypes { get; set; } = new();

    private CancellationTokenSource? _searchDebounceCts;

    private bool _hasLoadedData = false;

    [ObservableProperty]
    public partial string Header { get; set; } = "Vault";

    [ObservableProperty]
    public partial Symbol Icon { get; set; } = Symbol.Document;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VaultTotalString))]
    public partial ObservableCollection<KeyVaultItemProperties> VaultContents { get; set; } = new();


    private void BroadcastItemCount()
    {
        WeakReferenceMessenger.Default.Send(new VaultItemCountChangedMessage(VaultTotalString));
    }


    [ObservableProperty]
    public partial KeyVaultItemProperties DataGridSelectedItem { get; set; }

    public string VaultTotalString =>
        VaultContents.Count == 0 || VaultContents.Count > 1
            ? $"{VaultContents.Count} {_localizer?["ItemsText"] ?? "items"}"
            : $"1 {_localizer?["ItemText"] ?? "item"}";

    partial void OnVaultContentsChanged(ObservableCollection<KeyVaultItemProperties> value)
    {
        BroadcastItemCount();
    }

    [ObservableProperty]
    public partial string AuthorizationMessage { get; set; }

    [ObservableProperty]
    public partial bool HasAuthorizationError { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VaultTotalString))]
    public partial bool IsBusy { get; set; }


    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial KeyVaultItemProperties SelectedRow { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTab))]
    public partial int SelectedIndex { get; set; } = 0;

    public KeyVaultItemType SelectedTab => SelectedIndex switch
    {
        0 => KeyVaultItemType.Secret,
        1 => KeyVaultItemType.Certificate,
        2 => KeyVaultItemType.Key,
        3 => KeyVaultItemType.All,
        _ => KeyVaultItemType.Secret
    };

    [ObservableProperty]
    public partial Uri? VaultUri { get; set; }

    [ObservableProperty]
    public partial KeyVaultData? KeyVaultData { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; } = false;

    public VaultViewModel()
    {

    }

    protected override void OnActivated()
    {
        base.OnActivated();
        WeakReferenceMessenger.Default.Send(new VaultItemCountChangedMessage(VaultTotalString));
    }
    protected override void OnDeactivated()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnDeactivated();
    }
    private void EnsureServices()
    {
        if (_vaultService is not null && _dispatcher is not null && _localSettings is not null && _navigator is not null && _authService is not null && _serviceProvider is not null)
            return;

        var services = (Application.Current as App)?.Host?.Services;
        if (services is null)
            return;

        _vaultService ??= services.GetRequiredService<VaultService>();
        _dispatcher ??= services.GetService<IDispatcher>();
        _localSettings ??= services.GetRequiredService<ILocalSettingsService>();
        _navigator ??= services.GetService<INavigator>();
        _authService ??= services.GetRequiredService<AuthService>();
        _localizer ??= services.GetRequiredService<IStringLocalizer>();
        _serviceProvider ??= services;
    }

    private bool AreServicesAvailable()
    {
        return _vaultService is not null && _localSettings is not null && _authService is not null && _serviceProvider is not null;
    }

    private async Task ExecuteOnDispatcherAsync(Action action)
    {
        if (_dispatcher is not null)
        {
            await _dispatcher.ExecuteAsync(action);
        }
        else
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue is not null)
            {
                dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }

    private void TryEnqueueOnDispatcher(Action action)
    {
        if (_dispatcher is not null)
        {
            _dispatcher.TryEnqueue(action);
        }
        else
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue is not null)
            {
                dispatcherQueue.TryEnqueue(() => action());
            }
            else
            {
                action();
            }
        }
    }

    [RelayCommand]
    private void NewSecret()
    {
        ArgumentNullException.ThrowIfNull(KeyVaultData);

        var vm = new NewVersionViewModel
        {
            IsEdit = false,
            IsNew = true,
            VaultUri = KeyVaultData.Properties.VaultUri,
            ItemPropertiesModel = new()
            {
                Enabled = true
            }
        };
        var window = new SharedWindow();
        window.AppWindow.Title = _localizer["NewSecret"] ?? "New Secret";
        window.Content = new NewItem()
        {
            DataContext = vm,
            ParentWindow = window
        };

        window.AppWindow.Closing += (s, e) =>
        {
            (vm as IDisposable)?.Dispose();
        };


        if (OperatingSystem.IsMacOS())
            ResizeWindowScaled(window.AppWindow, 640, 680);
        else
            window.AppWindow.Resize(new SizeInt32 { Width = 640, Height = 680 });

        window.AppWindow.Show();
    }

    async partial void OnSelectedIndexChanged(int value)
    {
        try
        {
            await FilterAndLoadVaultValueTypeCommand.ExecuteAsync(new CancellationToken());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load selected vault tab: {ex}");
            TryEnqueueOnDispatcher(() =>
            {
                HasAuthorizationError = true;
                AuthorizationMessage = ex.Message;
            });
        }
    }

    public void Initialize(KeyVaultData keyVaultData)
    {
        KeyVaultData = keyVaultData;
        Header = keyVaultData.Name;
        VaultUri = keyVaultData.Properties.VaultUri;
        IsInitialized = true;
    }

    public async Task LoadDataIfNeededAsync()
    {
        EnsureServices();
        if (!AreServicesAvailable() || _hasLoadedData || !IsInitialized || VaultUri == null)
            return;

        _hasLoadedData = true;

#if DEBUG
        await CreateFakeDataAsync();
#endif
        await FilterAndLoadVaultValueTypeCommand.ExecuteAsync(new CancellationToken());
    }

    private async Task CreateFakeDataAsync()
    {
        var list = await Task.Run(() =>
        {
            var generated = new List<KeyVaultItemProperties>();

            for (int i = 0; i < 1120; i++)
            {
                var sp = new SecretProperties($"{i}_Demo__Key_Token")
                {
                    ContentType = "application/json",
                    Enabled = true,
                    ExpiresOn = new DateTime(),
                };

                var item = new KeyVaultItemProperties
                {
                    CreatedOn = new DateTime(),
                    UpdatedOn = new DateTime(),
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
                        item.Tags = new Dictionary<string, string>
                        {
                            { "LastName", "Karnik" },
                            { "ID", $"{i}" }
                        };
                        item.UpdatedOn = DateTime.Now.AddDays(-i);
                        break;

                    case 1:
                        item.Name = $"{i}__Key";
                        item.Type = KeyVaultItemType.Key;
                        break;

                    case 2:
                        item.Name = $"{i}_Certificate";
                        item.Type = KeyVaultItemType.Certificate;
                        break;
                }

                generated.Add(item);
            }

            return generated;
        });

        await ExecuteOnDispatcherAsync(() =>
        {
            lock (_vaultContentsLock)
            {
                _vaultContents = list;
            }

            VaultContents = new ObservableCollection<KeyVaultItemProperties>(list);
        });
    }

    private void ClearClipboard()
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        if (_localSettings!.GetValue(nameof(SettingsViewModel.ClearClipboardTimeout), 60) is int timeoutInSeconds)
        {
            _debounceTimerClipboard.Debounce(() =>
            {
                Clipboard.Clear();
            },
            interval: TimeSpan.FromSeconds(timeoutInSeconds));
        }
    }

    [RelayCommand(FlowExceptionsToTaskScheduler = false, AllowConcurrentExecutions = false, IncludeCancelCommand = true)]
    private async Task FilterAndLoadVaultValueType(CancellationToken token)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        try
        {
            await ExecuteOnDispatcherAsync(() =>
            {
                HasAuthorizationError = false;
            });

            if (!LoadedItemTypes.ContainsKey(SelectedTab))
            {
                await ExecuteOnDispatcherAsync(() =>
                {
                    IsBusy = true;
                });

                token.ThrowIfCancellationRequested();

                var loadTasks = SelectedTab switch
                {
                    KeyVaultItemType.Certificate => new[]
                    {
                        LoadAndMarkAsLoaded(GetCertificatesForVault, KeyVaultItemType.Certificate, token)
                    },
                    KeyVaultItemType.Key => new[]
                    {
                        LoadAndMarkAsLoaded(GetKeysForVault, KeyVaultItemType.Key, token)
                    },
                    KeyVaultItemType.Secret => new[]
                    {
                        LoadAndMarkAsLoaded(GetSecretsForVault, KeyVaultItemType.Secret, token)
                    },
                    KeyVaultItemType.All => new[]
                    {
                        LoadAndMarkAsLoaded(GetSecretsForVault, KeyVaultItemType.Secret, token),
                        LoadAndMarkAsLoaded(GetKeysForVault, KeyVaultItemType.Key, token),
                        LoadAndMarkAsLoaded(GetCertificatesForVault, KeyVaultItemType.Certificate, token)
                    },
                    _ => Array.Empty<Task>()
                };

                if (loadTasks.Length > 0)
                {
                    await Task.WhenAll(loadTasks);
                }

                LoadedItemTypes.TryAdd(SelectedTab, true);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Loading canceled.");
            return;
        }
        catch (AuthenticationRequiredException ex)
        {
            TryEnqueueOnDispatcher(() =>
            {
                HasAuthorizationError = true;
                AuthorizationMessage = ex.Message;
            });
        }
        catch (Exception ex) when (ex.Message.Contains("403", StringComparison.OrdinalIgnoreCase))
        {
            // we only show error when not on "All" tab, partial success is acceptable for "All"
            if (SelectedTab != KeyVaultItemType.All)
            {
                TryEnqueueOnDispatcher(() =>
                {
                    HasAuthorizationError = true;
                    AuthorizationMessage = ex.Message;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unhandled vault loading error: {ex}");
            TryEnqueueOnDispatcher(() =>
            {
                HasAuthorizationError = true;
                AuthorizationMessage = ex.Message;
            });
        }
        finally
        {
            await ExecuteOnDispatcherAsync(() =>
            {
                IList<KeyVaultItemProperties> snapshot;
                lock (_vaultContentsLock)
                    snapshot = _vaultContents.ToList();

                IEnumerable<KeyVaultItemProperties> contents = SelectedTab == KeyVaultItemType.All ? snapshot : snapshot.Where(x => x.Type == SelectedTab);
                VaultContents = FilterService.FilterByQuery(contents, SearchQuery, i => i.Name, i => i.Tags, i => i.ContentType);
                IsBusy = false;
            });
        }
    }

    private async Task LoadAndMarkAsLoaded(Func<Uri, CancellationToken, Task> loadFunction, KeyVaultItemType type, CancellationToken token)
    {
        await loadFunction(VaultUri, token);
        LoadedItemTypes.TryAdd(type, true);
    }


    #region Get Vault Items
    public async Task GetCertificatesForVault(Uri kvUri, CancellationToken cancellationToken)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        var certs = _vaultService!.GetVaultAssociatedCertificates(kvUri, cancellationToken);
        var newItems = new List<KeyVaultItemProperties>();

        await foreach (var val in certs.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string uniqueKey = $"{val.Id}-{KeyVaultItemType.Certificate}";
            if (_addedItems.Add(uniqueKey))
            {
                var item = new KeyVaultItemProperties
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
                    Enabled = val.Enabled ?? false,
                    NotBefore = val.NotBefore,
                    RecoverableDays = val.RecoverableDays,
                    RecoveryLevel = val.RecoveryLevel
                };
                newItems.Add(item);
            }
        }

        if (newItems.Count > 0)
        {
            lock (_vaultContentsLock)
            {
                _vaultContents = _vaultContents.Concat(newItems).ToList();
            }
        }
    }

    public async Task GetKeysForVault(Uri kvUri, CancellationToken cancellationToken)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        var keys = _vaultService!.GetVaultAssociatedKeys(kvUri, cancellationToken);
        var newItems = new List<KeyVaultItemProperties>();

        await foreach (var val in keys.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string uniqueKey = $"{val.Id}-{KeyVaultItemType.Key}";
            if (_addedItems.Add(uniqueKey))
            {
                var item = new KeyVaultItemProperties
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
                    Enabled = val.Enabled ?? false,
                    NotBefore = val.NotBefore,
                    RecoverableDays = val.RecoverableDays,
                    RecoveryLevel = val.RecoveryLevel
                };
                newItems.Add(item);
            }
        }

        if (newItems.Count > 0)
        {
            lock (_vaultContentsLock)
            {
                _vaultContents = _vaultContents.Concat(newItems).ToList();
            }
        }
    }

    public async Task GetSecretsForVault(Uri kvUri, CancellationToken cancellationToken)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        var newItems = new List<KeyVaultItemProperties>();

        await foreach (var val in _vaultService!.GetVaultAssociatedSecrets(kvUri, cancellationToken).WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string uniqueKey = $"{val.Id}-{KeyVaultItemType.Secret}";
            if (_addedItems.Add(uniqueKey))
            {
                var item = new KeyVaultItemProperties
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
                    Enabled = val.Enabled ?? false,
                    NotBefore = val.NotBefore,
                    RecoverableDays = val.RecoverableDays,
                    RecoveryLevel = val.RecoveryLevel
                };
                newItems.Add(item);
            }
        }

        if (newItems.Count > 0)
        {
            lock (_vaultContentsLock)
            {
                _vaultContents = _vaultContents.Concat(newItems).ToList();
            }
        }
    }

    #endregion Get Vault Items



    [RelayCommand]
    private void CloseError() => HasAuthorizationError = false;

    // need the "isactive" check due to page caching and needing to know if were executing this on the right VM.
    [RelayCommand(CanExecute = nameof(IsActive))]
    private async Task Copy(KeyVaultItemProperties keyVaultItem)
    {
        if (keyVaultItem is null)
            return;

        EnsureServices();
        if (!AreServicesAvailable())
            return;

        try
        {
            string value = string.Empty;

            if (keyVaultItem.Type == KeyVaultItemType.Key)
            {
                var key = await _vaultService!.GetKey(keyVaultItem.VaultUri, keyVaultItem.Name);
                if (key.KeyType == KeyType.Rsa)
                {
                    using var rsa = key.Key.ToRSA();
                    var publicKey = rsa.ExportRSAPublicKey();
                    string pem = "-----BEGIN PUBLIC KEY-----\n" +
                                 Convert.ToBase64String(publicKey) +
                                 "\n-----END PUBLIC KEY-----";
                    value = pem;
                }
            }
            else if (keyVaultItem.Type == KeyVaultItemType.Secret)
            {
                var sv = await _vaultService!.GetSecret(keyVaultItem.VaultUri, keyVaultItem.Name);
                value = sv.Value;
            }
            else if (keyVaultItem.Type == KeyVaultItemType.Certificate)
            {
                // For now, just retrieve to validate access
                _ = await _vaultService!.GetCertificate(keyVaultItem.VaultUri, keyVaultItem.Name);
            }

            await ExecuteOnDispatcherAsync(() =>
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(value);
                Clipboard.SetContent(dataPackage);
            });


            ShowInAppNotification(
                _localizer["Copied"] ??"Copied",
                string.Format(_localizer["SecretCopiedMessage"], keyVaultItem.Name) ?? $"The value of '{keyVaultItem.Name}' has been copied to the clipboard.",
                InfoBarSeverity.Success);

            ClearClipboard();
        }
        catch (KeyVaultItemNotFoundException ex)
        {

            ShowInAppNotification(
           _localizer["SecretNotFoundMessage"] ?? $"A value was not found for '{keyVaultItem.Name}'",
           ex.Message,
           InfoBarSeverity.Error);
   
        }
        catch (KeyVaultInsufficientPrivilegesException ex)
        {
            ShowInAppNotification(
                string.Format(_localizer["InsufficientPrivilegesMessage"], keyVaultItem.Name) ?? $"Insufficient Privileges to access '{keyVaultItem.Name}'",
                ex.Message,
                InfoBarSeverity.Error);
        }
        catch (Exception ex)
        {
            ShowInAppNotification(
                _localizer["NewItemErrorTitle"],
                ex.Message,
                InfoBarSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task CopyUri(KeyVaultItemProperties keyVaultItem)
    {
        if (keyVaultItem is null)
            return;

        EnsureServices();
        if (!AreServicesAvailable())
            return;

        await ExecuteOnDispatcherAsync(() =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(keyVaultItem.ValueUri.ToString());
            Clipboard.SetContent(dataPackage);
        });
    }

    partial void OnSearchQueryChanged(string value)
    {
        _ = DebounceAndApplySearchAsync(value);
    }

    private async Task DebounceAndApplySearchAsync(string? value)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        _debounceTimerSearch.Debounce(async () =>
        {
            try
            {
                var query = value?.Trim();
                var selectedTab = SelectedTab;

                IList<KeyVaultItemProperties> snapshot;
                lock (_vaultContentsLock)
                    snapshot = _vaultContents.ToList();

                IEnumerable<KeyVaultItemProperties> baseSequence = selectedTab == KeyVaultItemType.All ? snapshot : snapshot.Where(k => k.Type == selectedTab);

                ObservableCollection<KeyVaultItemProperties> filtered;

                if (string.IsNullOrWhiteSpace(query))
                {
                    filtered = new ObservableCollection<KeyVaultItemProperties>(baseSequence);
                }
                else
                {
                    var result = FilterService.FilterByQuery(baseSequence, query, item => item.Name, item => item.Tags, item => item.ContentType);
                    filtered = result is ObservableCollection<KeyVaultItemProperties> oc ? oc : new ObservableCollection<KeyVaultItemProperties>(result);
                }

                await ExecuteOnDispatcherAsync(() =>
                {
                    if (SearchQuery?.Trim() == query)
                    {
                        VaultContents = filtered;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Search debounce canceled.");
            }
        },
               interval: TimeSpan.FromMilliseconds(100),
               immediate: SearchQuery.IsNullOrWhiteSpace());
    }

    [RelayCommand]
    private async Task OpenInAzure(KeyVaultItemProperties keyVaultItem)
    {
        if (keyVaultItem is null) return;

        string tenantId = _authService!.TenantId;
        string portalBaseUri = _authService!.AzurePortalBaseUri;
        var uri = new Uri($"{portalBaseUri}/#@{tenantId}/asset/Microsoft_Azure_KeyVault/{keyVaultItem.Type}/{keyVaultItem.Id}");
        await Windows.System.Launcher.LaunchUriAsync(uri);
    }

    [RelayCommand]
    private async Task Refresh(CancellationToken token)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        lock (_vaultContentsLock)
        {
            _vaultContents = [];
        }

        LoadedItemTypes.Clear();
        _addedItems.Clear();
        await FilterAndLoadVaultValueTypeCommand.ExecuteAsync(token);
    }

    private static void ResizeWindowScaled(Microsoft.UI.Windowing.AppWindow appWindow, int logicalWidth, int logicalHeight)
    {
        //https://github.com/unoplatform/uno/issues/22217
        var scale = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
        appWindow.Resize(new SizeInt32 { Width = (int)(logicalWidth * scale), Height = (int)(logicalHeight * scale) });
    }

    private void ShowInAppNotification(string subject, string message, InfoBarSeverity notificationType)
    {
        WeakReferenceMessenger.Default.Send(new SendInAppNotificationMessage(new Notification
        {
            Severity = notificationType,
            Message = message,
            Title = subject,
            Duration = TimeSpan.FromSeconds(2)
        }));
    }

    public void Dispose()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;

        _debounceTimerClipboard?.Stop();
        _debounceTimerSearch?.Stop();

        lock (_vaultContentsLock)
        {
            _vaultContents.Clear();
        }
        VaultContents.Clear();
        _addedItems.Clear();
        LoadedItemTypes.Clear();

        _hasLoadedData = false;
        IsInitialized = false;

    }



    [RelayCommand]
    private async Task ShowProperties(KeyVaultItemProperties model)
    {
        EnsureServices();
        if (!AreServicesAvailable())
            return;

        try
        {
            var window = new SharedWindow();
            window.AppWindow.Title = $"{_localizer["PropertiesText"]} - {model.Name}";

            var itemVm = _serviceProvider!.GetService<ItemPropertiesViewModel>();
            itemVm._currentWindow = window;
            if (itemVm == null)
            {
                ShowInAppNotification(
                    "Error",
                    "Unable to resolve the properties view model.",
                    InfoBarSeverity.Error);
                return;
            }

            var itemDetailsView = new ItemDetails
            {
                DataContext = itemVm
            };
            window.Content = itemDetailsView;

            window.AppWindow.Closing += (s, e) =>
            {
                (itemVm as IDisposable)?.Dispose();
            };

            if (OperatingSystem.IsMacOS())
                ResizeWindowScaled(window.AppWindow, 620, 680);
            else
                window.AppWindow.Resize(new SizeInt32 { Width = 640, Height = 680 });

            if (Application.Current is App app && app.MainWindow is Window mainWindow)
            {
                var mainPos = mainWindow.AppWindow.Position;
                var mainSize = mainWindow.AppWindow.Size;
                var childSize = window.AppWindow.Size;

                int x = mainPos.X + (mainSize.Width - childSize.Width) / 2;
                int y = mainPos.Y + (mainSize.Height - childSize.Height) / 2;
                window.AppWindow.Move(new PointInt32 { X = x, Y = y });
            }
            window.AppWindow.Show();
            try
            {
                await itemVm.GetPropertiesForKeyVaultValue(model);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading properties: {ex}");
                ShowInAppNotification(
                    "Error",
                    $"Unable to load properties: {ex.Message}",
                    InfoBarSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing properties: {ex}");
            ShowInAppNotification(
                "Error",
                $"Unable to open properties window: {ex.Message}",
                InfoBarSeverity.Error);
        }
    }
}

