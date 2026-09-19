using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.Json.Serialization;

namespace EverskiesOrganizer;

public abstract class Bindable : INotifyPropertyChanged {
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
public sealed class ImageAsset : Bindable {
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Path { get; set; } = "";
    public string OriginalName => System.IO.Path.GetFileName(Path);
    public string DisplayName => OriginalName.Length > 25 ? OriginalName[..22] + "…" : OriginalName;
    public string? Status { get; set; }
    public bool Exists => File.Exists(Path);
    [JsonIgnore] public System.Windows.Media.ImageSource? Thumbnail { get; set; }
    [JsonIgnore] public double ItemOpacity { get; set; } = 1;
}
public sealed class Variation : Bindable {
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? MinusAssetId { get; set; }
public string? PlusAssetId { get; set; }
public string? AllAssetId { get; set; }

public ObservableCollection<string> Tags { get; set; } = [];
    public string Label { get; set; } = "Variation";
    [JsonIgnore] public System.Windows.Media.ImageSource? MinusThumbnail { get; set; }
    [JsonIgnore] public System.Windows.Media.ImageSource? PlusThumbnail { get; set; }
    [JsonIgnore] public System.Windows.Media.ImageSource? AllThumbnail { get; set; }
    public string TagsText { get => string.Join(", ", Tags); set { Tags = new(value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)); Changed(); } }
}
public sealed class OrganizerItem : Bindable {
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New item";
    public string ItemType { get; set; } = "Normal";
    public string Category { get; set; } = "";
    public string MainCategory { get; set; } = "";
    public string SubCategory { get; set; } = "";
    public string? PreviewAssetId { get; set; }
    [JsonIgnore] public System.Windows.Media.ImageSource? PreviewThumbnail { get; set; }
    public ObservableCollection<Variation> Variations { get; set; } = [];
public ObservableCollection<string> UnassignedAssetIds { get; set; } = [];
}
public sealed class SettingsData {
    public ObservableCollection<string> Tags { get; set; } = new(["Beige","Black","Blonde","Blue","Brown","Copper","Ginger","Gold","Green","Grey","Multi","Orange","Pink","Purple","Red","Silver","Skin","Turquoise","White","Yellow"]);
    public Dictionary<string, ObservableCollection<string>> CategoryTree { get; set; } = new() {
        ["Skin"] = new(["Body", "Arms", "Legs", "Face", "Makeup", "Hair", "Tattoo", "Other", "Body Mods"]),
        ["Head"] = new(["Hair", "Hats", "Face", "Other", "Glasses", "Facial Hair", "Scarves"]),
        ["Upper Body"] = new(["Coats", "Long Dresses", "Short Dresses", "Other", "Sweaters", "Undershirts", "Short Sleeved", "Long Sleeved", "Jumpsuits"]),
        ["Lower Body"] = new(["Pants", "Skirts", "Leggings", "Other", "Shorts", "Underpants", "Legs"]),
        ["Feet"] = new(["Sneakers", "Boots", "Heels", "Socks", "Sandals", "Other", "Leggings"]),
        ["Accessories"] = new(["Handbags", "Scarves", "Other", "Belt", "Background Deco", "Gloves", "Face Jewellery", "Necklaces", "Earrings", "Bracelets", "Animals"])
    };
    public string FilenameTemplate { get; set; } = "category_{category}-body_{body}-counterpart_{counterpart}-tags_{tags}-{item}";
    public bool DarkMode { get; set; }
}
public sealed class ProjectData {
    public ObservableCollection<ImageAsset> Assets { get; set; } = [];
    public ObservableCollection<OrganizerItem> Items { get; set; } = [];
    public SettingsData Settings { get; set; } = new();
}
