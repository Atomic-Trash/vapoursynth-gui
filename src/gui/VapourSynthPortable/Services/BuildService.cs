using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using VapourSynthPortable.Models;

namespace VapourSynthPortable.Services;

public class BuildService : IBuildService
{
    private readonly string _scriptPath;

    public BuildService()
    {
        // Look for script in parent directories
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _scriptPath = FindScript(baseDir, "Build-Portable.ps1") ?? "";
    }

    private string? FindScript(string startDir, string scriptName)
    {
        // Search up to 10 parent directories to find the script
        var dir = new DirectoryInfo(startDir);
        for (int i = 0; i < 10 && dir != null; i++)
        {
            // Check root directory first (legacy location)
            var scriptPath = Path.Combine(dir.FullName, scriptName);
            if (File.Exists(scriptPath))
                return scriptPath;

            // Check scripts/build/ subdirectory (new organized location)
            scriptPath = Path.Combine(dir.FullName, "scripts", "build", scriptName);
            if (File.Exists(scriptPath))
                return scriptPath;

            dir = dir.Parent;
        }
        return null;
    }

    public async Task<BuildResult> RunBuildAsync(
        BuildConfiguration config,
        Action<string> onOutput,
        Action<BuildProgress> onProgress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_scriptPath) || !File.Exists(_scriptPath))
        {
            throw new FileNotFoundException("Build script not found", "Build-Portable.ps1");
        }

        var result = new BuildResult();
        var startTime = DateTime.Now;

        try
        {
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            using var ps = PowerShell.Create();
            ps.Runspace = runspace;

            // Build the command - wrap in script block to capture Write-Host
            var scriptDir = Path.GetDirectoryName(_scriptPath);
            var components = config.Components.Count > 0 ? string.Join(",", config.Components) : "all";
            var cleanParam = config.Clean ? "-Clean" : "";

            var script = $@"
                $InformationPreference = 'Continue'
                Set-Location '{scriptDir}'
                & '{_scriptPath}' -PluginSet '{config.PluginSet}' -Components '{components}' {cleanParam} *>&1
            ";

            ps.AddScript(script);

            // Redirect all streams to capture Write-Host output
            runspace.SessionStateProxy.SetVariable("InformationPreference", "Continue");

            // Set up output handling
            ps.Streams.Information.DataAdded += (s, e) =>
            {
                var info = ps.Streams.Information[e.Index];
                onOutput(info.MessageData?.ToString() + "\n");
            };

            ps.Streams.Warning.DataAdded += (s, e) =>
            {
                var warning = ps.Streams.Warning[e.Index];
                onOutput($"[WARNING] {warning.Message}\n");
            };

            ps.Streams.Error.DataAdded += (s, e) =>
            {
                var error = ps.Streams.Error[e.Index];
                onOutput($"[ERROR] {error.Exception?.Message ?? error.ToString()}\n");
                result.Errors.Add(error.Exception?.Message ?? error.ToString());
            };

            ps.Streams.Progress.DataAdded += (s, e) =>
            {
                var progress = ps.Streams.Progress[e.Index];
                onProgress(new BuildProgress
                {
                    Percent = progress.PercentComplete,
                    Operation = progress.CurrentOperation ?? progress.Activity
                });
            };

            // Run asynchronously
            var outputCollection = new PSDataCollection<PSObject>();
            outputCollection.DataAdded += (s, e) =>
            {
                var output = outputCollection[e.Index];
                onOutput(output?.ToString() + "\n");
            };

            var asyncResult = ps.BeginInvoke<PSObject, PSObject>(null, outputCollection);

            // Wait for completion or cancellation
            while (!asyncResult.IsCompleted)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    ps.Stop();
                    throw new OperationCanceledException();
                }
                await Task.Delay(100, cancellationToken);
            }

            ps.EndInvoke(asyncResult);

            result.Success = !ps.HadErrors;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            onOutput($"[FATAL] {ex.Message}\n");
        }

        result.Duration = DateTime.Now - startTime;
        return result;
    }
}
