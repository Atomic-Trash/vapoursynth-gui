using VapourSynthPortable.ViewModels;

namespace VapourSynthPortable.Tests.ViewModels;

public class RestoreViewModelTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_WithMediaPoolService_DoesNotThrow()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();

        // Act & Assert
        var action = () => new RestoreViewModel(mockMediaPool.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_InitializesPresetsCollection()
    {
        // Arrange
        var mockMediaPool = new Mock<IMediaPoolService>();

        // Act
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);
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
        var viewModel = new RestoreViewModel(mockMediaPool.Object);

        // Act & Assert
        var action = () => viewModel.ResetParameterCommand.Execute(null);
        action.Should().NotThrow();
    }

    #endregion
}
