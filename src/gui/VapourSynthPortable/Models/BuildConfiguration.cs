namespace VapourSynthPortable.Models;

public class BuildConfiguration
{
    public string PluginSet { get; set; } = "standard";
    public List<string> Components { get; set; } = new();
    public bool Clean { get; set; }
    public string? OutputDirectory { get; set; }
}

public class BuildProgress
{
    public int Percent { get; set; }
    public string Operation { get; set; } = "";
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
}

public class BuildResult
{
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> InstalledPlugins { get; set; } = new();
    public List<string> FailedPlugins { get; set; } = new();
    public List<string> InstalledPackages { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
