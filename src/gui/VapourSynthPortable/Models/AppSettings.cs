namespace VapourSynthPortable.Models;

public class AppSettings
{
    public string OutputDirectory { get; set; } = "dist";
    public string CacheDirectory { get; set; } = "build";
    public string DefaultPluginSet { get; set; } = "standard";
    public string PythonVersion { get; set; } = "3.12.4";
    public string VapourSynthVersion { get; set; } = "R68";

    // Custom media bins
    public List<CustomBinSettings> CustomBins { get; set; } = [];
}

public class CustomBinSettings
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> ItemPaths { get; set; } = [];
}
