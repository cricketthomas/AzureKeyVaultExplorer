using Azure.ResourceManager.KeyVault;
using AzureKeyVaultStudio.Presentation.ViewModels;

namespace AzureKeyVaultStudio.Presentation;

public partial class VaultTabContainer : TabContainerBase
{
    [ObservableProperty]
    public partial KeyVaultData? KeyVaultData { get; set; }

    [ObservableProperty]
    public partial VaultViewModel ViewModel { get; set; } = new();

    [ObservableProperty]
    public partial bool IsDataLoaded { get; set; } = false;

    public async Task EnsureDataLoadedAsync()
    {
        if (!IsDataLoaded && ViewModel != null)
        {
            await ViewModel.LoadDataIfNeededAsync();
            IsDataLoaded = true;
        }
    }

    partial void OnKeyVaultDataChanged(KeyVaultData? oldValue, KeyVaultData? newValue)
    {
        if (newValue != null && ViewModel.IsInitialized == false)
        {
            ViewModel.Initialize(newValue);
        }
    }

    public override void Dispose()
    {
        ViewModel?.Dispose();
        (ViewModel as IDisposable)?.Dispose();
        IsDataLoaded = false;
    }
}
