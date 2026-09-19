using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

namespace EverskiesOrganizer;

public static class ThumbnailService {
    // Crop only the bitmap presented in the UI; source PNGs are never edited.
    public static ImageSource? FromPng(string path) {
        try {
            var decoder = new PngBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var source = decoder.Frames[0];
            var pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            converted.CopyPixels(pixels, source.PixelWidth * 4, 0);
            int minX = source.PixelWidth, minY = source.PixelHeight, maxX = -1, maxY = -1;
            for (var y=0; y<source.PixelHeight; y++) for (var x=0; x<source.PixelWidth; x++) if (pixels[(y*source.PixelWidth+x)*4+3] > 8) { minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y); }
            BitmapSource result = maxX >= minX ? new CroppedBitmap(converted, new Int32Rect(minX,minY,maxX-minX+1,maxY-minY+1)) : converted;
            result.Freeze(); return result;
        } catch { return null; }
    }
}
public sealed record AssetInsight(ImageAsset Asset, int Width, int Height, double Coverage, string ColorTag);
public static class SuggestionService {
    public static List<AssetInsight> Inspect(IEnumerable<ImageAsset> assets) => assets.Select(Read).Where(x=>x is not null).Cast<AssetInsight>().ToList();
    static AssetInsight? Read(ImageAsset asset) {
        try {
            var frame=new PngBitmapDecoder(new Uri(asset.Path),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];var img=new FormatConvertedBitmap(frame,PixelFormats.Bgra32,null,0);var p=new byte[img.PixelWidth*img.PixelHeight*4];img.CopyPixels(p,img.PixelWidth*4,0);
            var minX=img.PixelWidth;var minY=img.PixelHeight;var maxX=-1;var maxY=-1;long count=0;
            for(var y=0;y<img.PixelHeight;y++)for(var x=0;x<img.PixelWidth;x++){var i=(y*img.PixelWidth+x)*4;if(p[i+3]>16){count++;minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}}
            if(count==0)return new AssetInsight(asset,0,0,0,"Multi");
            var w=maxX-minX+1;var h=maxY-minY+1;var inset=Math.Max(2,Math.Min(w,h)/10);long interiorCount=0, r=0,g=0,b=0;
            for(var y=minY+inset;y<=maxY-inset;y++)for(var x=minX+inset;x<=maxX-inset;x++){var i=(y*img.PixelWidth+x)*4;if(p[i+3]>96){interiorCount++;b+=p[i];g+=p[i+1];r+=p[i+2];}}
            if(interiorCount==0){for(var y=minY;y<=maxY;y++)for(var x=minX;x<=maxX;x++){var i=(y*img.PixelWidth+x)*4;if(p[i+3]>96){interiorCount++;b+=p[i];g+=p[i+1];r+=p[i+2];}}}
            return new AssetInsight(asset,w,h,(double)count/(w*h),Color(r/interiorCount,g/interiorCount,b/interiorCount));
        }catch{return null;}
    }
    static string Color(long r,long g,long b)
    {
        var max=Math.Max(r,Math.Max(g,b));
        var min=Math.Min(r,Math.Min(g,b));
        var saturation=max == 0 ? 0 : (max-min)/(double)max;
        var lightness=(max+min)/2d;
        if(max<45)return "Black";
        if(lightness>225 && saturation<.18)return "White";
        if(saturation<.16)return lightness<145 ? "Grey" : "White";

        var hue=Math.Atan2(Math.Sqrt(3)*(g-b),2*r-g-b)*180/Math.PI;
        if(hue<0)hue+=360;

        if(lightness<120 && hue>=15 && hue<65 && r>=g*.8)return "Brown";
        if(lightness>=120 && lightness<205 && saturation<.48 && hue>=15 && hue<70)return "Beige";
        if(hue>=15 && hue<50 && r>g*1.12 && g>b*1.15)return "Copper";
        return hue switch {>=330 or <15=>"Red",<45=>"Orange",<65=>"Yellow",<160=>"Green",<200=>"Turquoise",<250=>"Blue",<295=>"Purple",<330=>"Pink",_=>"Multi"};
    }
    public static double ShapeScore(AssetInsight a,AssetInsight b){if(a.Width==0||b.Width==0)return 0;var ratioA=(double)a.Width/a.Height;var ratioB=(double)b.Width/b.Height;var ratio=1-Math.Min(1,Math.Abs(ratioA-ratioB)/Math.Max(ratioA,ratioB));var cover=1-Math.Min(1,Math.Abs(a.Coverage-b.Coverage));return ratio*.65+cover*.35;}
    public static List<AssetInsight> SimilarGroup(List<AssetInsight> all){var best=new List<AssetInsight>();foreach(var seed in all){var group=all.Where(x=>ShapeScore(seed,x)>=.93).ToList();if(group.Count>best.Count)best=group;}return best.Count>1?best:[];}
    public static List<List<AssetInsight>> SimilarGroups(List<AssetInsight> all)
    {
        var groups = new List<List<AssetInsight>>();
        var remaining = all.ToHashSet();

        while (remaining.Count > 0)
        {
            var seed = remaining.First();
            var group = new HashSet<AssetInsight> { seed };
            var pending = new Queue<AssetInsight>(new[] { seed });

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                foreach (var candidate in remaining.Where(x => !group.Contains(x)).ToList())
                {
                    if (ShapeScore(current, candidate) < .98)
                        continue;

                    group.Add(candidate);
                    pending.Enqueue(candidate);
                }
            }

            remaining.ExceptWith(group);
            if (group.Count > 1)
                groups.Add(group.ToList());
        }

        return groups.OrderByDescending(group => group.Count).ToList();
    }
    public static (AssetInsight,AssetInsight)? Counterpart(List<AssetInsight> all){(AssetInsight,AssetInsight)? best=null;double score=0;for(var i=0;i<all.Count;i++)for(var j=i+1;j<all.Count;j++){var s=ShapeScore(all[i],all[j]);var areaA=all[i].Width*all[i].Height;var areaB=all[j].Width*all[j].Height;if(areaA==0||areaB==0)continue;var size=Math.Abs(areaA-areaB)/(double)Math.Max(areaA,areaB);if(s>.88&&size is >.03 and <.45&&s>score){score=s;best=(all[i],all[j]);}}return best;}
}
public static class ProjectStore {
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static void Save(ProjectData data, string path) => File.WriteAllText(path, JsonSerializer.Serialize(data, Options));
    public static ProjectData Load(string path) => JsonSerializer.Deserialize<ProjectData>(File.ReadAllText(path), Options) ?? new();
}
public static class FilenameGenerator {
    public static string Make(OrganizerItem item, Variation v, string body, string? template = null, string? itemName = null) {
        var category = string.Join("_", item.Category
            .Split(" → ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(CleanWithSpaces));
        var tags = string.Join("_", v.Tags.Select(Clean));
        var variationNumber = item.Variations.IndexOf(v) + 1;
        var format = template ?? "category_{category}-body_{body}-counterpart_{counterpart}-tags_{tags}-{item}";
        if (format.Contains("{tags}") && !format.Contains("tags_{tags}"))
            format = format.Replace("{tags}", "tags_{tags}");
        if (!format.Contains("category_"))
            format = format.Replace("{category}", "category_{category}");
        var name = format.Replace("{category}", category).Replace("{body}", body).Replace("{counterpart}", variationNumber.ToString()).Replace("{tags}", tags).Replace("{item}", Clean(itemName ?? item.Name));
        return string.Join("-", name.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) + ".png";
    }
    public static string Clean(string value) => string.Concat(value.Trim().Replace(' ', '_').Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? char.ToLowerInvariant(c) : '_')).Trim('_');
    static string CleanWithSpaces(string value) => string.Concat(value.Trim().Select(c => char.IsLetterOrDigit(c) || c is ' ' or '_' or '-' ? char.ToLowerInvariant(c) : '_')).Trim();
}
public static class Validator {
    public static List<string> Check(ProjectData project) {
        var errors = new List<string>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nameCounts = project.Items.GroupBy(item => FilenameGenerator.Clean(item.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var seenNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in project.Items) {
            var title = string.IsNullOrWhiteSpace(item.Name) ? "Unnamed item" : item.Name;
            var cleanName = FilenameGenerator.Clean(item.Name);
            seenNames.TryGetValue(cleanName, out var occurrence);
            seenNames[cleanName] = occurrence + 1;
            var exportName = nameCounts[cleanName] > 1 && occurrence > 0 ? $"{cleanName} ({occurrence + 1})" : cleanName;
            if (string.IsNullOrWhiteSpace(item.Name)) errors.Add("An item is missing a name.");
            if (string.IsNullOrWhiteSpace(item.Category)) errors.Add($"Item '{title}' is missing a category.");
            if (item.PreviewAssetId is null) errors.Add($"Item '{title}' is missing a preview.");
            if (!project.Assets.Any(a => a.Id == item.PreviewAssetId && a.Exists)) errors.Add($"Item '{title}' has a missing preview source file.");
            if (item.Variations.Count == 0)
    errors.Add($"Item '{title}' has no variations.");

if (item.Variations.Count > 7)
    errors.Add($"Item '{title}' has {item.Variations.Count}/7 variations.");

foreach (var v in item.Variations)
{
    if (v.MinusAssetId is null && v.PlusAssetId is null && v.AllAssetId is null)
        errors.Add($"Item '{title}' has an empty variation.");

    if (!v.Tags.Any())
        errors.Add($"Item '{title}' has a variation without tags.");

    if (v.AllAssetId is not null && (v.MinusAssetId is not null || v.PlusAssetId is not null))
        errors.Add($"Item '{title}' has a variation using All together with Minus/Plus.");

    foreach (var (id, body) in new[]
    {
        (v.MinusAssetId, "minus"),
        (v.PlusAssetId, "plus"),
        (v.AllAssetId, "all")
    })
    {
        if (id is not null)
        {
            if (!project.Assets.Any(a => a.Id == id && a.Exists))
                errors.Add($"Item '{title}' has a missing {body} source file.");

            var filename = FilenameGenerator.Make(
                item,
                v,
                body,
            project.Settings.FilenameTemplate,
            exportName);

            if (!names.Add(filename))
                errors.Add($"Duplicate output filename: {filename}");
        }
    }
}
        }
        return errors;
    }
}
public static class Exporter {
    public static void Export(ProjectData project, string root) {
        Directory.CreateDirectory(root);
        var tagged = Path.Combine(root, "tagged assets");
        Directory.CreateDirectory(tagged);
        var nameCounts = project.Items.GroupBy(item => FilenameGenerator.Clean(item.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var seenNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in project.Items) {
            var cleanName = FilenameGenerator.Clean(item.Name);
            seenNames.TryGetValue(cleanName, out var occurrence);
            seenNames[cleanName] = occurrence + 1;
            var exportName = nameCounts[cleanName] > 1 && occurrence > 0 ? $"{cleanName} ({occurrence + 1})" : cleanName;
            var folder = Path.Combine(tagged, exportName);
            Directory.CreateDirectory(folder);
            var preview = item.PreviewAssetId is null ? null : project.Assets.FirstOrDefault(a=>a.Id==item.PreviewAssetId);
            if (preview is not null)
            {
                var previewAssignment = item.Variations
                    .SelectMany(variation => new[]
                    {
                        (Variation: variation, Body: "minus", AssetId: variation.MinusAssetId),
                        (Variation: variation, Body: "plus", AssetId: variation.PlusAssetId),
                        (Variation: variation, Body: "all", AssetId: variation.AllAssetId)
                    })
                    .FirstOrDefault(assignment => assignment.AssetId == item.PreviewAssetId);
                var previewName = previewAssignment.AssetId is null
                    ? $"category_{FilenameGenerator.Clean(item.Category)}-body_all-counterpart_0-tags_-{exportName}.png"
                    : FilenameGenerator.Make(
                        item,
                        previewAssignment.Variation,
                        previewAssignment.Body,
                        project.Settings.FilenameTemplate,
                        exportName);
                File.Copy(preview.Path, Path.Combine(tagged, previewName), true);
            }
            foreach (var assetId in item.UnassignedAssetIds.Where(id => id != item.PreviewAssetId))
            {
                var asset = project.Assets.FirstOrDefault(x => x.Id == assetId);
                if (asset is not null)
                    File.Copy(asset.Path, Path.Combine(folder, asset.OriginalName), true);
            }
            foreach (var variation in item.Variations)
                foreach (var (id, body) in new[]{(variation.MinusAssetId, "minus"), (variation.PlusAssetId, "plus"), (variation.AllAssetId, "all")})
                    if (id is not null && id != item.PreviewAssetId)
                    {
                        var asset = project.Assets.FirstOrDefault(x => x.Id == id);
                        if (asset is not null)
                            File.Copy(asset.Path, Path.Combine(folder, FilenameGenerator.Make(item, variation, body, project.Settings.FilenameTemplate, exportName)), true);
                    }
        }
    }
}
