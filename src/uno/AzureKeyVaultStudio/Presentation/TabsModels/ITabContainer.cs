namespace AzureKeyVaultStudio.Presentation;
/// <summary>
/// Base container for all the tabs, that are nav'd to.
/// </summary>
public abstract partial class TabContainerBase : ObservableObject, IDisposable
{
    [ObservableProperty]
    public partial string Header { get; set; }

    [ObservableProperty]
    public partial Symbol Icon { get; set; }

    public abstract void Dispose();
}
