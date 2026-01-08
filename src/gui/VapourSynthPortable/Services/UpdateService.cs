using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class UpdateResult
{
    public string PluginName { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string LatestVersion { get; set; } = "";
    public bool HasUpdate { get; set; }
    public string? NewUrl { get; set; }
}

public class UpdateService
{
    private static readonly HttpClient _httpClient = new();
    private const string GitHubApiBase = "https://api.github.com";

    static UpdateService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "VapourSynth-Portable-GUI");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    public async Task<List<UpdateResult>> CheckPluginUpdatesAsync(IEnumerable<Plugin> plugins, Action<int, int>? onProgress = null)
    {
        var results = new List<UpdateResult>();
        var pluginList = plugins.ToList();
        var total = pluginList.Count;
        var current = 0;

        foreach (var plugin in pluginList)
        {
            current++;
            onProgress?.Invoke(current, total);

            var result = await CheckPluginAsync(plugin);
            if (result != null)
                results.Add(result);

            // Rate limiting - be nice to GitHub API
            await Task.Delay(100);
        }

        return results;
    }

    private async Task<UpdateResult?> CheckPluginAsync(Plugin plugin)
    {
        try
        {
            var repoInfo = ExtractGitHubRepo(plugin.Url);
            if (repoInfo == null)
                return null;

            var (owner, repo) = repoInfo.Value;
            var latestRelease = await GetLatestReleaseAsync(owner, repo);
            if (latestRelease == null)
                return null;

            var status = CompareVersions(plugin.Version, latestRelease.TagName);

            return new UpdateResult
            {
                PluginName = plugin.Name,
                CurrentVersion = plugin.Version,
                LatestVersion = latestRelease.TagName,
                HasUpdate = status == "outdated",
                NewUrl = status == "outdated" ? FindCompatibleAssetUrl(latestRelease.Assets) : null
            };
        }
        catch
        {
            return null;
        }
    }

    private (string Owner, string Repo)? ExtractGitHubRepo(string url)
    {
        var match = Regex.Match(url, @"github\.com/([^/]+)/([^/]+)");
        if (match.Success)
            return (match.Groups[1].Value, match.Groups[2].Value);
        return null;
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync(string owner, string repo)
    {
        try
        {
            var url = $"{GitHubApiBase}/repos/{owner}/{repo}/releases/latest";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            var assets = json["assets"]?.Select(a => new GitHubAsset
            {
                Name = a["name"]?.ToString() ?? "",
                Url = a["browser_download_url"]?.ToString() ?? ""
            }).ToList() ?? new List<GitHubAsset>();

            return new GitHubRelease
            {
                TagName = json["tag_name"]?.ToString() ?? "",
                Name = json["name"]?.ToString() ?? "",
                Assets = assets
            };
        }
        catch
        {
            return null;
        }
    }

    private string CompareVersions(string current, string latest)
    {
        // Normalize versions (remove common prefixes like 'v', 'r', 'R')
        var currentNorm = Regex.Replace(current, @"^[vVrR]", "");
        var latestNorm = Regex.Replace(latest, @"^[vVrR]", "");

        if (currentNorm == latestNorm)
            return "current";

        // Try numeric comparison
        try
        {
            var currentNum = double.Parse(Regex.Replace(currentNorm, @"[^0-9.]", ""));
            var latestNum = double.Parse(Regex.Replace(latestNorm, @"[^0-9.]", ""));

            if (latestNum > currentNum)
                return "outdated";
            if (latestNum < currentNum)
                return "newer";
            return "current";
        }
        catch
        {
            // Fall back to string comparison
            return currentNorm != latestNorm ? "different" : "current";
        }
    }

    private string? FindCompatibleAssetUrl(List<GitHubAsset> assets)
    {
        // Look for x64/win64 assets
        var compatible = assets.FirstOrDefault(a =>
            Regex.IsMatch(a.Name, @"x64|win64|64bit|amd64", RegexOptions.IgnoreCase) &&
            Regex.IsMatch(a.Name, @"\.zip$|\.7z$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(a.Name, @"x86|win32|32bit", RegexOptions.IgnoreCase));

        if (compatible != null)
            return compatible.Url;

        // Fallback: any zip/7z that's not clearly 32-bit
        var fallback = assets.FirstOrDefault(a =>
            Regex.IsMatch(a.Name, @"\.zip$|\.7z$", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(a.Name, @"x86|win32|32bit", RegexOptions.IgnoreCase));

        return fallback?.Url;
    }

    private class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public string Name { get; set; } = "";
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
