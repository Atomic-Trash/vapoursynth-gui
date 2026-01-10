using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Helpers;

/// <summary>
/// Factory for creating commonly used mock objects in tests
/// </summary>
public static class MockFactory
{
    #region Media Pool Service

    public static Mock<IMediaPoolService> CreateMediaPoolService()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        return mock;
    }

    public static Mock<IMediaPoolService> CreateMediaPoolService(MediaItem currentSource)
    {
        var mock = new Mock<IMediaPoolService>();
        var pool = new ObservableCollection<MediaItem> { currentSource };
        mock.Setup(m => m.CurrentSource).Returns(currentSource);
        mock.Setup(m => m.MediaPool).Returns(pool);
        mock.Setup(m => m.HasSource).Returns(true);
        return mock;
    }

    public static Mock<IMediaPoolService> CreateMediaPoolService(IEnumerable<MediaItem> items)
    {
        var mock = new Mock<IMediaPoolService>();
        var pool = new ObservableCollection<MediaItem>(items);
        mock.Setup(m => m.MediaPool).Returns(pool);
        if (pool.Count > 0)
        {
            mock.Setup(m => m.CurrentSource).Returns(pool[0]);
            mock.Setup(m => m.HasSource).Returns(true);
        }
        else
        {
            mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
            mock.Setup(m => m.HasSource).Returns(false);
        }
        return mock;
    }

    #endregion

    #region Settings Service

    public static Mock<ISettingsService> CreateSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        mock.Setup(m => m.ProjectRoot).Returns(AppDomain.CurrentDomain.BaseDirectory);
        mock.Setup(m => m.GetCachePath()).Returns(Path.GetTempPath());
        mock.Setup(m => m.GetOutputPath()).Returns(Path.GetTempPath());
        mock.Setup(m => m.GetCacheSize()).Returns(0);
        return mock;
    }

    public static Mock<ISettingsService> CreateSettingsService(AppSettings settings)
    {
        var mock = CreateSettingsService();
        mock.Setup(m => m.Load()).Returns(settings);
        return mock;
    }

    #endregion

    #region Project Service

    public static Mock<IProjectService> CreateProjectService()
    {
        var mock = new Mock<IProjectService>();
        mock.Setup(m => m.CreateNew()).Returns(new Project());
        mock.Setup(m => m.GetRecentProjects()).Returns(new List<string>());
        return mock;
    }

    public static Mock<IProjectService> CreateProjectService(Project project)
    {
        var mock = CreateProjectService();
        mock.Setup(m => m.CreateNew()).Returns(project);
        mock.Setup(m => m.LoadAsync(It.IsAny<string>())).ReturnsAsync(project);
        return mock;
    }

    #endregion

    #region VapourSynth Service

    public static Mock<IVapourSynthService> CreateVapourSynthService()
    {
        var mock = new Mock<IVapourSynthService>();
        mock.Setup(m => m.IsAvailable).Returns(true);
        return mock;
    }

    public static Mock<IVapourSynthService> CreateVapourSynthService(bool isAvailable)
    {
        var mock = CreateVapourSynthService();
        mock.Setup(m => m.IsAvailable).Returns(isAvailable);
        return mock;
    }

    #endregion

    #region Navigation Service

    public static Mock<INavigationService> CreateNavigationService()
    {
        var mock = new Mock<INavigationService>();
        mock.Setup(m => m.CurrentPage).Returns(PageType.Restore);
        mock.Setup(m => m.CanGoBack).Returns(false);
        mock.Setup(m => m.CanGoForward).Returns(false);
        return mock;
    }

    public static Mock<INavigationService> CreateNavigationService(PageType currentPage)
    {
        var mock = CreateNavigationService();
        mock.Setup(m => m.CurrentPage).Returns(currentPage);
        return mock;
    }

    #endregion

    #region Plugin Service

    public static Mock<IPluginService> CreatePluginService()
    {
        var mock = new Mock<IPluginService>();
        mock.Setup(m => m.LoadPlugins()).Returns(new List<Plugin>());
        mock.Setup(m => m.LoadEnabledPlugins()).Returns(new HashSet<string>());
        return mock;
    }

    public static Mock<IPluginService> CreatePluginService(IEnumerable<Plugin> plugins)
    {
        var mock = CreatePluginService();
        mock.Setup(m => m.LoadPlugins()).Returns(plugins.ToList());
        mock.Setup(m => m.LoadEnabledPlugins()).Returns(plugins.Select(p => p.Name).ToHashSet());
        return mock;
    }

    #endregion

    #region Build Service

    public static Mock<IBuildService> CreateBuildService()
    {
        var mock = new Mock<IBuildService>();
        mock.Setup(m => m.RunBuildAsync(
                It.IsAny<BuildConfiguration>(),
                It.IsAny<Action<string>>(),
                It.IsAny<Action<BuildProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuildResult { Success = true });
        return mock;
    }

    #endregion

    #region Sample Data Helpers

    public static MediaItem CreateSampleMediaItem(string? path = null)
    {
        return new MediaItem
        {
            FilePath = path ?? Path.Combine(Path.GetTempPath(), "sample.mp4"),
            Name = "Sample Video",
            Duration = 300, // 5 minutes in seconds
            Width = 1920,
            Height = 1080,
            MediaType = MediaType.Video,
            HasAudioStream = true
        };
    }

    public static Project CreateSampleProject(string? name = null)
    {
        return new Project
        {
            Name = name ?? "Test Project"
        };
    }

    public static Plugin CreateSamplePlugin(string? name = null)
    {
        return new Plugin
        {
            Name = name ?? "test-plugin",
            Description = "A test plugin",
            Set = "standard",
            Version = "1.0.0"
        };
    }

    public static AppSettings CreateSampleSettings()
    {
        return new AppSettings
        {
            OutputDirectory = "output",
            CacheDirectory = "cache",
            ShowLogPanel = true,
            AutoSaveEnabled = true
        };
    }

    #endregion
}
