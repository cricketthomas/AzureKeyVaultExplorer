namespace AzureKeyVaultStudio.UserControls;

public sealed class ToolPane
{
    public string Title { get; }
    public UIElement Content { get; }

    public ToolPane(string title, UIElement content)
    {
        Title = title;
        Content = content;
    }
}
