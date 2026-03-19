using Azure.Security.KeyVault.Secrets;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using KeyVaultExplorer.Models;
using System.Collections.ObjectModel;

namespace KeyVaultExplorer.ViewModels.Models;

public partial class DocumentItem : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SecretProperties> secretList;

    [ObservableProperty]
    private ObservableCollection<KeyVaultContentsAmalgamation> vaultContents;

    public DocumentItem()
    {
    }

    public object Content { get; set; }
    public string Header { get; set; }

    public IconSource IconSource { get; set; }
}