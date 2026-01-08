using Newtonsoft.Json;

namespace VapourSynthPortable.Models;

public class Plugin
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("set")]
    public string Set { get; set; } = "standard";

    [JsonProperty("url")]
    public string Url { get; set; } = "";

    [JsonProperty("version")]
    public string Version { get; set; } = "";

    [JsonProperty("files")]
    public List<string> Files { get; set; } = new();

    [JsonProperty("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

public class PluginConfig
{
    [JsonProperty("version")]
    public string Version { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("plugins")]
    public List<Plugin> Plugins { get; set; } = new();

    [JsonProperty("pythonPackages")]
    public List<PythonPackage> PythonPackages { get; set; } = new();
}

public class PythonPackage
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("set")]
    public string Set { get; set; } = "standard";
}
