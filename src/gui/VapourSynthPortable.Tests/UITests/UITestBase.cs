using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace VapourSynthPortable.Tests.UITests;

/// <summary>
/// Base class for all UI automation tests. Provides app launch/close and navigation utilities.
/// </summary>
[Trait("Category", "UI")]
public abstract class UITestBase : IDisposable
{
    protected Application App { get; private set; } = null!;
    protected UIA3Automation Automation { get; private set; } = null!;
    protected Window MainWindow { get; private set; } = null!;

    private bool _disposed;

    /// <summary>
    /// Launches the VapourSynth Studio application and waits for the main window.
    /// </summary>
    protected void LaunchApp()
    {
        // Kill any existing instances to ensure clean state
        KillExistingInstances();

        var projectDir = FindProjectRoot();
        var exePath = Path.Combine(projectDir,
            "src", "gui", "VapourSynthPortable", "bin", "Debug", "net8.0-windows", "VapourSynthPortable.exe");

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(
                $"Application not found. Run 'dotnet build' first.\nExpected path: {exePath}");
        }

        Automation = new UIA3Automation();
        App = Application.Launch(exePath);
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(15))
            ?? throw new InvalidOperationException("Could not find main window within timeout");
    }

    /// <summary>
    /// Kills any existing VapourSynthPortable instances to ensure clean test state.
    /// </summary>
    private static void KillExistingInstances()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcessesByName("VapourSynthPortable");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                catch { /* Ignore errors killing orphan processes */ }
                finally
                {
                    process.Dispose();
                }
            }

            // Brief delay to ensure process cleanup is complete
            if (processes.Length > 0)
            {
                Thread.Sleep(500);
            }
        }
        catch { /* Ignore any errors during cleanup */ }
    }

    /// <summary>
    /// Finds the project root directory by looking for the solution file.
    /// </summary>
    private static string FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            // Look for solution file or the distinctive folder name
            if (File.Exists(Path.Combine(dir, "VapourSynth GUI.sln")) ||
                Directory.Exists(Path.Combine(dir, "src", "gui")))
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException(
            "Could not find project root. Ensure tests are run from within the project directory.");
    }

    /// <summary>
    /// Navigates to a page by clicking its navigation button.
    /// </summary>
    /// <param name="pageAutomationId">The AutomationId of the navigation button</param>
    protected void NavigateTo(string pageAutomationId)
    {
        var navButton = MainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId(pageAutomationId));

        if (navButton == null)
        {
            throw new InvalidOperationException(
                $"Navigation button with AutomationId '{pageAutomationId}' not found");
        }

        navButton.Click();
        Thread.Sleep(300); // Allow page transition animation
    }

    /// <summary>
    /// Takes a screenshot and saves it for debugging failed tests.
    /// </summary>
    /// <param name="testName">Name to include in the screenshot filename</param>
    protected void TakeScreenshot(string testName)
    {
        try
        {
            var screenshotDir = Path.Combine(FindProjectRoot(), "test-screenshots");
            Directory.CreateDirectory(screenshotDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var filename = Path.Combine(screenshotDir, $"{testName}_{timestamp}.png");

            var capture = FlaUI.Core.Capturing.Capture.Screen();
            capture.ToFile(filename);
        }
        catch
        {
            // Ignore screenshot failures - they shouldn't fail tests
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            try
            {
                if (App != null && !App.HasExited)
                {
                    App.Close();

                    // Wait up to 3 seconds for graceful exit
                    var timeout = DateTime.Now.AddSeconds(3);
                    while (!App.HasExited && DateTime.Now < timeout)
                    {
                        Thread.Sleep(100);
                    }

                    // Force kill if still running
                    if (!App.HasExited)
                    {
                        try { App.Kill(); } catch { }
                    }
                }
            }
            catch
            {
                // Force kill if close fails
                try { App?.Kill(); } catch { }
            }
            finally
            {
                Automation?.Dispose();
            }
        }
    }
}
