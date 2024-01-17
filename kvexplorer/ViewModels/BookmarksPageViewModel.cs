﻿using Avalonia.Threading;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kvexplorer.shared;
using kvexplorer.shared.Database;
using kvexplorer.shared.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace kvexplorer.ViewModels;

public partial class BookmarksPageViewModel : ViewModelBase
{
    private readonly VaultService _vaultService;
    private readonly KvExplorerDb _db;

    public BookmarksPageViewModel()
    {
        _vaultService = Defaults.Locator.GetRequiredService<VaultService>();
        _db = Defaults.Locator.GetRequiredService<KvExplorerDb>();
    }

    [ObservableProperty]
    public ObservableCollection<string> selectedSubscription;
    /// <summary>
    /// The Title of this page
    /// </summary>
    public string Title => "Welcome to our Wizard-Sample.";

    /// <summary>
    /// The content of this page
    /// </summary>
    public string Message => "Press \"Next\" to register yourself.";


    [ObservableProperty]
    public ObservableCollection<SubscriptionResource> subscriptions;

    [RelayCommand]
    public async Task GetAllKeyVaults()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var resource = _vaultService.GetAllSubscriptions();
            await foreach (var item in resource)
            {
                Subscriptions.Add(item.SubscriptionResource);
            }
        });
    }
}