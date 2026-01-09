using VapourSynthPortable.Tests.Fixtures;

namespace VapourSynthPortable.Tests.Services;

public class ProjectServiceTests : IDisposable
{
    private readonly ProjectService _service;
    private readonly TempDirectoryFixture _tempDir;

    public ProjectServiceTests()
    {
        _service = new ProjectService();
        _tempDir = new TempDirectoryFixture();
    }

    public void Dispose()
    {
        _tempDir.Dispose();
    }

    #region CreateNew Tests

    [Fact]
    public void CreateNew_ReturnsProject_WithDefaultName()
    {
        // Act
        var project = _service.CreateNew();

        // Assert
        project.Should().NotBeNull();
        project.Name.Should().Be("Untitled");
    }

    [Fact]
    public void CreateNew_ReturnsProject_WithDefaultTracks()
    {
        // Act
        var project = _service.CreateNew();

        // Assert
        project.TimelineData.Should().NotBeNull();
        project.TimelineData.Tracks.Should().HaveCount(4);
        project.TimelineData.Tracks.Should().Contain(t => t.Name == "V1" && t.TrackType == TrackType.Video);
        project.TimelineData.Tracks.Should().Contain(t => t.Name == "V2" && t.TrackType == TrackType.Video);
        project.TimelineData.Tracks.Should().Contain(t => t.Name == "A1" && t.TrackType == TrackType.Audio);
        project.TimelineData.Tracks.Should().Contain(t => t.Name == "A2" && t.TrackType == TrackType.Audio);
    }

    [Fact]
    public void CreateNew_ReturnsProject_WithTimestamps()
    {
        // Arrange
        var beforeCreate = DateTime.Now;

        // Act
        var project = _service.CreateNew();

        // Assert
        project.CreatedDate.Should().BeOnOrAfter(beforeCreate);
        project.ModifiedDate.Should().BeOnOrAfter(beforeCreate);
    }

    [Fact]
    public void CreateNew_ReturnsProject_WithDefaultSettings()
    {
        // Act
        var project = _service.CreateNew();

        // Assert
        project.Settings.Should().NotBeNull();
    }

    #endregion

    #region SaveAsync/LoadAsync Tests

    [Fact]
    public async Task SaveAsync_WritesJsonFile()
    {
        // Arrange
        var project = _service.CreateNew();
        project.Name = "TestProject";
        var filePath = _tempDir.GetPath("test.vsproj");

        // Act
        await _service.SaveAsync(project, filePath);

        // Assert
        _tempDir.FileExists("test.vsproj").Should().BeTrue();
        var content = _tempDir.ReadFile("test.vsproj");
        content.Should().Contain("TestProject");
    }

    [Fact]
    public async Task SaveAsync_SetsFilePath()
    {
        // Arrange
        var project = _service.CreateNew();
        var filePath = _tempDir.GetPath("test.vsproj");

        // Act
        await _service.SaveAsync(project, filePath);

        // Assert
        project.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task SaveAsync_MarksProjectClean()
    {
        // Arrange
        var project = _service.CreateNew();
        project.Name = "Modified"; // This should mark it dirty
        var filePath = _tempDir.GetPath("test.vsproj");

        // Act
        await _service.SaveAsync(project, filePath);

        // Assert
        project.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ReturnsNull_WhenFileNotExists()
    {
        // Arrange
        var filePath = _tempDir.GetPath("nonexistent.vsproj");

        // Act
        var result = await _service.LoadAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_DeserializesProject()
    {
        // Arrange
        var project = _service.CreateNew();
        project.Name = "LoadTest";
        var filePath = _tempDir.GetPath("load_test.vsproj");
        await _service.SaveAsync(project, filePath);

        // Act
        var loaded = await _service.LoadAsync(filePath);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("LoadTest");
    }

    [Fact]
    public async Task LoadAsync_SetsFilePath()
    {
        // Arrange
        var project = _service.CreateNew();
        var filePath = _tempDir.GetPath("path_test.vsproj");
        await _service.SaveAsync(project, filePath);

        // Act
        var loaded = await _service.LoadAsync(filePath);

        // Assert
        loaded!.FilePath.Should().Be(filePath);
    }

    [Fact]
    public async Task LoadAsync_MarksProjectClean()
    {
        // Arrange
        var project = _service.CreateNew();
        var filePath = _tempDir.GetPath("clean_test.vsproj");
        await _service.SaveAsync(project, filePath);

        // Act
        var loaded = await _service.LoadAsync(filePath);

        // Assert
        loaded!.IsDirty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ThrowsException_WhenJsonInvalid()
    {
        // Arrange
        var filePath = _tempDir.CreateFile("invalid.vsproj", "{ invalid json }}}");

        // Act & Assert - The service throws on invalid JSON (no try-catch in LoadAsync)
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await _service.LoadAsync(filePath));
    }

    [Fact]
    public async Task SaveAndLoad_PreservesTimelineData()
    {
        // Arrange
        var project = _service.CreateNew();
        project.TimelineData.FrameRate = 29.97;
        project.TimelineData.PlayheadFrame = 100;
        var filePath = _tempDir.GetPath("timeline_test.vsproj");

        // Act
        await _service.SaveAsync(project, filePath);
        var loaded = await _service.LoadAsync(filePath);

        // Assert
        loaded!.TimelineData.FrameRate.Should().Be(29.97);
        loaded.TimelineData.PlayheadFrame.Should().Be(100);
    }

    #endregion

    #region RecentProjects Tests

    [Fact]
    public void GetRecentProjects_ReturnsNonNullList()
    {
        // Act
        var result = _service.GetRecentProjects();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddToRecentProjects_DoesNotThrow()
    {
        // Arrange
        var testPath = _tempDir.GetPath($"recent_test_{Guid.NewGuid():N}.vsproj");

        // Act & Assert - Should not throw
        var action = () => _service.AddToRecentProjects(testPath);
        action.Should().NotThrow();
    }

    [Fact]
    public void AddToRecentProjects_ReturnsListAfterAdd()
    {
        // Arrange
        var testPath = _tempDir.GetPath($"recent_test_{Guid.NewGuid():N}.vsproj");

        // Act
        _service.AddToRecentProjects(testPath);
        var recent = _service.GetRecentProjects();

        // Assert - Method should return a list (actual persistence depends on AppData)
        recent.Should().NotBeNull();
        recent.Should().BeOfType<List<string>>();
    }

    #endregion

    #region AutoSave Tests

    [Fact]
    public void GetAutoSavePath_ReturnsHiddenFile_WhenProjectHasPath()
    {
        // Arrange
        var project = _service.CreateNew();
        project.FilePath = _tempDir.GetPath("myproject.vsproj");

        // Act
        var autoSavePath = _service.GetAutoSavePath(project);

        // Assert
        autoSavePath.Should().Contain(".myproject_autosave.vsproj");
    }

    [Fact]
    public void GetAutoSavePath_ReturnsTempPath_WhenProjectHasNoPath()
    {
        // Arrange
        var project = _service.CreateNew();

        // Act
        var autoSavePath = _service.GetAutoSavePath(project);

        // Assert
        autoSavePath.Should().Contain("autosave");
        autoSavePath.Should().EndWith(".vsproj");
    }

    #endregion
}
