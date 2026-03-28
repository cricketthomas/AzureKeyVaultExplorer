using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace AzureKeyVaultStudio.UserControls;

public sealed partial class TagsEditor : UserControl
{

    
    public ObservableCollection<TagItem> EditableTags { get; } = new();

    public TagsEditor()
    {
        this.InitializeComponent();
        TranformTagsToTagItems();

        EditableTags.CollectionChanged += EditableTagsUpdateBindingTags_CollectionChanged;
    }

    private void EditableTagsUpdateBindingTags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dictionary<string, string>? dictTags = [];
        foreach(var tag in EditableTags)
        {
            dictTags.Add(tag.Key, tag.Value);
        }
    }

    //public static readonly DependencyProperty RemoveTagCommandProperty = DependencyProperty.Register(
    // nameof(RemoveTagCommand),
    // typeof(ICommand),
    // typeof(TagsEditor),
    // new PropertyMetadata(default));

    //public ICommand RemoveTagCommand
    //{
    //    get => (ICommand)GetValue(RemoveTagCommandProperty);
    //    set => SetValue(RemoveTagCommandProperty, value);
    //}


    public static readonly DependencyProperty TagsProperty =
        DependencyProperty.Register(
            nameof(Tags),
            typeof(Dictionary<string, string>),
            typeof(TagsEditor),
            new PropertyMetadata(default(TagsEditor)));

    public Dictionary<string, string> Tags
    {
        get => (Dictionary<string, string>)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    private void RemoveTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TagItem tagItem)
        {
            EditableTags.Remove(tagItem);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        var count = EditableTags is null ? 0 : EditableTags.Count;
        EditableTags.Add(new TagItem
        {
            Key = $"Key{count}",
            Value = $"Value{count}"
        });
    }

    private void TranformTagsToTagItems()
    {
        foreach(var item in Tags)
        {
            EditableTags.Add(new TagItem { Key = item.Key, Value = item.Value });
        }
    }

}

public partial class TagItem : ObservableObject
{
    [ObservableProperty]
    public partial string Key { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
