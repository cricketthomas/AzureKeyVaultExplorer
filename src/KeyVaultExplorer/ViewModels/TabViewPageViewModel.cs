using Avalonia.Controls;
using Avalonia.Threading;
using Azure.ResourceManager.KeyVault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using KeyVaultExplorer.Views.Pages;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace KeyVaultExplorer.ViewModels;

public partial class TabViewPageViewModel : ViewModelBase
{
    private SettingsPageViewModel _settingsPageViewModel { get; set; }

    public TabViewPageViewModel()
    {
        Documents = new ObservableCollection<TabViewItem>();
#if DEBUG
        Documents.Add(AddDocument(1));
#endif

        _settingsPageViewModel = Defaults.Locator.GetRequiredService<SettingsPageViewModel>();

        var settings = Task.Run(async () =>
        {
            return await _settingsPageViewModel.GetAppSettings();
        });

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            SplitViewDisplayMode = (await settings).SplitViewDisplayMode == "Inline" ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Overlay;
        });
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPin))]
    private SplitViewDisplayMode splitViewDisplayMode = SplitViewDisplayMode.Inline;

    public bool ShowPin => SplitViewDisplayMode == SplitViewDisplayMode.Inline;

    [RelayCommand]
    private void ChangePaneDisplay()
    {
        if (SplitViewDisplayMode is SplitViewDisplayMode.Inline)
            SplitViewDisplayMode = SplitViewDisplayMode.Overlay;
        else
            SplitViewDisplayMode = SplitViewDisplayMode.Inline;

        Dispatcher.UIThread.Post(async () =>
        {
            await _settingsPageViewModel.SetSplitViewDisplayModeCommand.ExecuteAsync(SplitViewDisplayMode.ToString());
        }, DispatcherPriority.ApplicationIdle);
    }

    [ObservableProperty]
    private ObservableCollection<TabViewItem> documents = [];

    public ObservableCollection<TabViewItem> KeyBindingDocuments { get; }

    [ObservableProperty]
    private TabViewItem selectedItem;

    //public DocumentItem KeyBindingSelectedDocument
    //{
    //    get => _keybindingSelectedDocument;
    //    set => RaiseAndSetIfChanged(ref _keybindingSelectedDocument, value);
    //}

    public string KeyBindingText { get; set; }

    [RelayCommand]
    private void AddDocument()
    {
        var doc = AddDocument(Documents.Count);
        Documents.Add(doc);
    }

    private TabViewItem AddDocument(int index)
    {
        var tab = new TabViewItem
        {
            Header = $"My document {index}",
        };

        switch (index % 3)
        {
            case 0:
                tab.IconSource = new SymbolIconSource { Symbol = Symbol.Document };
                tab.Content = new VaultPage();
                break;

            case 1:
                tab.IconSource = new SymbolIconSource { Symbol = Symbol.Star };
                tab.Content = new VaultPage();
                break;

            case 2:
                tab.IconSource = new SymbolIconSource { Symbol = Symbol.Open };
                tab.Content = new VaultPage();
                break;
        }

        return tab;
    }

    [RelayCommand]
    private void AddVaultPage(KeyVaultData model)
    {
        var tab = new TabViewItem
        {
            Header = model.Name,
            IconSource = new SymbolIconSource { Symbol = Symbol.ProtectedDocument },
            Content = new VaultPage(model.Properties.VaultUri)
        };

        Documents.Insert(0, tab);
        SelectedItem = tab;
        SelectedItem.Focus();
    }
}