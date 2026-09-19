using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using WinForms = System.Windows.Forms;

namespace EverskiesOrganizer;
public partial class MainWindow : Window {
    const string AppVersion = "1.0.1";
    ProjectData Project = new(); OrganizerItem? CurrentItem;
Variation? CurrentVariation;
bool loading;
    public ObservableCollection<ImageAsset> Assets => Project.Assets;
    public ObservableCollection<OrganizerItem> Items => Project.Items;
    public MainWindow() { InitializeComponent(); DataContext=this; ApplyTheme(); }
    void Import_Click(object s, RoutedEventArgs e) { using var dialog=new WinForms.FolderBrowserDialog { Description="Select the folder containing exported PNG files" }; if(dialog.ShowDialog()!=WinForms.DialogResult.OK)return; ImportFolder(dialog.SelectedPath); }
    void ImportFolder(string folder) { var files=Directory.EnumerateFiles(folder,"*.png",SearchOption.AllDirectories).ToArray(); foreach(var f in files.Where(f=>!Project.Assets.Any(a=>a.Path.Equals(f,StringComparison.OrdinalIgnoreCase)))) Project.Assets.Add(new ImageAsset{Path=f,Thumbnail=ThumbnailService.FromPng(f)});HydrateVisuals();RefreshImportedAssets(); StatusText.Text=$"Imported {files.Length} PNGs from this folder and all subfolders. Select images, then create an item."; }
    List<ImageAsset> SelectedAssets() => AssetsList.SelectedItems.Cast<ImageAsset>().ToList();
    void CreateItem_Click(object s, RoutedEventArgs e) { var selected=SelectedAssets(); if(!selected.Any()){System.Windows.MessageBox.Show("Select one or more PNGs first.");return;} var item=new OrganizerItem{Name="New item",PreviewAssetId=selected[0].Id,PreviewThumbnail=selected[0].Thumbnail}; foreach(var a in selected)item.UnassignedAssetIds.Add(a.Id); Project.Items.Add(item); RefreshImportedAssets(); ItemsList.SelectedItem=item; }
    void NewItem_Click(object s,RoutedEventArgs e) { var item=new OrganizerItem();Project.Items.Add(item);ItemsList.SelectedItem=item; }
    void RemoveItem_Click(object s, RoutedEventArgs e) { if(CurrentItem is null)return; Project.Items.Remove(CurrentItem);CurrentItem=null;EditorPanel.IsEnabled=false;RefreshImportedAssets(); }
    void Item_Changed(object s, SelectionChangedEventArgs e) { CurrentItem=ItemsList.SelectedItem as OrganizerItem; LoadItem(); }
    void LoadItem() { loading=true; EditorPanel.IsEnabled=CurrentItem is not null; if(CurrentItem is null){loading=false;return;} ItemName.Text=CurrentItem.Name; if(string.IsNullOrEmpty(CurrentItem.MainCategory)&&CurrentItem.Category.Contains(" → ")){var p=CurrentItem.Category.Split(" → ");CurrentItem.MainCategory=p[0];CurrentItem.SubCategory=p[^1];} MainCategoryBox.ItemsSource=Project.Settings.CategoryTree.Keys;MainCategoryBox.SelectedItem=CurrentItem.MainCategory;SubCategoryBox.ItemsSource=Project.Settings.CategoryTree.TryGetValue(CurrentItem.MainCategory,out var subs)?subs:[];SubCategoryBox.SelectedItem=CurrentItem.SubCategory;
RefreshItemAssets();
RenderVariations(); TagsConfig.Text=string.Join(Environment.NewLine,Project.Settings.Tags);CategoriesConfig.Text=string.Join(Environment.NewLine,Project.Settings.CategoryTree.Select(x=>$"{x.Key}: {string.Join(", ",x.Value)}"));FilenameTemplateBox.Text=Project.Settings.FilenameTemplate; loading=false; }
    ImageAsset? Asset(string id)=>Project.Assets.FirstOrDefault(a=>a.Id==id);
    void RefreshImportedAssets()
    {
        var assignedIds = Project.Items
            .SelectMany(item => new[] { item.PreviewAssetId }
                .Concat(item.UnassignedAssetIds)
                .Concat(item.Variations.SelectMany(v => new[] { v.MinusAssetId, v.PlusAssetId, v.AllAssetId })))
            .Where(id => id is not null)
            .ToHashSet();

        AssetsList.ItemsSource = Project.Assets.Where(asset => !assignedIds.Contains(asset.Id)).ToList();
    }
    void HydrateVisuals()
{
    foreach (var item in Project.Items)
    {
        item.PreviewThumbnail =
            Asset(item.PreviewAssetId ?? "")?.Thumbnail;

        foreach (var v in item.Variations)
        {
            v.MinusThumbnail =
                Asset(v.MinusAssetId ?? "")?.Thumbnail;

            v.PlusThumbnail =
                Asset(v.PlusAssetId ?? "")?.Thumbnail;

            v.AllThumbnail =
                Asset(v.AllAssetId ?? "")?.Thumbnail;
        }
    }

    ItemsList.Items.Refresh();
    VariationsList.Items.Refresh();
}
        void RefreshItemAssets()
{
    if (CurrentItem is null)
        return;

    var ids = new[] { CurrentItem.PreviewAssetId }
        .Concat(CurrentItem.UnassignedAssetIds)
        .Concat(
            CurrentItem.Variations
                .SelectMany(v => new[]
                {
                    v.MinusAssetId,
                    v.PlusAssetId,
                    v.AllAssetId
                })
        )
        .Where(x => x is not null)
        .Cast<string>()
        .Distinct();

    var itemAssets = ids
        .Select(Asset)
        .Where(a => a is not null)
        .Cast<ImageAsset>()
        .ToList();

    var assignedIds = CurrentItem.Variations
        .SelectMany(v => new[] { v.MinusAssetId, v.PlusAssetId, v.AllAssetId })
        .Where(id => id is not null)
        .ToHashSet();

    foreach (var asset in itemAssets)
        asset.ItemOpacity = assignedIds.Contains(asset.Id) ? 0.5 : 1;

    UnassignedList.ItemsSource = itemAssets;
}
            void MainCategory_Changed(object s,SelectionChangedEventArgs e) { if(loading||CurrentItem is null)return;CurrentItem.MainCategory=MainCategoryBox.SelectedItem?.ToString()??"";SubCategoryBox.ItemsSource=Project.Settings.CategoryTree.TryGetValue(CurrentItem.MainCategory,out var values)?values:[];SubCategoryBox.SelectedIndex=-1;CurrentItem.SubCategory="";CurrentItem.Category=CurrentItem.MainCategory;ItemsList.Items.Refresh(); }
    void ItemDetails_Changed(object s,RoutedEventArgs e) { if(loading||CurrentItem is null)return;CurrentItem.Name=ItemName.Text;CurrentItem.SubCategory=SubCategoryBox.SelectedItem?.ToString()??"";CurrentItem.Category=string.Join(" → ",new[]{CurrentItem.MainCategory,CurrentItem.SubCategory}.Where(x=>!string.IsNullOrWhiteSpace(x)));ItemsList.Items.Refresh(); }
    void SetPreview_Click(object s,RoutedEventArgs e)
    {
        CurrentItem ??= ItemsList.SelectedItem as OrganizerItem;
        var element = s as FrameworkElement;
        var asset = element?.Tag as ImageAsset ?? element?.DataContext as ImageAsset;
        if (CurrentItem is null || asset is null)
        {
            System.Windows.MessageBox.Show("Select an item asset first.");
            return;
        }

        CurrentItem.PreviewAssetId = asset.Id;
        CurrentItem.PreviewThumbnail = asset.Thumbnail;
        RefreshItemAssets();
        RefreshImportedAssets();
        ItemsList.Items.Refresh();
    }
void RemoveItemAsset_Click(object s, RoutedEventArgs e)
{
    CurrentItem ??= ItemsList.SelectedItem as OrganizerItem;
    if (CurrentItem is null)
        return;

    var element = s as FrameworkElement;
    var asset = element?.Tag as ImageAsset ?? element?.DataContext as ImageAsset;
    if (asset is null)
        return;

    if (CurrentItem.PreviewAssetId == asset.Id)
    {
        CurrentItem.PreviewAssetId = null;
        CurrentItem.PreviewThumbnail = null;
    }

    CurrentItem.UnassignedAssetIds.Remove(asset.Id);
    RefreshImportedAssets();

    foreach (var variation in CurrentItem.Variations)
    {
        if (variation.MinusAssetId == asset.Id)
            variation.MinusAssetId = null;

        if (variation.PlusAssetId == asset.Id)
            variation.PlusAssetId = null;

        if (variation.AllAssetId == asset.Id)
            variation.AllAssetId = null;
    }

    RefreshItemAssets();
    RefreshImportedAssets();
    RenderVariations();
    ItemsList.Items.Refresh();
}
void NewVariation_Click(object s, RoutedEventArgs e)
{
    if (CurrentItem is null)
        return;

    var variation = new Variation
    {
        Label = $"Variation {CurrentItem.Variations.Count + 1}"
    };

    CurrentItem.Variations.Add(variation);
    RefreshItemAssets();
    ItemsList.Items.Refresh();
    RenderVariations();
}

void Variation_Changed(object s, SelectionChangedEventArgs e)
{
    CurrentVariation = VariationsList.SelectedItem as Variation;
    RenderTags();
}

void RemoveVariation_Click(object s, RoutedEventArgs e)
{
    if (CurrentItem is null ||
        ((FrameworkElement)s).Tag is not Variation variation)
        return;

    AddToUnassignedIfUnused(variation.MinusAssetId, variation);
    AddToUnassignedIfUnused(variation.PlusAssetId, variation);
    AddToUnassignedIfUnused(variation.AllAssetId, variation);
    CurrentItem.Variations.Remove(variation);
    if (ReferenceEquals(CurrentVariation, variation))
        CurrentVariation = null;

    RefreshItemAssets();
    RefreshImportedAssets();
    LoadItem();
}

void Variation_Drop(object s, System.Windows.DragEventArgs e)
{
    e.Handled = true;

    if (CurrentItem is null ||
        ((FrameworkElement)s).DataContext is not Variation target ||
        e.Data.GetData(typeof(List<ImageAsset>)) is not List<ImageAsset> assets ||
        assets.Count != 1)
        return;

    var targetSide = target.AllAssetId is null
        ? (target.MinusAssetId is null ? "minus" : "plus")
        : null;

    if (targetSide is null)
    {
        System.Windows.MessageBox.Show(
            "This variation already uses All. Clear it before adding Minus/Plus."
        );
        return;
    }

    SetVariationAsset(target, targetSide, assets[0]);
}

void Variation_DragOver(object s, System.Windows.DragEventArgs e)
{
    if (((FrameworkElement)s).DataContext is Variation &&
        e.Data.GetData(typeof(List<ImageAsset>)) is List<ImageAsset> assets &&
        assets.Count == 1)
    {
        e.Effects = System.Windows.DragDropEffects.Move;
    }
    else
    {
        e.Effects = System.Windows.DragDropEffects.None;
    }

    e.Handled = true;
}

void VariationSlot_Drop(object s, System.Windows.DragEventArgs e)
{
    e.Handled = true;
    if (CurrentItem is null ||
        ((FrameworkElement)s).DataContext is not Variation target ||
        ((FrameworkElement)s).Tag is not string side ||
        e.Data.GetData(typeof(List<ImageAsset>)) is not List<ImageAsset> assets ||
        assets.Count != 1)
        return;

    SetVariationAsset(target, side, assets[0]);
}

void VariationSlot_DragOver(object s, System.Windows.DragEventArgs e)
{
    if (((FrameworkElement)s).DataContext is Variation &&
        ((FrameworkElement)s).Tag is string side &&
        side is "minus" or "plus" or "all" &&
        e.Data.GetData(typeof(List<ImageAsset>)) is List<ImageAsset> assets &&
        assets.Count == 1)
        e.Effects = System.Windows.DragDropEffects.Move;
    else
        e.Effects = System.Windows.DragDropEffects.None;

    e.Handled = true;
}

void ClearVariationSlot_Click(object s, RoutedEventArgs e)
{
    if (CurrentItem is null ||
        ((FrameworkElement)s).DataContext is not Variation variation ||
        ((FrameworkElement)s).Tag is not string side)
        return;

    ClearVariationSlot(variation, side);
}

void SetVariationAsset(Variation variation, string side, ImageAsset asset)
{
    if (CurrentItem is null)
        return;

    if (side == "all" &&
        (variation.MinusAssetId is not null || variation.PlusAssetId is not null))
    {
        System.Windows.MessageBox.Show(
            "This variation already uses Minus/Plus. Clear them before assigning All."
        );
        return;
    }

    if (side is "minus" or "plus" && variation.AllAssetId is not null)
    {
        System.Windows.MessageBox.Show(
            "This variation already uses All. Clear it before assigning Minus/Plus."
        );
        return;
    }

    var oldId = side switch
    {
        "minus" => variation.MinusAssetId,
        "plus" => variation.PlusAssetId,
        "all" => variation.AllAssetId,
        _ => null
    };

    if (oldId == asset.Id)
        return;

    if (oldId is not null)
        AddToUnassignedIfUnused(oldId, variation);

    switch (side)
    {
        case "minus":
            variation.MinusAssetId = asset.Id;
            variation.MinusThumbnail = asset.Thumbnail;
            break;
        case "plus":
            variation.PlusAssetId = asset.Id;
            variation.PlusThumbnail = asset.Thumbnail;
            break;
        case "all":
            variation.AllAssetId = asset.Id;
            variation.AllThumbnail = asset.Thumbnail;
            break;
        default:
            return;
    }

    CurrentItem.UnassignedAssetIds.Remove(asset.Id);
    RefreshImportedAssets();
    RefreshItemAssets();
    VariationsList.Items.Refresh();
    ItemsList.Items.Refresh();
    StatusText.Text = $"PNG assigned to {side}.";
}

void ClearVariationSlot(Variation variation, string side)
{
    var assetId = side switch
    {
        "minus" => variation.MinusAssetId,
        "plus" => variation.PlusAssetId,
        "all" => variation.AllAssetId,
        _ => null
    };

    if (assetId is null)
        return;

    switch (side)
    {
        case "minus":
            variation.MinusAssetId = null;
            variation.MinusThumbnail = null;
            break;
        case "plus":
            variation.PlusAssetId = null;
            variation.PlusThumbnail = null;
            break;
        case "all":
            variation.AllAssetId = null;
            variation.AllThumbnail = null;
            break;
        default:
            return;
    }

    AddToUnassignedIfUnused(assetId, variation);
    RefreshItemAssets();
    RefreshImportedAssets();
    VariationsList.Items.Refresh();
    ItemsList.Items.Refresh();
}

void Item_Drop(object s, System.Windows.DragEventArgs e)
{
    e.Handled = true;
    if (((FrameworkElement)s).DataContext is not OrganizerItem target)
        return;

    if (e.Data.GetData(typeof(OrganizerItem)) is OrganizerItem sourceItem)
    {
        if (ReferenceEquals(sourceItem, target))
            return;

        if (!ConfirmCombine(sourceItem, target))
            return;

        CombineItems(sourceItem, target);
        return;
    }

    if (e.Data.GetData(typeof(List<ImageAsset>)) is not List<ImageAsset> assets ||
        assets.Count == 0)
        return;

    foreach (var asset in assets)
        if (!ItemContainsAsset(target, asset.Id))
            target.UnassignedAssetIds.Add(asset.Id);

    CurrentItem = target;
    ItemsList.SelectedItem = target;
    LoadItem();
    RefreshImportedAssets();
    StatusText.Text = assets.Count == 1
        ? "PNG added to the item."
        : $"{assets.Count} PNGs added to the item.";
}

void Item_DragOver(object s, System.Windows.DragEventArgs e)
{
    if (((FrameworkElement)s).DataContext is OrganizerItem target &&
        e.Data.GetData(typeof(OrganizerItem)) is OrganizerItem source &&
        !ReferenceEquals(source, target))
    {
        e.Effects = System.Windows.DragDropEffects.Move;
    }
    else if (((FrameworkElement)s).DataContext is OrganizerItem &&
             e.Data.GetData(typeof(List<ImageAsset>)) is List<ImageAsset> assets &&
             assets.Count > 0)
    {
        e.Effects = System.Windows.DragDropEffects.Move;
    }
    else
        e.Effects = System.Windows.DragDropEffects.None;

    e.Handled = true;
}

void ItemsList_DragOver(object s, System.Windows.DragEventArgs e)
{
    e.Effects = e.Data.GetData(typeof(List<ImageAsset>)) is List<ImageAsset> assets &&
                assets.Count > 0
        ? System.Windows.DragDropEffects.Move
        : System.Windows.DragDropEffects.None;
    e.Handled = true;
}

void ItemsList_Drop(object s, System.Windows.DragEventArgs e)
{
    e.Handled = true;
    if (e.Data.GetData(typeof(List<ImageAsset>)) is not List<ImageAsset> assets ||
        assets.Count == 0)
        return;

    var item = new OrganizerItem
    {
        Name = "New item",
        PreviewAssetId = assets[0].Id,
        PreviewThumbnail = assets[0].Thumbnail
    };

    foreach (var asset in assets)
        item.UnassignedAssetIds.Add(asset.Id);

    Project.Items.Add(item);
    CurrentItem = item;
    ItemsList.SelectedItem = item;
    LoadItem();
    RefreshImportedAssets();
    StatusText.Text = assets.Count == 1
        ? "New item created from the PNG."
        : $"New item created from {assets.Count} PNGs.";
}

void ItemsList_MouseMove(object s, System.Windows.Input.MouseEventArgs e)
{
    if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed ||
        ItemsList.SelectedItem is not OrganizerItem item)
        return;

    System.Windows.DragDrop.DoDragDrop(
        ItemsList,
        item,
        System.Windows.DragDropEffects.Move);
}
void ItemsList_PreviewMouseWheel(object s, System.Windows.Input.MouseWheelEventArgs e)
{
    var scrollViewer = FindDescendant<System.Windows.Controls.ScrollViewer>(ItemsList);
    if (scrollViewer is null)
        return;

    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
    e.Handled = true;
}

static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
{
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
    {
        var child = VisualTreeHelper.GetChild(root, i);
        if (child is T match)
            return match;

        var descendant = FindDescendant<T>(child);
        if (descendant is not null)
            return descendant;
    }

    return null;
}

bool ConfirmCombine(OrganizerItem source, OrganizerItem target)
{
    var sourceAsset = FirstItemAsset(source);
    var targetAsset = FirstItemAsset(target);
    var result = false;
    var dialog = new Window
    {
        Title = "Combine items",
        Owner = this,
        Width = 500,
        Height = 300,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize
    };

    var root = new StackPanel { Margin = new Thickness(18) };
    root.Children.Add(new TextBlock
    {
        Text = "Are you sure you want to combine these two items into one?",
        FontSize = 16,
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap
    });

    var previews = new StackPanel
    {
        Orientation = System.Windows.Controls.Orientation.Horizontal,
        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        Margin = new Thickness(0, 16, 0, 16)
    };
    previews.Children.Add(CombinePreview(source, sourceAsset));
    previews.Children.Add(CombinePreview(target, targetAsset));
    root.Children.Add(previews);

    var buttons = new StackPanel
    {
        Orientation = System.Windows.Controls.Orientation.Horizontal,
        HorizontalAlignment = System.Windows.HorizontalAlignment.Right
    };
    var yes = new System.Windows.Controls.Button { Content = "Yes", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
    var no = new System.Windows.Controls.Button { Content = "No", Width = 80 };
    yes.Click += (_, _) => { result = true; dialog.Close(); };
    no.Click += (_, _) => dialog.Close();
    buttons.Children.Add(yes);
    buttons.Children.Add(no);
    root.Children.Add(buttons);
    dialog.Content = root;
    dialog.ShowDialog();
    return result;
}

StackPanel CombinePreview(OrganizerItem item, ImageAsset? asset)
{
    var panel = new StackPanel
    {
        Width = 190,
        Margin = new Thickness(8)
    };
    panel.Children.Add(new System.Windows.Controls.Image
    {
        Source = asset?.Thumbnail,
        Width = 110,
        Height = 110,
        Stretch = Stretch.Uniform
    });
    panel.Children.Add(new TextBlock
    {
        Text = item.Name,
        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    });
    return panel;
}

ImageAsset? FirstItemAsset(OrganizerItem item)
{
    var id = item.PreviewAssetId
        ?? item.UnassignedAssetIds.FirstOrDefault()
        ?? item.Variations.SelectMany(v => new[] { v.MinusAssetId, v.PlusAssetId, v.AllAssetId })
            .FirstOrDefault(assetId => assetId is not null);
    return id is null ? null : Asset(id);
}

void CombineItems(OrganizerItem source, OrganizerItem target)
{
    foreach (var variation in source.Variations)
    {
        if (!target.Variations.Contains(variation))
            target.Variations.Add(variation);
    }

    if (target.PreviewAssetId is null && source.PreviewAssetId is not null)
    {
        target.PreviewAssetId = source.PreviewAssetId;
        target.PreviewThumbnail = source.PreviewThumbnail;
    }

    var sourceAssetIds = new[] { source.PreviewAssetId }
        .Concat(source.UnassignedAssetIds)
        .Concat(source.Variations.SelectMany(v => new[] { v.MinusAssetId, v.PlusAssetId, v.AllAssetId }))
        .Where(id => id is not null)
        .Cast<string>()
        .Distinct();

    foreach (var assetId in sourceAssetIds)
        if (!ItemContainsAsset(target, assetId))
            target.UnassignedAssetIds.Add(assetId);

    Project.Items.Remove(source);
    CurrentItem = target;
    ItemsList.SelectedItem = target;
    LoadItem();
    RefreshImportedAssets();
    StatusText.Text = $"Combined '{source.Name}' with '{target.Name}'.";
}

bool ItemContainsAsset(OrganizerItem item, string assetId)
{
    return item.PreviewAssetId == assetId ||
        item.UnassignedAssetIds.Contains(assetId) ||
        item.Variations.Any(v =>
            v.MinusAssetId == assetId ||
            v.PlusAssetId == assetId ||
            v.AllAssetId == assetId);
}

void AddToUnassignedIfUnused(string? assetId, Variation excludingVariation)
{
    if (CurrentItem is null || assetId is null)
        return;

    var stillUsed = CurrentItem.Variations
        .Where(v => !ReferenceEquals(v, excludingVariation))
        .Any(v => v.MinusAssetId == assetId || v.PlusAssetId == assetId || v.AllAssetId == assetId);

    if (!stillUsed && !CurrentItem.UnassignedAssetIds.Contains(assetId))
        CurrentItem.UnassignedAssetIds.Add(assetId);
}

void RenderVariations()
{
    if (CurrentItem is null)
    {
        VariationsList.ItemsSource = null;
        return;
    }

    VariationsList.ItemsSource = CurrentItem.Variations;
    VariationsList.Items.Refresh();
}
void AssetsList_MouseMove(object s, System.Windows.Input.MouseEventArgs e)
{
    if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        return;

    if (IsWithinScrollBar(e.OriginalSource as DependencyObject))
        return;

    var selected = SelectedAssets();

    if (selected.Count == 0)
    {
        return;
    }

    System.Windows.DragDrop.DoDragDrop(
        AssetsList,
        selected,
        System.Windows.DragDropEffects.Move
    );
}
void ItemAssetsList_MouseMove(object s, System.Windows.Input.MouseEventArgs e)
{
    if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        return;

    if (IsWithinScrollBar(e.OriginalSource as DependencyObject))
        return;

    var selected = UnassignedList.SelectedItems
        .Cast<ImageAsset>()
        .ToList();

    if (selected.Count == 0)
    {
        return;
    }

    System.Windows.DragDrop.DoDragDrop(
        UnassignedList,
        selected,
        System.Windows.DragDropEffects.Move
    );
}
static bool IsWithinScrollBar(DependencyObject? source)
{
    while (source is not null)
    {
        if (source is System.Windows.Controls.Primitives.ScrollBar)
            return true;

        source = VisualTreeHelper.GetParent(source);
    }

    return false;
}
void ItemAssetList_DragOver(object s, System.Windows.DragEventArgs e)
{
    e.Effects = e.Data.GetData(typeof(List<ImageAsset>)) is List<ImageAsset> assets && assets.Count > 0
        ? System.Windows.DragDropEffects.Move
        : System.Windows.DragDropEffects.None;
    e.Handled = true;
}
void ItemAssetList_Drop(object s, System.Windows.DragEventArgs e)
{
    e.Handled = true;
    if (CurrentItem is null ||
        e.Data.GetData(typeof(List<ImageAsset>)) is not List<ImageAsset> assets ||
        assets.Count == 0)
        return;

    foreach (var asset in assets)
        if (!ItemContainsAsset(CurrentItem, asset.Id))
            CurrentItem.UnassignedAssetIds.Add(asset.Id);
    RefreshItemAssets();
    RefreshImportedAssets();
    ItemsList.Items.Refresh();
}
    void RenderTags(){TagPanel.Children.Clear();foreach(var tag in Project.Settings.Tags){var box=new System.Windows.Controls.CheckBox{Content=tag,IsChecked=CurrentVariation?.Tags.Contains(tag)??false,IsEnabled=CurrentVariation is not null,Margin=new Thickness(0,0,10,4)};box.Checked+=(s,e)=>SetTag(tag,true);box.Unchecked+=(s,e)=>SetTag(tag,false);TagPanel.Children.Add(box);}}
    void SetTag(string tag,bool add){if(CurrentVariation is null)return;if(add&&!CurrentVariation.Tags.Contains(tag))CurrentVariation.Tags.Add(tag);if(!add)CurrentVariation.Tags.Remove(tag);VariationsList.Items.Refresh();}
    void Settings_Changed(object s,RoutedEventArgs e) { ApplySettingsFields(); }
    void ApplySettingsFields()
    {
        Project.Settings.Tags = new(TagsConfig.Text.Split(["\r\n","\n"],StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries));
        var tree = new Dictionary<string,ObservableCollection<string>>();
        foreach (var line in CategoriesConfig.Text.Split(["\r\n","\n"],StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':',2);
            if (parts.Length == 2) tree[parts[0].Trim()] = new(parts[1].Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries));
        }
        if (tree.Any()) Project.Settings.CategoryTree = tree;
        Project.Settings.FilenameTemplate = FilenameTemplateBox.Text.Trim();
        MainCategoryBox.ItemsSource = Project.Settings.CategoryTree.Keys;
        RenderTags();
    }
    void SettingsSave_Click(object s, RoutedEventArgs e) { ApplySettingsFields(); StatusText.Text = "Settings updated. Save the project to keep them permanently."; }
    void SettingsReset_Click(object s, RoutedEventArgs e)
    {
        Project.Settings = new SettingsData();
        TagsConfig.Text = string.Join(Environment.NewLine, Project.Settings.Tags);
        CategoriesConfig.Text = string.Join(Environment.NewLine, Project.Settings.CategoryTree.Select(x => $"{x.Key}: {string.Join(", ", x.Value)}"));
        FilenameTemplateBox.Text = Project.Settings.FilenameTemplate;
        ApplySettingsFields();
    }
    void Help_Click(object s,RoutedEventArgs e)=>System.Windows.MessageBox.Show($"Everskies Studio Organizer version {AppVersion}\n\n1. Import a folder of PNGs.\n2. Select PNGs and create an item, or drag them directly onto an existing item or an empty area in the item overview.\n3. Create variations manually and assign Minus, Plus, or All by dragging one PNG onto the desired target.\n4. Select a variation to edit its tags.\n5. Validate before exporting. Export creates a tagged assets folder containing item previews and each item's variation PNGs.\n\nSuggestions compare visible alpha shapes and colours; accepting a suggestion only adds free assets to a new item. It never creates a variation automatically.","How this works");
    void Settings_Click(object s,RoutedEventArgs e)
    {
        if (SettingsExpander.Parent is not System.Windows.Controls.Panel parent)
            return;

        parent.Children.Remove(SettingsExpander);
        SettingsExpander.Visibility = Visibility.Visible;

        var dialog = new Window
        {
            Title = "Settings",
            Owner = this,
            Width = 520,
            Height = 520,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)Resources["AppBackgroundBrush"],
            Foreground = (System.Windows.Media.Brush)Resources["AppTextBrush"],
            Content = new Border
            {
                Background = (System.Windows.Media.Brush)Resources["AppPanelBrush"],
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18),
                Child = SettingsExpander
            }
        };
        dialog.Closed += (_, _) =>
        {
            ((Border)dialog.Content).Child = null;
            SettingsExpander.Visibility = Visibility.Collapsed;
            parent.Children.Add(SettingsExpander);
        };
        dialog.ShowDialog();
    }
    void Suggestions_Click(object s, RoutedEventArgs e)
    {
        var assignedIds = Project.Items
            .SelectMany(item => new[] { item.PreviewAssetId }
                .Concat(item.UnassignedAssetIds)
                .Concat(item.Variations.SelectMany(v => new[] { v.MinusAssetId, v.PlusAssetId, v.AllAssetId })))
            .Where(id => id is not null)
            .ToHashSet();
        var source = SelectedAssets().Where(asset => !assignedIds.Contains(asset.Id)).ToList();
        if (source.Count < 2)
            source = Project.Assets.Where(asset => !assignedIds.Contains(asset.Id)).ToList();

        if (source.Count < 2)
        {
            System.Windows.MessageBox.Show("Import at least two PNGs first.");
            return;
        }

        var groups = SuggestionService.SimilarGroups(SuggestionService.Inspect(source));
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock
        {
            Text = "Similar asset groups",
            FontSize = 22,
            FontWeight = FontWeights.Bold
        });
        panel.Children.Add(new TextBlock
        {
            Text = "These suggestions compare alpha shapes only. Each group is shown separately so different possible items stay distinct.",
            Margin = new Thickness(0, 8, 0, 12),
            TextWrapping = TextWrapping.Wrap
        });
        var acceptAll = new System.Windows.Controls.Button
        {
            Content = "Accept all",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 14)
        };
        panel.Children.Add(acceptAll);

        foreach (var (group, index) in groups.Select((group, index) => (group, index + 1)))
        {
            var groupPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 14), Tag = group };
            groupPanel.Children.Add(new TextBlock
            {
                Text = $"Suggestion group {index} ({group.Count} assets) — palette: {string.Join(", ", group.Select(x => x.ColorTag).Distinct())}",
                FontWeight = FontWeights.Bold
            });

            var cards = new WrapPanel();
            foreach (var insight in group)
            {
                var card = SuggestionCard(insight.Asset, $"{insight.Asset.DisplayName}\nSuggested colour: {insight.ColorTag}");
                var remove = new System.Windows.Controls.Button { Content = "×", Width = 20, Height = 20, Padding = new Thickness(0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right, VerticalAlignment = System.Windows.VerticalAlignment.Top };
                remove.Click += (_, _) =>
                {
                    group.Remove(insight);
                    cards.Children.Remove(card);
                };
                if (card.Child is StackPanel cardPanel)
                {
                    var content = cardPanel.Children.Cast<UIElement>().ToList();
                    cardPanel.Children.Clear();
                    var header = new DockPanel();
                    DockPanel.SetDock(remove, Dock.Right);
                    header.Children.Add(remove);
                    cardPanel.Children.Add(header);
                    foreach (var child in content)
                        cardPanel.Children.Add(child);
                }
                cards.Children.Add(card);
            }
            groupPanel.Children.Add(cards);

            var accept = new System.Windows.Controls.Button
            {
                Content = "Accept as new item",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Tag = group
            };
            accept.Click += SuggestionGroupAccept_Click;
            groupPanel.Children.Add(accept);
            panel.Children.Add(groupPanel);
        }
        acceptAll.Click += (_, _) =>
        {
            foreach (var group in groups)
                AcceptSuggestionGroup(group);

            foreach (var groupPanel in panel.Children.OfType<StackPanel>())
                groupPanel.Visibility = Visibility.Collapsed;

            acceptAll.Visibility = Visibility.Collapsed;
        };

        if (groups.Count == 0)
            panel.Children.Add(new TextBlock { Text = "No confident recolour groups found." });

        var dialog = new Window
        {
            Title = "Asset suggestions",
            Owner = this,
            Width = 720,
            Height = 650,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)Resources["AppBackgroundBrush"],
            Foreground = (System.Windows.Media.Brush)Resources["AppTextBrush"],
            Resources = Resources,
            Content = new ScrollViewer { Content = panel }
        };
        var close = new System.Windows.Controls.Button
        {
            Content = "Close",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 0)
        };
        close.Click += (_, _) => dialog.Close();
        panel.Children.Add(close);
        dialog.ShowDialog();
    }
    void SuggestionGroupAccept_Click(object s, RoutedEventArgs e)
    {
        if ((s as FrameworkElement)?.Tag is not List<AssetInsight> group || group.Count == 0)
            return;

        AcceptSuggestionGroup(group);
        if (s is FrameworkElement button &&
            button.Parent is StackPanel groupPanel)
            groupPanel.Visibility = Visibility.Collapsed;
    }

    void AcceptSuggestionGroup(List<AssetInsight> group)
    {
        var item = new OrganizerItem
        {
            Name = "Suggested item — review",
            PreviewAssetId = group[0].Asset.Id,
            PreviewThumbnail = group[0].Asset.Thumbnail
        };
        foreach (var insight in group)
            item.UnassignedAssetIds.Add(insight.Asset.Id);

        Project.Items.Add(item);
        RefreshImportedAssets();
        ItemsList.SelectedItem = item;
    }
    Border SuggestionCard(ImageAsset asset,string label)=>new(){BorderBrush=(System.Windows.Media.Brush)Resources["AppBorderBrush"],Background=(System.Windows.Media.Brush)Resources["AppCardBrush"],BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Margin=new Thickness(0,0,8,8),Padding=new Thickness(5),Child=new StackPanel{Width=100,Children={new System.Windows.Controls.Image{Source=asset.Thumbnail,Height=72,Stretch=System.Windows.Media.Stretch.Uniform},new TextBlock{Text=label,FontSize=10,TextWrapping=TextWrapping.Wrap}}}};
    void Save_Click(object s,RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Everskies Organizer project (*.eskproj)|*.eskproj|All files (*.*)|*.*",
            DefaultExt = ".eskproj",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            ProjectStore.Save(Project, dialog.FileName);
            StatusText.Text = "Project saved.";
            System.Windows.MessageBox.Show($"Project saved to:\n{dialog.FileName}", "Project saved");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"The project could not be saved:\n\n{ex.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    void Open_Click(object s,RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Everskies Organizer project (*.eskproj)|*.eskproj|All files (*.*)|*.*",
            DefaultExt = ".eskproj",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            Project=ProjectStore.Load(dialog.FileName);
            foreach(var a in Project.Assets)
                a.Thumbnail=ThumbnailService.FromPng(a.Path);
            HydrateVisuals();
            ItemsList.ItemsSource=Project.Items;
            RefreshImportedAssets();
            CurrentItem=null;
            EditorPanel.IsEnabled=false;
            StatusText.Text="Project opened. Missing source paths are reported during validation.";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"The project could not be opened:\n\n{ex.Message}", "Open error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    void Export_Click(object s,RoutedEventArgs e)
    {
        var errors=Validator.Check(Project);
        if(errors.Any()){System.Windows.MessageBox.Show("Fix these before export:\n\n"+string.Join("\n",errors),"Validation",System.Windows.MessageBoxButton.OK,System.Windows.MessageBoxImage.Warning);return;}
        string folder;
        using (var dialog = new WinForms.FolderBrowserDialog { Description = "Select target folder for tagged assets", UseDescriptionForTitle = true })
        {
            if (dialog.ShowDialog() != WinForms.DialogResult.OK) return;
            folder = dialog.SelectedPath;
        }
        Exporter.Export(Project, folder);
        StatusText.Text="Export complete. Original PNGs were copied without modification.";
        System.Windows.MessageBox.Show("Export complete. Files were written to 'tagged assets'.","Everskies Studio Organizer");
    }
    void Theme_Click(object s,RoutedEventArgs e){Project.Settings.DarkMode=!Project.Settings.DarkMode;ApplyTheme();} void ApplyTheme(){var dark=Project.Settings.DarkMode;Background=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(28,31,36):System.Windows.Media.Color.FromRgb(241,242,247));Foreground=dark?System.Windows.Media.Brushes.White:System.Windows.Media.Brushes.Black;Resources["AppBackgroundBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(28,31,36):System.Windows.Media.Color.FromRgb(241,242,247));Resources["AppPanelBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(43,47,54):System.Windows.Media.Color.FromRgb(255,255,255));Resources["AppSurfaceBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(52,57,65):System.Windows.Media.Color.FromRgb(229,224,240));Resources["AppTextBrush"]=dark?System.Windows.Media.Brushes.White:System.Windows.Media.Brushes.Black;Resources["AppBorderBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(82,89,101):System.Windows.Media.Color.FromRgb(205,199,220));Resources["AppCardBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(48,53,61):System.Windows.Media.Color.FromRgb(233,237,241));Resources["AppSlotBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(55,49,63):System.Windows.Media.Color.FromRgb(244,241,247));Resources["AppThumbnailBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(42,47,54):System.Windows.Media.Color.FromRgb(238,240,243));Resources["AppAccentBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(112,91,158):System.Windows.Media.Color.FromRgb(146,122,180));Resources["AppSuccessBrush"]=new System.Windows.Media.SolidColorBrush(dark?System.Windows.Media.Color.FromRgb(63,139,105):System.Windows.Media.Color.FromRgb(88,184,138));}
    void Window_Drop(object s,System.Windows.DragEventArgs e){if(e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths){var dir=paths.FirstOrDefault(Directory.Exists);if(dir is not null)ImportFolder(dir);}}
}