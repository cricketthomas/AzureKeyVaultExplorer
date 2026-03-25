using System.Collections.ObjectModel;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Microsoft.UI;

namespace AzureKeyVaultStudio.Models;

public abstract partial class KvTreeNodeModel : ObservableObject
{
    [ObservableProperty]
    public partial bool HasSubNodeDataBeenFetched { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    public string DisplayName { get; set; } = null!;

    public ObservableCollection<KvTreeNodeModel> Children { get; } = [];

    public virtual KeyVaultResource? VaultResource => null;

    public abstract string Glyph { get; }

    internal static readonly Brush GrayBrush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x8A, 0x8A, 0x8A));
    internal static readonly Brush OrangeBrush = new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0xFF, 0x66, 0x00));

    public virtual Brush? IconForeground => GetThemeBrush("TextFillColorSecondaryBrush");

    protected static Brush? GetThemeBrush(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true)
        {
            return value as Brush;
        }

        if (key != "TextFillColorSecondaryBrush"
            && Application.Current?.Resources.TryGetValue("TextFillColorSecondaryBrush", out var secondaryValue) == true)
        {
            return secondaryValue as Brush;
        }

        return GrayBrush;
    }

    protected static Brush CreateThemeContrastBrush()
    {
        var isDark = Application.Current?.RequestedTheme == ApplicationTheme.Dark;
        return new SolidColorBrush(isDark ? Colors.White : Colors.Black);
    }

public partial class KvSubscriptionModel : KvTreeNodeModel
    {
        public enum ExplorerItemType
        { QuickAccess, ResourceGroup };

        public ExplorerItemType Type { get; set; } = ExplorerItemType.ResourceGroup;
        public SubscriptionResource Subscription { get; set; } = null!;
        public string? SubscriptionId { get; set; }

        public override string Glyph => Type == ExplorerItemType.QuickAccess ? "\uE840" : "\uE774";

        public override Brush? IconForeground => Type == ExplorerItemType.QuickAccess
            ? GrayBrush
            : GetThemeBrush("IconForegroundColorBrush");
    }

    public partial class KvResourceGroupModel : KvTreeNodeModel
    {
        public ResourceGroupResource ResourceGroupResource { get; set; } = null!;

        public override string Glyph => "\uE8B7";

        public override Brush? IconForeground => new SolidColorBrush(ColorHelper.FromArgb(0xFF, 0x99, 0x6F, 0x00));
    }

    public partial class KvKeyVaultResourceModel : KvTreeNodeModel
    {
        public KeyVaultResource Resource { get; set; } = null!;

        public bool IsPlaceholder { get; set; }

        public override KeyVaultResource? VaultResource => IsPlaceholder ? null : Resource;

        public override string Glyph => "\uEC19";

        public override Brush? IconForeground => GetThemeBrush("IconForegroundSecondaryColorBrush");

        public static KvKeyVaultResourceModel CreatePlaceholder() => new()
        {
            IsPlaceholder = true,
            DisplayName = string.Empty,
        };
    }
}
