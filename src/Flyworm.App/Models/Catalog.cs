using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flyworm.Models;

public sealed class FeatureFile
{
    [JsonPropertyName("Categories")] public List<FeatureCategory> Categories { get; set; } = [];
    [JsonPropertyName("UiGroups")] public List<UiGroup> UiGroups { get; set; } = [];
    [JsonPropertyName("Features")] public List<FeatureItem> Features { get; set; } = [];
}

public sealed class FeatureCategory
{
    [JsonPropertyName("Name")] public string Name { get; set; } = "";
}

public sealed class UiGroup
{
    [JsonPropertyName("GroupId")] public string GroupId { get; set; } = "";
    [JsonPropertyName("Label")] public string Label { get; set; } = "";
    [JsonPropertyName("ToolTip")] public string? ToolTip { get; set; }
    [JsonPropertyName("Category")] public string Category { get; set; } = "";
    [JsonPropertyName("Values")] public List<UiGroupValue> Values { get; set; } = [];
}

public sealed class UiGroupValue
{
    [JsonPropertyName("Label")] public string Label { get; set; } = "";
    [JsonPropertyName("FeatureIds")] public List<string> FeatureIds { get; set; } = [];
}

public sealed class FeatureItem
{
    [JsonPropertyName("FeatureId")] public string FeatureId { get; set; } = "";
    [JsonPropertyName("Label")] public string Label { get; set; } = "";
    [JsonPropertyName("ToolTip")] public string? ToolTip { get; set; }
    [JsonPropertyName("Category")] public string? Category { get; set; }
    [JsonPropertyName("Action")] public string? Action { get; set; }
}

public sealed class DefaultSettingsFile
{
    [JsonPropertyName("Settings")] public List<DefaultSetting> Settings { get; set; } = [];
}

public sealed class DefaultSetting
{
    [JsonPropertyName("Name")] public string Name { get; set; } = "";
    [JsonPropertyName("Value")] public bool Value { get; set; }
}

public sealed class TweakCatalog
{
    [JsonPropertyName("tweaks")] public List<TweakEntry> Tweaks { get; set; } = [];
    [JsonPropertyName("gpu")] public List<TweakEntry> Gpu { get; set; } = [];
}

public sealed class TweakEntry
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("file")] public string File { get; set; } = "";
    [JsonPropertyName("choice")] public string Choice { get; set; } = "1";
    [JsonPropertyName("order")] public int Order { get; set; }
    [JsonPropertyName("needsNet")] public bool NeedsNet { get; set; }
    [JsonPropertyName("opensSettings")] public bool OpensSettings { get; set; }
    [JsonPropertyName("dangerous")] public bool Dangerous { get; set; }
}

public sealed class SelectableOption
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "feature";
    public bool IsDangerous { get; init; }
    public bool OpensSettings { get; init; }
    public bool IsChecked { get; set; }
    public string? ScriptFile { get; init; }
    public string? Choice { get; init; }
}

public sealed class SelectableGroup
{
    public string GroupId { get; init; } = "";
    public string Category { get; init; } = "";
    public List<SelectableGroupValue> Values { get; init; } = [];
    public int SelectedIndex { get; set; }
}

public sealed class SelectableGroupValue
{
    public string LabelKey { get; init; } = "";
    public string Fallback { get; init; } = "";
    public string FeatureId { get; init; } = "";
}

public static class CatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static FeatureFile LoadFeatures(string path) =>
        JsonSerializer.Deserialize<FeatureFile>(File.ReadAllText(path), JsonOptions) ?? new FeatureFile();

    public static HashSet<string> LoadDefaultFeatureIds(string path)
    {
        var file = JsonSerializer.Deserialize<DefaultSettingsFile>(File.ReadAllText(path), JsonOptions);
        return file?.Settings.Where(s => s.Value).Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
               ?? [];
    }

    public static TweakCatalog LoadTweaks(string path) =>
        JsonSerializer.Deserialize<TweakCatalog>(File.ReadAllText(path), JsonOptions) ?? new TweakCatalog();
}
