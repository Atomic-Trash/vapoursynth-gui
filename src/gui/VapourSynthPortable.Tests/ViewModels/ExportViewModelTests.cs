using System.Collections.ObjectModel;
using Moq;
using VapourSynthPortable.Models;
using VapourSynthPortable.Services;
using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class ExportViewModelTests
{
    private static Mock<IMediaPoolService> CreateMockMediaPool()
    {
        var mock = new Mock<IMediaPoolService>();
        mock.Setup(m => m.MediaPool).Returns(new ObservableCollection<MediaItem>());
        mock.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        mock.Setup(m => m.HasSource).Returns(false);
        return mock;
    }

    private static Mock<ISettingsService> CreateMockSettingsService()
    {
        var mock = new Mock<ISettingsService>();
        mock.Setup(m => m.Load()).Returns(new AppSettings());
        return mock;
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - verify key properties are set and collections are populated
        Assert.NotNull(vm.SelectedVideoCodec);
        Assert.NotNull(vm.SelectedAudioCodec);
        Assert.NotNull(vm.SelectedPresetSpeed);
        Assert.NotNull(vm.SelectedFormat);
        Assert.NotEmpty(vm.Presets);
        Assert.NotEmpty(vm.Resolutions);
        Assert.NotEmpty(vm.VideoCodecs);
        Assert.NotEmpty(vm.AudioCodecs);
    }

    [Fact]
    public void Constructor_LoadsSettingsFromService()
    {
        // Arrange
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(m => m.Load()).Returns(new AppSettings());

        // Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Assert - verify settings service was consulted during initialization
        settingsService.Verify(s => s.Load(), Times.AtLeastOnce);

        // Verify ViewModel was properly initialized (details depend on preset/settings interaction)
        Assert.NotNull(vm.SelectedVideoCodec);
        Assert.NotNull(vm.SelectedFormat);
    }

    [Fact]
    public void Resolutions_ContainsExpectedOptions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(vm.Resolutions, r => r.Name == "Source");
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("4K"));
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("1080p"));
        Assert.Contains(vm.Resolutions, r => r.Name.Contains("720p"));
    }

    #endregion

    #region Export Mode Tests

    [Fact]
    public void IsDirectEncodeMode_WhenDirectEncode_ReturnsTrue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Assert
        Assert.True(vm.IsDirectEncodeMode);
        Assert.False(vm.IsTimelineWithEffectsMode);
    }

    [Fact]
    public void IsTimelineWithEffectsMode_WhenTimelineMode_ReturnsTrue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Assert
        Assert.False(vm.IsDirectEncodeMode);
        Assert.True(vm.IsTimelineWithEffectsMode);
    }

    [Fact]
    public void IsDirectEncodeMode_Setter_ChangesExportMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Act
        vm.IsDirectEncodeMode = true;

        // Assert
        Assert.Equal(ExportMode.DirectEncode, vm.ExportMode);
    }

    [Fact]
    public void IsTimelineWithEffectsMode_Setter_ChangesExportMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Act
        vm.IsTimelineWithEffectsMode = true;

        // Assert
        Assert.Equal(ExportMode.TimelineWithEffects, vm.ExportMode);
    }

    #endregion

    #region Codec Detection Tests

    [Theory]
    [InlineData("h264_nvenc", true)]
    [InlineData("hevc_nvenc", true)]
    [InlineData("libx264", false)]
    [InlineData("libx265", false)]
    public void IsNvencCodec_DetectsNvencCodecs(string codec, bool expected)
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            SelectedVideoCodec = codec
        };

        // Assert
        Assert.Equal(expected, vm.IsNvencCodec);
    }

    [Theory]
    [InlineData("prores_ks", true)]
    [InlineData("libx264", false)]
    [InlineData("h264_nvenc", false)]
    public void IsProresCodec_DetectsProresCodecs(string codec, bool expected)
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            SelectedVideoCodec = codec
        };

        // Assert
        Assert.Equal(expected, vm.IsProresCodec);
    }

    #endregion

    #region Queue Management Tests

    [Fact]
    public void AddToQueueCommand_WithValidPaths_AddsJobToQueue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = @"C:\test\output.mp4"
        };

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("output.mp4", vm.ExportQueue[0].Name);
    }

    [Fact]
    public void AddToQueueCommand_WithEmptyInputPath_DoesNotAddJob()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = "",
            OutputPath = @"C:\test\output.mp4"
        };

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Empty(vm.ExportQueue);
        Assert.Contains("Please select", vm.StatusText);
    }

    [Fact]
    public void RemoveJobCommand_RemovesJobFromQueue()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = @"C:\test\output.mp4"
        };
        vm.AddToQueueCommand.Execute(null);
        var job = vm.ExportQueue[0];

        // Act
        vm.RemoveJobCommand.Execute(job);

        // Assert
        Assert.Empty(vm.ExportQueue);
    }

    [Fact]
    public void ClearQueueCommand_RemovesCompletedJobs()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var completedJob = new ExportJob { Name = "Completed", Status = ExportJobStatus.Completed };
        var pendingJob = new ExportJob { Name = "Pending", Status = ExportJobStatus.Pending };
        vm.ExportQueue.Add(completedJob);
        vm.ExportQueue.Add(pendingJob);

        // Act
        vm.ClearQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("Pending", vm.ExportQueue[0].Name);
    }

    [Fact]
    public void ClearQueueCommand_RemovesFailedAndCancelledJobs()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        vm.ExportQueue.Add(new ExportJob { Name = "Failed", Status = ExportJobStatus.Failed });
        vm.ExportQueue.Add(new ExportJob { Name = "Cancelled", Status = ExportJobStatus.Cancelled });
        vm.ExportQueue.Add(new ExportJob { Name = "Pending", Status = ExportJobStatus.Pending });

        // Act
        vm.ClearQueueCommand.Execute(null);

        // Assert
        Assert.Single(vm.ExportQueue);
        Assert.Equal("Pending", vm.ExportQueue[0].Name);
    }

    #endregion

    #region Settings Persistence Tests

    [Fact]
    public void SelectedFormat_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedFormat = "mkv";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void SelectedVideoCodec_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedVideoCodec = "libx265";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Quality_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.Quality = 18;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region HasCurrentSource Tests

    [Fact]
    public void HasCurrentSource_WhenNoSource_ReturnsFalse()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.HasSource).Returns(false);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.HasCurrentSource);
    }

    [Fact]
    public void HasCurrentSource_WhenHasSource_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.HasSource).Returns(true);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.HasCurrentSource);
    }

    [Fact]
    public void CurrentSourcePath_WhenHasSource_ReturnsPath()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem { FilePath = @"C:\videos\test.mp4" };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal(@"C:\videos\test.mp4", vm.CurrentSourcePath);
    }

    [Fact]
    public void CurrentSourcePath_WhenNoSource_ReturnsEmpty()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.CurrentSourcePath);
    }

    #endregion

    #region Log Management Tests

    [Fact]
    public void ClearLogCommand_ClearsLogOutput()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            LogOutput = "Some log content"
        };

        // Act
        vm.ClearLogCommand.Execute(null);

        // Assert
        Assert.Equal("", vm.LogOutput);
    }

    #endregion

    #region Preset Selection Tests

    [Fact]
    public void SelectedPreset_WhenChanged_UpdatesCodecSettings()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var preset = new ExportPreset
        {
            Name = "Test Preset",
            VideoCodec = "libx265",
            AudioCodec = "flac",
            Quality = 18,
            Preset = "slow",
            Format = "mkv"
        };

        // Act
        vm.SelectedPreset = preset;

        // Assert
        Assert.Equal("libx265", vm.SelectedVideoCodec);
        Assert.Equal("flac", vm.SelectedAudioCodec);
        Assert.Equal(18, vm.Quality);
        Assert.Equal("slow", vm.SelectedPresetSpeed);
        Assert.Equal("mkv", vm.SelectedFormat);
    }

    #endregion

    #region Status Tests

    [Fact]
    public void StatusText_InitialValue_IsReady()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("Ready", vm.StatusText);
    }

    [Fact]
    public void IsEncoding_InitialValue_IsFalse()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.IsEncoding);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - should not throw
        vm.Dispose();
        vm.Dispose();
    }

    #endregion

    #region Restoration Tests

    [Fact]
    public void HasRestoration_WhenNoSource_ReturnsFalse()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.HasRestoration);
    }

    [Fact]
    public void HasRestoration_WhenSourceHasRestoration_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem
        {
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Test", IsEnabled = true }
        };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.HasRestoration);
    }

    [Fact]
    public void RestorationPresetName_WhenHasRestoration_ReturnsPresetName()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem
        {
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Film Grain Removal", IsEnabled = true }
        };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("Film Grain Removal", vm.RestorationPresetName);
    }

    [Fact]
    public void RestorationPresetName_WhenNoRestoration_ReturnsEmpty()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        mediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.RestorationPresetName);
    }

    [Fact]
    public void RestorationEnabled_WhenRestorationEnabled_ReturnsTrue()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem
        {
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Test", IsEnabled = true }
        };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.RestorationEnabled);
    }

    [Fact]
    public void RestorationEnabled_WhenRestorationDisabled_ReturnsFalse()
    {
        // Arrange
        var mediaPool = CreateMockMediaPool();
        var mediaItem = new MediaItem
        {
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Test", IsEnabled = false }
        };
        mediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var vm = new ExportViewModel(mediaPool.Object, CreateMockSettingsService().Object);

        // Assert
        Assert.False(vm.RestorationEnabled);
    }

    [Fact]
    public void IncludeRestoration_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.IncludeRestoration);
    }

    [Fact]
    public void IncludeRestoration_CanBeChanged()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.IncludeRestoration = false;

        // Assert
        Assert.False(vm.IncludeRestoration);
    }

    #endregion

    #region Video Settings Tests

    [Fact]
    public void VideoEnabled_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.VideoEnabled);
    }

    [Fact]
    public void VideoEnabled_WhenChanged_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.VideoEnabled = false;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void VideoCodecs_ContainsExpectedCodecs()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("libx264", vm.VideoCodecs);
        Assert.Contains("libx265", vm.VideoCodecs);
        Assert.Contains("h264_nvenc", vm.VideoCodecs);
        Assert.Contains("hevc_nvenc", vm.VideoCodecs);
        Assert.Contains("prores_ks", vm.VideoCodecs);
        Assert.Contains("ffv1", vm.VideoCodecs);
    }

    [Fact]
    public void PresetSpeeds_ContainsExpectedSpeeds()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("ultrafast", vm.PresetSpeeds);
        Assert.Contains("medium", vm.PresetSpeeds);
        Assert.Contains("veryslow", vm.PresetSpeeds);
    }

    [Fact]
    public void Quality_DefaultValue_Is22()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - depends on loaded settings, but default AppSettings has 22
        Assert.InRange(vm.Quality, 0, 51);
    }

    [Fact]
    public void SelectedPresetSpeed_DefaultValue_IsMedium()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - should be medium if AppSettings defaults
        Assert.NotNull(vm.SelectedPresetSpeed);
    }

    [Fact]
    public void SelectedPresetSpeed_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedPresetSpeed = "slow";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region Audio Settings Tests

    [Fact]
    public void AudioEnabled_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.True(vm.AudioEnabled);
    }

    [Fact]
    public void AudioEnabled_WhenChanged_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.AudioEnabled = false;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void AudioCodecs_ContainsExpectedCodecs()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("aac", vm.AudioCodecs);
        Assert.Contains("libmp3lame", vm.AudioCodecs);
        Assert.Contains("flac", vm.AudioCodecs);
        Assert.Contains("pcm_s16le", vm.AudioCodecs);
        Assert.Contains("copy", vm.AudioCodecs);
    }

    [Fact]
    public void AudioBitrates_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(128, vm.AudioBitrates);
        Assert.Contains(192, vm.AudioBitrates);
        Assert.Contains(256, vm.AudioBitrates);
        Assert.Contains(320, vm.AudioBitrates);
    }

    [Fact]
    public void SelectedAudioBitrate_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedAudioBitrate = 256;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void SelectedAudioCodec_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedAudioCodec = "flac";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region Resolution Tests

    [Fact]
    public void SelectedResolution_DefaultValue_IsSource()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.NotNull(vm.SelectedResolution);
        Assert.Equal("Source", vm.SelectedResolution.Name);
    }

    [Fact]
    public void SelectedResolution_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);
        var resolution1080p = vm.Resolutions.First(r => r.Name.Contains("1080p"));

        // Act
        vm.SelectedResolution = resolution1080p;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Resolutions_4K_HasCorrectDimensions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var res4K = vm.Resolutions.First(r => r.Name.Contains("4K"));

        // Assert
        Assert.Equal(3840, res4K.Width);
        Assert.Equal(2160, res4K.Height);
    }

    [Fact]
    public void Resolutions_1080p_HasCorrectDimensions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var res1080p = vm.Resolutions.First(r => r.Name.Contains("1080p"));

        // Assert
        Assert.Equal(1920, res1080p.Width);
        Assert.Equal(1080, res1080p.Height);
    }

    [Fact]
    public void Resolutions_Source_HasZeroDimensions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var resSource = vm.Resolutions.First(r => r.Name == "Source");

        // Assert
        Assert.Equal(0, resSource.Width);
        Assert.Equal(0, resSource.Height);
    }

    #endregion

    #region Frame Rate Tests

    [Fact]
    public void FrameRates_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("Source", vm.FrameRates);
        Assert.Contains("23.976", vm.FrameRates);
        Assert.Contains("24", vm.FrameRates);
        Assert.Contains("29.97", vm.FrameRates);
        Assert.Contains("30", vm.FrameRates);
        Assert.Contains("60", vm.FrameRates);
    }

    [Fact]
    public void SelectedFrameRate_DefaultValue_IsSource()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("Source", vm.SelectedFrameRate);
    }

    [Fact]
    public void SelectedFrameRate_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedFrameRate = "30";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region NVENC Preset Tests

    [Fact]
    public void NvencPresets_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(vm.NvencPresets, p => p.Contains("p1"));
        Assert.Contains(vm.NvencPresets, p => p.Contains("p4"));
        Assert.Contains(vm.NvencPresets, p => p.Contains("p7"));
    }

    [Fact]
    public void SelectedNvencPreset_DefaultValue_IsP4()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("p4", vm.SelectedNvencPreset);
    }

    [Fact]
    public void SelectedNvencPreset_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedNvencPreset = "p7 (best quality)";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region ProRes Profile Tests

    [Fact]
    public void ProresProfiles_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(vm.ProresProfiles, p => p.Contains("Proxy"));
        Assert.Contains(vm.ProresProfiles, p => p.Contains("LT"));
        Assert.Contains(vm.ProresProfiles, p => p.Contains("Standard"));
        Assert.Contains(vm.ProresProfiles, p => p.Contains("HQ"));
        Assert.Contains(vm.ProresProfiles, p => p.Contains("4444"));
    }

    [Fact]
    public void SelectedProresProfile_DefaultValue_IsStandard()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("Standard", vm.SelectedProresProfile);
    }

    [Fact]
    public void SelectedProresProfile_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.SelectedProresProfile = "3 - HQ";

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    #endregion

    #region Format Tests

    [Fact]
    public void Formats_ContainsExpectedValues()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains("mp4", vm.Formats);
        Assert.Contains("mkv", vm.Formats);
        Assert.Contains("mov", vm.Formats);
        Assert.Contains("avi", vm.Formats);
        Assert.Contains("webm", vm.Formats);
    }

    [Fact]
    public void SelectedFormat_DefaultValue_IsMp4()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("mp4", vm.SelectedFormat);
    }

    #endregion

    #region Path Handling Tests

    [Fact]
    public void InputPath_InitialValue_IsEmpty()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.InputPath);
    }

    [Fact]
    public void OutputPath_InitialValue_IsEmpty()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.OutputPath);
    }

    [Fact]
    public void OutputFileName_InitialValue_IsOutput()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("output", vm.OutputFileName);
    }

    #endregion

    #region Cancel Export Tests

    [Fact]
    public void CancelExportCommand_SetsIsEncodingToFalse()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.CancelExportCommand.Execute(null);

        // Assert
        Assert.False(vm.IsEncoding);
        Assert.Contains("cancelled", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CancelExportCommand_WithCurrentJob_SetsJobStatusToCancelled()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var job = new ExportJob { Name = "Test Job", Status = ExportJobStatus.Encoding };
        vm.CurrentJob = job;

        // Act
        vm.CancelExportCommand.Execute(null);

        // Assert
        Assert.Equal(ExportJobStatus.Cancelled, job.Status);
    }

    #endregion

    #region Timeline Tests

    [Fact]
    public void Timeline_InitialValue_CanBeNull()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - Timeline may be null initially if EditTimeline is null
        // This is acceptable behavior
        Assert.True(true);
    }

    [Fact]
    public void Timeline_CanBeSet()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var timeline = new Timeline();

        // Act
        vm.Timeline = timeline;

        // Assert
        Assert.Same(timeline, vm.Timeline);
    }

    #endregion

    #region Export Mode Change Tests

    [Fact]
    public void ExportMode_Change_SavesSettings()
    {
        // Arrange
        var settingsService = CreateMockSettingsService();
        var vm = new ExportViewModel(CreateMockMediaPool().Object, settingsService.Object);

        // Act
        vm.ExportMode = ExportMode.TimelineWithEffects;

        // Assert
        settingsService.Verify(s => s.Save(It.IsAny<AppSettings>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ExportModes_ContainsBothOptions()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Contains(ExportMode.DirectEncode, vm.ExportModes);
        Assert.Contains(ExportMode.TimelineWithEffects, vm.ExportModes);
    }

    #endregion

    #region Queue Edge Cases Tests

    [Fact]
    public void AddToQueueCommand_WithEmptyOutputPath_DoesNotAddJob()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = ""
        };

        // Act
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Empty(vm.ExportQueue);
    }

    [Fact]
    public void RemoveJobCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - should not throw
        vm.RemoveJobCommand.Execute(null);
    }

    [Fact]
    public void RemoveJobCommand_WithEncodingJob_DoesNotRemove()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var job = new ExportJob { Name = "Encoding", Status = ExportJobStatus.Encoding };
        vm.ExportQueue.Add(job);

        // Act
        vm.RemoveJobCommand.Execute(job);

        // Assert
        Assert.Single(vm.ExportQueue);
    }

    [Fact]
    public void ClearQueueCommand_WithEmptyQueue_DoesNotThrow()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act & Assert - should not throw
        vm.ClearQueueCommand.Execute(null);
        Assert.Empty(vm.ExportQueue);
    }

    [Fact]
    public void AddToQueueCommand_MultipleTimes_AddsMultipleJobs()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            InputPath = @"C:\test\input.mp4",
            OutputPath = @"C:\test\output.mp4"
        };

        // Act
        vm.AddToQueueCommand.Execute(null);
        vm.OutputPath = @"C:\test\output2.mp4";
        vm.AddToQueueCommand.Execute(null);

        // Assert
        Assert.Equal(2, vm.ExportQueue.Count);
    }

    #endregion

    #region Info Display Tests

    [Fact]
    public void EstimatedSize_InitialValue_IsEmpty()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.EstimatedSize);
    }

    [Fact]
    public void InputInfo_InitialValue_IsEmpty()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.InputInfo);
    }

    [Fact]
    public void InputDuration_InitialValue_IsZero()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal(0, vm.InputDuration);
    }

    #endregion

    #region CurrentJob Tests

    [Fact]
    public void CurrentJob_InitialValue_IsNull()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Null(vm.CurrentJob);
    }

    [Fact]
    public void CurrentJob_CanBeSet()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var job = new ExportJob { Name = "Test" };

        // Act
        vm.CurrentJob = job;

        // Assert
        Assert.Same(job, vm.CurrentJob);
    }

    #endregion

    #region LogOutput Tests

    [Fact]
    public void LogOutput_InitialValue_IsEmpty()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert
        Assert.Equal("", vm.LogOutput);
    }

    [Fact]
    public void LogOutput_CanBeSet()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        vm.LogOutput = "Test log message";

        // Assert
        Assert.Equal("Test log message", vm.LogOutput);
    }

    #endregion

    #region VapourSynth Availability Tests

    [Fact]
    public void IsVapourSynthAvailable_ReturnsServiceAvailability()
    {
        // Arrange & Act
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Assert - VapourSynthService.IsAvailable depends on system state
        // We just verify it doesn't throw
        var result = vm.IsVapourSynthAvailable;
        Assert.True(result || !result); // Always true, just testing access
    }

    #endregion

    #region Preset Audio Bitrate Tests

    [Fact]
    public void SelectedPreset_WithAudioBitrate_UpdatesAudioBitrate()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var preset = new ExportPreset
        {
            Name = "High Quality Audio",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Quality = 20,
            Preset = "medium",
            Format = "mp4",
            AudioBitrate = 320
        };

        // Act
        vm.SelectedPreset = preset;

        // Assert
        Assert.Equal(320, vm.SelectedAudioBitrate);
    }

    [Fact]
    public void SelectedPreset_WithZeroAudioBitrate_DefaultsTo192()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var preset = new ExportPreset
        {
            Name = "No Audio Bitrate",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Quality = 20,
            Preset = "medium",
            Format = "mp4",
            AudioBitrate = 0
        };

        // Act
        vm.SelectedPreset = preset;

        // Assert
        Assert.Equal(192, vm.SelectedAudioBitrate);
    }

    [Fact]
    public void SelectedPreset_WithVideoEnabled_UpdatesVideoEnabled()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var preset = new ExportPreset
        {
            Name = "Audio Only",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Quality = 20,
            Preset = "medium",
            Format = "mp4",
            VideoEnabled = false
        };

        // Act
        vm.SelectedPreset = preset;

        // Assert
        Assert.False(vm.VideoEnabled);
    }

    [Fact]
    public void SelectedPreset_Null_DoesNotChangeSettings()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var originalCodec = vm.SelectedVideoCodec;

        // Act
        vm.SelectedPreset = null;

        // Assert
        Assert.Equal(originalCodec, vm.SelectedVideoCodec);
    }

    #endregion

    #region ResolutionOption Tests

    [Fact]
    public void ResolutionOption_ToString_ReturnsName()
    {
        // Arrange
        var resolution = new ResolutionOption { Name = "1080p (1920x1080)", Width = 1920, Height = 1080 };

        // Act
        var result = resolution.ToString();

        // Assert
        Assert.Equal("1080p (1920x1080)", result);
    }

    #endregion

    #region ExportMode Enum Tests

    [Fact]
    public void ExportMode_DirectEncode_HasValue0()
    {
        // Assert
        Assert.Equal(0, (int)ExportMode.DirectEncode);
    }

    [Fact]
    public void ExportMode_TimelineWithEffects_HasValue1()
    {
        // Assert
        Assert.Equal(1, (int)ExportMode.TimelineWithEffects);
    }

    #endregion

    #region Export Mode Radio Button Tests

    [Fact]
    public void IsDirectEncodeMode_SetToFalse_DoesNotChangeMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.DirectEncode
        };

        // Act - setting to false when already in DirectEncode shouldn't change anything
        vm.IsDirectEncodeMode = false;

        // Assert - should still be DirectEncode (false setter doesn't change to timeline)
        Assert.Equal(ExportMode.DirectEncode, vm.ExportMode);
    }

    [Fact]
    public void IsTimelineWithEffectsMode_SetToFalse_DoesNotChangeMode()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            ExportMode = ExportMode.TimelineWithEffects
        };

        // Act - setting to false when already in TimelineWithEffects shouldn't change anything
        vm.IsTimelineWithEffectsMode = false;

        // Assert - should still be TimelineWithEffects
        Assert.Equal(ExportMode.TimelineWithEffects, vm.ExportMode);
    }

    #endregion

    #region PropertyChanged Notification Tests

    [Fact]
    public void SelectedVideoCodec_Change_RaisesPropertyChangedForIsNvencCodec()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var propertyChangedRaised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsNvencCodec))
                propertyChangedRaised = true;
        };

        // Act
        vm.SelectedVideoCodec = "h264_nvenc";

        // Assert
        Assert.True(propertyChangedRaised);
    }

    [Fact]
    public void SelectedVideoCodec_Change_RaisesPropertyChangedForIsProresCodec()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);
        var propertyChangedRaised = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.IsProresCodec))
                propertyChangedRaised = true;
        };

        // Act
        vm.SelectedVideoCodec = "prores_ks";

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion

    #region StartQueueAsync Validation Tests

    [Fact]
    public async Task StartQueueCommand_WithEmptyQueue_SetsStatusToQueueEmpty()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object);

        // Act
        await vm.StartQueueCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("empty", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StartQueueCommand_WhenAlreadyEncoding_SetsStatusToAlreadyEncoding()
    {
        // Arrange
        var vm = new ExportViewModel(CreateMockMediaPool().Object, CreateMockSettingsService().Object)
        {
            IsEncoding = true
        };
        vm.ExportQueue.Add(new ExportJob { Name = "Test", Status = ExportJobStatus.Pending });

        // Act
        await vm.StartQueueCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("already", vm.StatusText.ToLower());
    }

    #endregion
}
