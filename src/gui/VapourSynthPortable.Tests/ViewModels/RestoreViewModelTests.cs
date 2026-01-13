using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class RestoreViewModelTests
{
    private static RestoreViewModel CreateViewModel(Mock<IMediaPoolService>? mockMediaPool = null)
    {
        mockMediaPool ??= new Mock<IMediaPoolService>();
        var mockVsService = new Mock<IVapourSynthService>();
        var mockSettingsService = new Mock<ISettingsService>();
        var mockNavigationService = new Mock<INavigationService>();
        mockSettingsService.Setup(s => s.Load()).Returns(new AppSettings());
        return new RestoreViewModel(mockMediaPool.Object, mockVsService.Object, mockSettingsService.Object, mockNavigationService.Object);
    }

    #region Construction Tests

    [Fact]
    public void Constructor_WithServices_DoesNotThrow()
    {
        // Act & Assert
        var action = () => CreateViewModel();
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesPresetsCollection()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();

        // Act
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.Presets.Should().NotBeNull();
        viewModel.Presets.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_InitializesCategories()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();

        // Act
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.Categories.Should().NotBeNull();
        viewModel.Categories.Should().Contain("All");
    }

    [Fact]
    public void Constructor_SetsDefaultSelectedCategory()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();

        // Act
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.SelectedCategory.Should().Be("All");
    }

    #endregion

    #region Search Functionality Tests

    [Fact]
    public void SearchQuery_FiltersPresets_ByName()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var initialCount = viewModel.FilteredPresets.Count;

        // Act
        viewModel.SearchQuery = "Upscale";

        // Assert
        viewModel.FilteredPresets.Should().NotBeEmpty();
        viewModel.FilteredPresets.All(p => p.Name.Contains("Upscale", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public void SearchQuery_FiltersPresets_ByDescription()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Act
        viewModel.SearchQuery = "Real-ESRGAN";

        // Assert
        viewModel.FilteredPresets.Should().NotBeEmpty();
        viewModel.FilteredPresets.Any(p =>
            p.Name.Contains("Real-ESRGAN", StringComparison.OrdinalIgnoreCase) ||
            p.Description.Contains("Real-ESRGAN", StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public void SearchQuery_EmptyString_ShowsAllPresets()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SearchQuery = "xyz123"; // Filter first
        var filteredCount = viewModel.FilteredPresets.Count;

        // Act
        viewModel.SearchQuery = "";

        // Assert
        viewModel.FilteredPresets.Count.Should().BeGreaterThan(filteredCount);
    }

    [Fact]
    public void SearchQuery_NoMatch_ReturnsEmptyList()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Act
        viewModel.SearchQuery = "xyz_no_match_999";

        // Assert
        viewModel.FilteredPresets.Should().BeEmpty();
    }

    #endregion

    #region Category Filter Tests

    [Fact]
    public void SelectedCategory_ChangeTo_FiltersPresets()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Act
        viewModel.SelectedCategory = "Denoise";

        // Assert
        viewModel.FilteredPresets.Should().NotBeEmpty();
        viewModel.FilteredPresets.All(p => p.Category == "Denoise").Should().BeTrue();
    }

    [Fact]
    public void SelectedCategory_All_ShowsAllPresets()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedCategory = "Denoise"; // Filter first

        // Act
        viewModel.SelectedCategory = "All";

        // Assert
        viewModel.FilteredPresets.Count.Should().Be(viewModel.Presets.Count);
    }

    #endregion

    #region Favorites System Tests

    [Fact]
    public void ToggleFavorite_AddsFavorite_WhenNotFavorited()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var preset = viewModel.Presets.First();
        preset.IsFavorite = false;

        // Act
        viewModel.ToggleFavoriteCommand.Execute(preset);

        // Assert
        preset.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public void ToggleFavorite_RemovesFavorite_WhenFavorited()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var preset = viewModel.Presets.First();
        preset.IsFavorite = true;

        // Act
        viewModel.ToggleFavoriteCommand.Execute(preset);

        // Assert
        preset.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public void FilterPresets_ShowsOnlyFavorites_WhenFavoritesCategorySelected()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Mark some presets as favorites
        viewModel.Presets[0].IsFavorite = true;
        viewModel.Presets[1].IsFavorite = true;

        // Act
        viewModel.SelectedCategory = "Favorites";

        // Assert
        viewModel.FilteredPresets.Should().HaveCount(2);
        viewModel.FilteredPresets.All(p => p.IsFavorite).Should().BeTrue();
    }

    [Fact]
    public void ToggleFavorite_WithNullPreset_DoesNotThrow()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Act & Assert
        var action = () => viewModel.ToggleFavoriteCommand.Execute(null);
        action.Should().NotThrow();
    }

    #endregion

    #region Queue Operation Tests

    [Fact]
    public void MoveJobUp_SwapsPositions_WhenNotFirst()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var job1 = new RestoreJob { Status = ProcessingStatus.Pending };
        var job2 = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job1);
        viewModel.JobQueue.Add(job2);

        // Act
        viewModel.MoveJobUpCommand.Execute(job2);

        // Assert
        viewModel.JobQueue[0].Should().Be(job2);
        viewModel.JobQueue[1].Should().Be(job1);
    }

    [Fact]
    public void MoveJobUp_DoesNotMove_WhenFirst()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var job = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job);

        // Act
        viewModel.MoveJobUpCommand.Execute(job);

        // Assert
        viewModel.JobQueue[0].Should().Be(job);
    }

    [Fact]
    public void MoveJobDown_SwapsPositions_WhenNotLast()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var job1 = new RestoreJob { Status = ProcessingStatus.Pending };
        var job2 = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job1);
        viewModel.JobQueue.Add(job2);

        // Act
        viewModel.MoveJobDownCommand.Execute(job1);

        // Assert
        viewModel.JobQueue[0].Should().Be(job2);
        viewModel.JobQueue[1].Should().Be(job1);
    }

    [Fact]
    public void MoveJobDown_DoesNotMove_WhenLast()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var job = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job);

        // Act
        viewModel.MoveJobDownCommand.Execute(job);

        // Assert
        viewModel.JobQueue[0].Should().Be(job);
    }

    [Fact]
    public void MoveJob_DoesNotMove_WhenProcessing()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var job1 = new RestoreJob { Status = ProcessingStatus.Processing };
        var job2 = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job1);
        viewModel.JobQueue.Add(job2);

        // Act
        viewModel.MoveJobDownCommand.Execute(job1);

        // Assert
        viewModel.JobQueue[0].Should().Be(job1);
        viewModel.JobQueue[1].Should().Be(job2);
    }

    #endregion

    #region Pause State Tests

    [Fact]
    public void TogglePause_SetsPausedState()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsPaused = false;

        // Act
        viewModel.TogglePauseCommand.Execute(null);

        // Assert
        viewModel.IsPaused.Should().BeTrue();
    }

    [Fact]
    public void TogglePause_ResumesWhenPaused()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsPaused = true;

        // Act
        viewModel.TogglePauseCommand.Execute(null);

        // Assert
        viewModel.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void TogglePause_UpdatesStatusText()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsPaused = false;

        // Act
        viewModel.TogglePauseCommand.Execute(null);

        // Assert
        viewModel.StatusText.Should().Contain("paused");
    }

    #endregion

    #region Queue Summary Tests

    [Fact]
    public void QueueSummary_CalculatesCorrectCounts()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Failed });

        // Assert
        viewModel.CompletedJobCount.Should().Be(2);
        viewModel.PendingJobCount.Should().Be(3);
    }

    [Fact]
    public void CanStartProcessing_ReturnsFalse_WhenProcessing()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsProcessing = true;
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });

        // Assert
        viewModel.CanStartProcessing.Should().BeFalse();
    }

    [Fact]
    public void CanStartProcessing_ReturnsFalse_WhenNoPendingJobs()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsProcessing = false;
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });

        // Assert
        viewModel.CanStartProcessing.Should().BeFalse();
    }

    [Fact]
    public void CanStartProcessing_ReturnsTrue_WhenNotProcessingAndHasPendingJobs()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.IsProcessing = false;
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });

        // Assert
        viewModel.CanStartProcessing.Should().BeTrue();
    }

    [Fact]
    public void EstimatedTotalTime_CalculatesBasedOnPendingJobs()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });

        // Assert (3 pending * 5 minutes = 15 minutes)
        viewModel.EstimatedTotalTime.Should().Be(TimeSpan.FromMinutes(15));
    }

    #endregion

    #region Toggle Show Original Tests

    [Fact]
    public void ToggleShowOriginal_TogglesState()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.ShowOriginalInToggle = false;

        // Act
        viewModel.ToggleShowOriginalCommand.Execute(null);

        // Assert
        viewModel.ShowOriginalInToggle.Should().BeTrue();
    }

    #endregion

    #region Parameter Reset Tests

    [Fact]
    public void ResetParameter_ResetsToDefault()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);
        var param = new PresetParameter
        {
            Name = "test",
            DefaultValue = 100,
            CurrentValue = 50
        };

        // Act
        viewModel.ResetParameterCommand.Execute(param);

        // Assert
        param.CurrentValue.Should().Be(100);
    }

    [Fact]
    public void ResetParameter_WithNullParameter_DoesNotThrow()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        var viewModel = CreateViewModel(mockMediaPool);

        // Act & Assert
        var action = () => viewModel.ResetParameterCommand.Execute(null);
        action.Should().NotThrow();
    }

    #endregion

    #region Select Category Command Tests

    [Fact]
    public void SelectCategoryCommand_ChangesSelectedCategory()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectCategoryCommand.Execute("Denoise");

        // Assert
        viewModel.SelectedCategory.Should().Be("Denoise");
    }

    [Fact]
    public void SelectCategoryCommand_FiltersPresetsToCategory()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectCategoryCommand.Execute("Upscale");

        // Assert
        viewModel.FilteredPresets.Should().OnlyContain(p => p.Category == "Upscale");
    }

    #endregion

    #region Apply Preset Command Tests

    [Fact]
    public void ApplyPresetCommand_SetsSelectedPreset()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var preset = viewModel.Presets.First();

        // Act
        viewModel.ApplyPresetCommand.Execute(preset);

        // Assert
        viewModel.SelectedPreset.Should().Be(preset);
    }

    [Fact]
    public void ApplyPresetCommand_UpdatesStatusText()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var preset = viewModel.Presets.First();

        // Act
        viewModel.ApplyPresetCommand.Execute(preset);

        // Assert
        viewModel.StatusText.Should().Contain(preset.Name);
    }

    [Fact]
    public void ApplyPresetCommand_WarnsWhenGpuRequiredButNotAvailable()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var gpuPreset = viewModel.Presets.FirstOrDefault(p => p.RequiresGpu);
        if (gpuPreset == null) return; // Skip if no GPU presets

        // Force GPU unavailable
        viewModel.GetType().GetProperty("GpuAvailable")?.SetValue(viewModel, false);

        // Act
        viewModel.ApplyPresetCommand.Execute(gpuPreset);

        // Assert
        viewModel.StatusText.Should().Contain("Warning");
    }

    #endregion

    #region Apply To Source Command Tests

    [Fact]
    public void ApplyToSourceCommand_WithNoSource_DoesNotThrow()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(false);
        mockMediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var viewModel = CreateViewModel(mockMediaPool);

        // Act & Assert
        var action = () => viewModel.ApplyToSourceCommand.Execute(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void ApplyToSourceCommand_WithNoPreset_DoesNotThrow()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedPreset = null;

        // Act & Assert
        var action = () => viewModel.ApplyToSourceCommand.Execute(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void ApplyToSourceCommand_AppliesRestorationToMediaItem()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedPreset = viewModel.Presets.First();

        // Act
        viewModel.ApplyToSourceCommand.Execute(null);

        // Assert
        mediaItem.AppliedRestoration.Should().NotBeNull();
        mediaItem.AppliedRestoration!.PresetName.Should().Be(viewModel.SelectedPreset.Name);
    }

    [Fact]
    public void ApplyToSourceCommand_SetsRestorationEnabled()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedPreset = viewModel.Presets.First();

        // Act
        viewModel.ApplyToSourceCommand.Execute(null);

        // Assert
        mediaItem.AppliedRestoration!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ApplyToSourceCommand_CapturesParameters()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);
        var preset = viewModel.Presets.First(p => p.Parameters.Count > 0);
        viewModel.SelectedPreset = preset;

        // Act
        viewModel.ApplyToSourceCommand.Execute(null);

        // Assert
        mediaItem.AppliedRestoration!.Parameters.Should().NotBeEmpty();
    }

    #endregion

    #region Send To Export Command Tests

    [Fact]
    public void SendToExportCommand_WithNoSource_DoesNotNavigate()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(false);
        var mockNavigation = new Mock<INavigationService>();
        var mockVsService = new Mock<IVapourSynthService>();
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(s => s.Load()).Returns(new AppSettings());
        var viewModel = new RestoreViewModel(mockMediaPool.Object, mockVsService.Object, mockSettingsService.Object, mockNavigation.Object);

        // Act
        viewModel.SendToExportCommand.Execute(null);

        // Assert
        mockNavigation.Verify(n => n.NavigateTo(It.IsAny<PageType>()), Times.Never);
    }

    [Fact]
    public void SendToExportCommand_NavigatesToExportPage()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var mockNavigation = new Mock<INavigationService>();
        var mockVsService = new Mock<IVapourSynthService>();
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(s => s.Load()).Returns(new AppSettings());
        var viewModel = new RestoreViewModel(mockMediaPool.Object, mockVsService.Object, mockSettingsService.Object, mockNavigation.Object);
        viewModel.SelectedPreset = viewModel.Presets.First();

        // Act
        viewModel.SendToExportCommand.Execute(null);

        // Assert
        mockNavigation.Verify(n => n.NavigateTo(PageType.Export), Times.Once);
    }

    [Fact]
    public void SendToExportCommand_AppliesRestorationBeforeNavigating()
    {
        // Arrange
        var mediaItem = new MediaItem { Name = "test.mp4", FilePath = @"C:\test.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var mockNavigation = new Mock<INavigationService>();
        var mockVsService = new Mock<IVapourSynthService>();
        var mockSettingsService = new Mock<ISettingsService>();
        mockSettingsService.Setup(s => s.Load()).Returns(new AppSettings());
        var viewModel = new RestoreViewModel(mockMediaPool.Object, mockVsService.Object, mockSettingsService.Object, mockNavigation.Object);
        viewModel.SelectedPreset = viewModel.Presets.First();

        // Act
        viewModel.SendToExportCommand.Execute(null);

        // Assert
        mediaItem.AppliedRestoration.Should().NotBeNull();
    }

    #endregion

    #region Clear Restoration Command Tests

    [Fact]
    public void ClearRestorationCommand_WithNoSource_DoesNotThrow()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var viewModel = CreateViewModel(mockMediaPool);

        // Act & Assert
        var action = () => viewModel.ClearRestorationCommand.Execute(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void ClearRestorationCommand_ClearsAppliedRestoration()
    {
        // Arrange
        var mediaItem = new MediaItem
        {
            Name = "test.mp4",
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Test" }
        };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);

        // Act
        viewModel.ClearRestorationCommand.Execute(null);

        // Assert
        mediaItem.AppliedRestoration.Should().BeNull();
    }

    [Fact]
    public void ClearRestorationCommand_UpdatesStatusText()
    {
        // Arrange
        var mediaItem = new MediaItem
        {
            Name = "test.mp4",
            FilePath = @"C:\test.mp4",
            AppliedRestoration = new RestorationSettings { PresetName = "Test" }
        };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);

        // Act
        viewModel.ClearRestorationCommand.Execute(null);

        // Assert
        viewModel.StatusText.Should().Contain("Cleared");
    }

    #endregion

    #region Cancel Processing Command Tests

    [Fact]
    public void CancelProcessingCommand_UpdatesStatusText()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.CancelProcessingCommand.Execute(null);

        // Assert
        viewModel.StatusText.Should().Be("Cancelled");
    }

    #endregion

    #region Clear Queue Command Tests

    [Fact]
    public void ClearQueueCommand_RemovesCompletedJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });

        // Act
        viewModel.ClearQueueCommand.Execute(null);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
        viewModel.JobQueue.Should().OnlyContain(j => j.Status == ProcessingStatus.Pending);
    }

    [Fact]
    public void ClearQueueCommand_RemovesFailedJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Failed });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });

        // Act
        viewModel.ClearQueueCommand.Execute(null);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
    }

    [Fact]
    public void ClearQueueCommand_RemovesCancelledJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Cancelled });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });

        // Act
        viewModel.ClearQueueCommand.Execute(null);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
    }

    [Fact]
    public void ClearQueueCommand_KeepsProcessingJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Processing });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });

        // Act
        viewModel.ClearQueueCommand.Execute(null);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
        viewModel.JobQueue.First().Status.Should().Be(ProcessingStatus.Processing);
    }

    [Fact]
    public void ClearQueueCommand_KeepsPendingJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { Status = ProcessingStatus.Completed });

        // Act
        viewModel.ClearQueueCommand.Execute(null);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
        viewModel.JobQueue.First().Status.Should().Be(ProcessingStatus.Pending);
    }

    #endregion

    #region Remove Job Command Tests

    [Fact]
    public void RemoveJobCommand_RemovesPendingJob()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var job = new RestoreJob { Status = ProcessingStatus.Pending };
        viewModel.JobQueue.Add(job);

        // Act
        viewModel.RemoveJobCommand.Execute(job);

        // Assert
        viewModel.JobQueue.Should().BeEmpty();
    }

    [Fact]
    public void RemoveJobCommand_RemovesCompletedJob()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var job = new RestoreJob { Status = ProcessingStatus.Completed };
        viewModel.JobQueue.Add(job);

        // Act
        viewModel.RemoveJobCommand.Execute(job);

        // Assert
        viewModel.JobQueue.Should().BeEmpty();
    }

    [Fact]
    public void RemoveJobCommand_DoesNotRemoveProcessingJob()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var job = new RestoreJob { Status = ProcessingStatus.Processing };
        viewModel.JobQueue.Add(job);

        // Act
        viewModel.RemoveJobCommand.Execute(job);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
    }

    #endregion

    #region Toggle Mode Command Tests

    [Fact]
    public void ToggleModeCommand_SwitchesToAdvancedMode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsSimpleMode = true;

        // Act
        viewModel.ToggleModeCommand.Execute(null);

        // Assert
        viewModel.IsSimpleMode.Should().BeFalse();
    }

    [Fact]
    public void ToggleModeCommand_SwitchesBackToSimpleMode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsSimpleMode = false;

        // Act
        viewModel.ToggleModeCommand.Execute(null);

        // Assert
        viewModel.IsSimpleMode.Should().BeTrue();
    }

    #endregion

    #region Comparison Mode Tests

    [Fact]
    public void SetComparisonModeCommand_SetsMode()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SetComparisonModeCommand.Execute(ComparisonMode.Wipe);

        // Assert
        viewModel.ComparisonMode.Should().Be(ComparisonMode.Wipe);
    }

    [Fact]
    public void SetComparisonModeCommand_SetsSideBySide()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ComparisonMode = ComparisonMode.Wipe;

        // Act
        viewModel.SetComparisonModeCommand.Execute(ComparisonMode.SideBySide);

        // Assert
        viewModel.ComparisonMode.Should().Be(ComparisonMode.SideBySide);
    }

    [Fact]
    public void ToggleComparisonCommand_TogglesShowComparison()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ShowComparison = false;

        // Act
        viewModel.ToggleComparisonCommand.Execute(null);

        // Assert
        viewModel.ShowComparison.Should().BeTrue();
    }

    [Fact]
    public void ToggleComparisonCommand_TogglesBackToFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.ShowComparison = true;

        // Act
        viewModel.ToggleComparisonCommand.Execute(null);

        // Assert
        viewModel.ShowComparison.Should().BeFalse();
    }

    #endregion

    #region Project Persistence Tests

    [Fact]
    public void ExportToProject_SavesJobQueue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob
        {
            SourcePath = @"C:\source.mp4",
            OutputPath = @"C:\output.mp4",
            Preset = viewModel.Presets.First(),
            Status = ProcessingStatus.Pending
        });
        var project = new Project();

        // Act
        viewModel.ExportToProject(project);

        // Assert
        project.RestoreJobs.Should().HaveCount(1);
        project.RestoreJobs.First().SourcePath.Should().Be(@"C:\source.mp4");
    }

    [Fact]
    public void ExportToProject_ClearsExistingJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.RestoreJobs.Add(new RestoreJobData { SourcePath = "old.mp4" });

        // Act
        viewModel.ExportToProject(project);

        // Assert
        project.RestoreJobs.Should().BeEmpty();
    }

    [Fact]
    public void ExportToProject_SavesMultipleJobs()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { SourcePath = @"C:\source1.mp4", Status = ProcessingStatus.Pending });
        viewModel.JobQueue.Add(new RestoreJob { SourcePath = @"C:\source2.mp4", Status = ProcessingStatus.Completed });
        viewModel.JobQueue.Add(new RestoreJob { SourcePath = @"C:\source3.mp4", Status = ProcessingStatus.Failed });
        var project = new Project();

        // Act
        viewModel.ExportToProject(project);

        // Assert
        project.RestoreJobs.Should().HaveCount(3);
    }

    [Fact]
    public void ImportFromProject_LoadsJobQueue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.RestoreJobs.Add(new RestoreJobData
        {
            SourcePath = @"C:\source.mp4",
            OutputPath = @"C:\output.mp4",
            PresetName = viewModel.Presets.First().Name,
            Status = ProcessingStatus.Pending
        });

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.JobQueue.Should().HaveCount(1);
        viewModel.JobQueue.First().SourcePath.Should().Be(@"C:\source.mp4");
    }

    [Fact]
    public void ImportFromProject_ClearsExistingQueue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.JobQueue.Add(new RestoreJob { SourcePath = "existing.mp4" });
        var project = new Project();

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.JobQueue.Should().BeEmpty();
    }

    [Fact]
    public void ImportFromProject_MatchesPresetByName()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var presetName = viewModel.Presets.First().Name;
        var project = new Project();
        project.RestoreJobs.Add(new RestoreJobData
        {
            SourcePath = @"C:\source.mp4",
            PresetName = presetName
        });

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.JobQueue.First().Preset.Should().NotBeNull();
        viewModel.JobQueue.First().Preset!.Name.Should().Be(presetName);
    }

    [Fact]
    public void ImportFromProject_HandlesUnknownPreset()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.RestoreJobs.Add(new RestoreJobData
        {
            SourcePath = @"C:\source.mp4",
            PresetName = "NonExistentPreset999"
        });

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.JobQueue.First().Preset.Should().BeNull();
    }

    [Fact]
    public void ImportFromProject_UpdatesStatusText()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.RestoreJobs.Add(new RestoreJobData { SourcePath = @"C:\source.mp4" });

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.StatusText.Should().Contain("Loaded");
    }

    [Fact]
    public void ImportFromProject_EmptyProject_SetsReadyStatus()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();

        // Act
        viewModel.ImportFromProject(project);

        // Assert
        viewModel.StatusText.Should().Be("Ready");
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var action = () =>
        {
            viewModel.Dispose();
            viewModel.Dispose();
            viewModel.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_PropertiesStillAccessible()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SelectedCategory = "Denoise";

        // Act
        viewModel.Dispose();

        // Assert
        viewModel.SelectedCategory.Should().Be("Denoise");
    }

    #endregion

    #region Source Info Property Tests

    [Fact]
    public void SourcePath_ReturnsEmptyWhenNoSource()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.CurrentSource).Returns((MediaItem?)null);
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.SourcePath.Should().BeEmpty();
    }

    [Fact]
    public void SourcePath_ReturnsFilePathWhenSourceSet()
    {
        // Arrange
        var mediaItem = new MediaItem { FilePath = @"C:\test\video.mp4" };
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.CurrentSource).Returns(mediaItem);
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.SourcePath.Should().Be(@"C:\test\video.mp4");
    }

    [Fact]
    public void HasSource_ReturnsFalseWhenNoSource()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(false);
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.HasSource.Should().BeFalse();
    }

    [Fact]
    public void HasSource_ReturnsTrueWhenSourceSet()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.HasSource.Should().BeTrue();
    }

    #endregion

    #region Preview State Tests

    [Fact]
    public void HasPreview_ReturnsFalseInitially()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.HasPreview.Should().BeFalse();
    }

    [Fact]
    public void CanGeneratePreview_ReturnsFalseWithoutSource()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(false);
        var viewModel = CreateViewModel(mockMediaPool);

        // Assert
        viewModel.CanGeneratePreview.Should().BeFalse();
    }

    [Fact]
    public void CanGeneratePreview_ReturnsFalseWithoutPreset()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedPreset = null;

        // Assert
        viewModel.CanGeneratePreview.Should().BeFalse();
    }

    [Fact]
    public void CanGeneratePreview_ReturnsFalseWhileGenerating()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();
        mockMediaPool.Setup(m => m.HasSource).Returns(true);
        var viewModel = CreateViewModel(mockMediaPool);
        viewModel.SelectedPreset = viewModel.Presets.First();
        viewModel.IsGeneratingPreview = true;

        // Assert
        viewModel.CanGeneratePreview.Should().BeFalse();
    }

    #endregion

    #region Wipe Position Tests

    [Fact]
    public void WipePosition_DefaultsToMiddle()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.WipePosition.Should().Be(0.5);
    }

    [Fact]
    public void WipePosition_CanBeSet()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.WipePosition = 0.75;

        // Assert
        viewModel.WipePosition.Should().Be(0.75);
    }

    #endregion

    #region GPU Properties Tests

    [Fact]
    public void GpuAvailable_CanBeAccessed()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert - Just verify we can access it without crashing
        var _ = viewModel.GpuAvailable;
    }

    [Fact]
    public void GpuName_CanBeAccessed()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert - Just verify we can access it without crashing
        var _ = viewModel.GpuName;
    }

    #endregion

    #region Model Settings Tests

    [Fact]
    public void ModelSettings_InitializedByDefault()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ModelSettings.Should().NotBeNull();
    }

    [Fact]
    public void ModelSettings_CanBeSet()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var newSettings = new AIModelSettings { Fp16 = false };

        // Act
        viewModel.ModelSettings = newSettings;

        // Assert
        viewModel.ModelSettings.Should().Be(newSettings);
        viewModel.ModelSettings.Fp16.Should().BeFalse();
    }

    #endregion

    #region Selected Preset Change Tests

    [Fact]
    public void SelectedPreset_ClearsProcessedFrame()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedPreset = viewModel.Presets.First();

        // Assert
        viewModel.ProcessedFrame.Should().BeNull();
    }

    [Fact]
    public void SelectedPreset_InitializesParameterCurrentValues()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var preset = viewModel.Presets.First(p => p.Parameters.Count > 0);
        foreach (var param in preset.Parameters)
        {
            param.CurrentValue = null;
        }

        // Act
        viewModel.SelectedPreset = preset;

        // Assert
        viewModel.SelectedPreset.Parameters.Should().OnlyContain(p => p.CurrentValue != null);
    }

    #endregion

    #region MoveJob Null Handling Tests

    [Fact]
    public void MoveJobUpCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var action = () => viewModel.MoveJobUpCommand.Execute(null);
        action.Should().NotThrow();
    }

    [Fact]
    public void MoveJobDownCommand_WithNull_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var action = () => viewModel.MoveJobDownCommand.Execute(null);
        action.Should().NotThrow();
    }

    #endregion

    #region Combined Filter Tests

    [Fact]
    public void Filter_CombinesCategoryAndSearch()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act - Set both filters
        viewModel.SelectedCategory = "Denoise";
        viewModel.SearchQuery = "a";

        // Assert - Should filter by both category AND search
        viewModel.FilteredPresets.Should().OnlyContain(p =>
            p.Category == "Denoise" &&
            (p.Name.Contains("a", StringComparison.OrdinalIgnoreCase) ||
             p.Description.Contains("a", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Filter_FavoritesAndSearch_Combined()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Presets[0].IsFavorite = true;
        viewModel.Presets[1].IsFavorite = true;

        // Act
        viewModel.SelectedCategory = "Favorites";
        viewModel.SearchQuery = viewModel.Presets[0].Name.Substring(0, 3);

        // Assert
        viewModel.FilteredPresets.Should().OnlyContain(p => p.IsFavorite);
    }

    #endregion
}
