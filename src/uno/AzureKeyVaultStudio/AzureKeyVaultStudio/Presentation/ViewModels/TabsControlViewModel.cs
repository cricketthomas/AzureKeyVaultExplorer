using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Azure.ResourceManager.KeyVault;
using AzureKeyVaultStudio.Messages;
using AzureKeyVaultStudio.Presentation.TabsModels;
using CommunityToolkit.Mvvm.Messaging;

namespace AzureKeyVaultStudio.Presentation.ViewModels;

public partial class TabsControlViewModel : ObservableRecipient

{
    private bool _suppressDispatch;

    
    
    [ObservableProperty]
    public partial TabContainerBase? SelectedTab { get; set; }

    [ObservableProperty]
    public partial bool IsPaneOpen { get; set; }
  

    [ObservableProperty]
    public partial bool IsTeachingTipOpen { get; set; } = true;

    [ObservableProperty]
    public partial VaultViewModel? CurrentVaultViewModel { get; set; }


    public ObservableCollection<TabContainerBase> Tabs { get; } = new();

    public TabsControlViewModel()
    {
        IsActive = true;

#if DEBUG
        InitializeDataBindingSampleData();
#endif
        if (Tabs.Count > 0 && SelectedTab is null)
        {
            SelectedTab = Tabs[0];
        }
    }

    partial void OnIsPaneOpenChanged(bool value)
    {
        if (_suppressDispatch) return;

        WeakReferenceMessenger.Default.Send(new PaneStateChangedMessage(value));
    }


    protected override void OnActivated()
    {
        OnDeactivated();

        Messenger.Register<AddDocumentMessage>(this, async (r, m) =>
        {
            await AddVaultTabAsync(m.KeyVaultData);
        });

        Messenger.Register<AddTabMessage>(this, async (r, m) =>
        {
            await AddGenericTabAsync(m.Tab);
        });

        WeakReferenceMessenger.Default.Register<TabsControlViewModel, OpenItemDetailsWindowMessage>(this, (r, m) =>
        {
            var vm = r.CurrentVaultViewModel;
            if (vm is not null)
                _ = vm.ShowPropertiesCommand.ExecuteAsync(m.Value);
            else // TODO: figure out the best way to open this..cuz this is dirty
                new VaultViewModel().ShowPropertiesCommand.ExecuteAsync(m.Value);
        });
        WeakReferenceMessenger.Default.Register<TabsControlViewModel, PaneStateChangedMessage>(this, (r, m) =>
        {
            r._suppressDispatch = true;
            r.IsPaneOpen = m.Value;
            r._suppressDispatch = false;
        });
    }

    protected override void OnDeactivated()
    {
        Messenger.UnregisterAll(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnDeactivated();
    }

    partial void OnSelectedTabChanged(TabContainerBase? value)
    {
        UpdateCurrentVaultViewModel();
        // show teaching tip until we figure out a better design for this would be menubar.
        IsTeachingTipOpen = Tabs.Count == 0 && IsActive;
    }

    private void UpdateCurrentVaultViewModel()
    {
        CurrentVaultViewModel = (SelectedTab as VaultTabContainer)?.ViewModel;
    }

    private void InitializeDataBindingSampleData()
    {
        for (var index = 0; index < 2; index++)
        {
            Tabs.Add(CreateSampleTab(index));
        }
    }

    private VaultTabContainer CreateSampleTab(int index)
    {
        var tab = new VaultTabContainer
        {
            Header = $"Sample Vault {index}",
            Icon = Symbol.Folder,
            KeyVaultData = null
        };

        tab.ViewModel.VaultUri = new Uri("https://kv-art-dev-only.vault.azure.net/");
        return tab;
    }

    public Task AddVaultTabAsync(KeyVaultData keyVaultData)
    {
        ArgumentNullException.ThrowIfNull(keyVaultData);

        var tab = new VaultTabContainer
        {
            Header = keyVaultData.Name,
            Icon = Symbol.Folder,
            KeyVaultData = keyVaultData
        };

        Tabs.Insert(0, tab);
        SelectedTab = tab;

        return Task.CompletedTask;
    }

    public Task AddGenericTabAsync(TabContainerBase tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        Tabs.Insert(0, tab);
        SelectedTab = tab;

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseTab(TabContainerBase? tab)
    {
        if (tab == null)
            return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);
        tab.Dispose();

        if (Tabs.Count == 0)
        {
            SelectedTab = null;

            // TODO: figure out if this is needed, chat gave it to me
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
        }
        else if (SelectedTab == tab)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;

            SelectedTab = Tabs[index];
        }
    }

    [RelayCommand]
    private void OpenPasswordGenerator()
    {
        var customTab = new PasswordGeneratorTab
        {
            Header = "Password Generator",
            Icon = Symbol.Calculator,
        };

        Messenger.Send(new AddTabMessage(customTab));
    }
}
