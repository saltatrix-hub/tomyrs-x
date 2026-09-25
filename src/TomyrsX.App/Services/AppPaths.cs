using System.IO;

namespace TomyrsX.Services;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string Vendor => Path.Combine(BaseDirectory, "vendor");
    public static string Win11Debloat => Path.Combine(Vendor, "Win11Debloat");
    public static string Tweaks => Path.Combine(Vendor, "tweaks");
    public static string Win11DebloatScript => Path.Combine(Win11Debloat, "Win11Debloat.ps1");
    public static string FeaturesJson => Path.Combine(Win11Debloat, "Config", "Features.json");
    public static string DefaultSettingsJson => Path.Combine(Win11Debloat, "Config", "DefaultSettings.json");
    public static string TweakCatalog => Path.Combine(BaseDirectory, "Resources", "tweaks.json");
    public static string TweakRunner => Path.Combine(BaseDirectory, "Resources", "RunTweak.ps1");
}
