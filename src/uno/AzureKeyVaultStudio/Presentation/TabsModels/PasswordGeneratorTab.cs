using AzureKeyVaultStudio.Presentation.ViewModels;

namespace AzureKeyVaultStudio.Presentation.TabsModels;

public partial class PasswordGeneratorTab : TabContainerBase
{
    [ObservableProperty]
    public partial PasswordGeneratorViewModel ViewModel { get; set; } = new();

    public override void Dispose()
    {
        (ViewModel as IDisposable)?.Dispose();
    }
}
