using System.Collections.ObjectModel;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class TagsEditor : UserControl
{

    public TagsEditor()
    {
        this.InitializeComponent();

        if (EditableTags is null)
            EditableTags = [];
    }

  

    public static readonly DependencyProperty EditableTagsProperty =
        DependencyProperty.Register(
            nameof(EditableTags),
            typeof(ObservableCollection<TagItem>),
            typeof(TagsEditor),
            new PropertyMetadata(default(TagsEditor)));

    public ObservableCollection<TagItem> EditableTags
    {
        get => (ObservableCollection<TagItem>)GetValue(EditableTagsProperty);
        set => SetValue(EditableTagsProperty, value);
    }

    private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TagItem tagItem)
        {
            EditableTags.Remove(tagItem);
        }
    }

    private void AddTagButton_Click(object sender, RoutedEventArgs e)
    {
        EditableTags.Add(new TagItem { Key = string.Empty, Value = string.Empty });
    }
 

}

