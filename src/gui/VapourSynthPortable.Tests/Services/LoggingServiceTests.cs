using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

/// <summary>
/// Tests for LoggingService centralized logging functionality.
/// </summary>
public class LoggingServiceTests
{
    [Fact]
    public void Initialize_CanBeCalledMultipleTimes()
    {
        // Act - should not throw on multiple calls
        var exception = Record.Exception(() =>
        {
            LoggingService.Initialize();
            LoggingService.Initialize();
        });

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GetLogger_ReturnsNonNullLogger()
    {
        // Arrange
        LoggingService.Initialize();

        // Act
        var logger = LoggingService.GetLogger<LoggingServiceTests>();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void LogEntries_IsObservableCollection()
    {
        // Arrange
        LoggingService.Initialize();

        // Act
        var entries = LoggingService.LogEntries;

        // Assert
        Assert.NotNull(entries);
        Assert.IsType<ObservableCollection<LogEntry>>(entries);
    }

    [Fact]
    public void LogEntries_CanBeEnumerated()
    {
        // Arrange
        LoggingService.Initialize();

        // Act
        var count = LoggingService.LogEntries.Count;

        // Assert
        Assert.True(count >= 0);
    }

    [Fact]
    public void LogEntry_HasTimestamp()
    {
        // Arrange
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Information,
            Message = "Test"
        };

        // Assert
        Assert.NotEqual(default, entry.Timestamp);
    }

    [Fact]
    public void LogEntry_HasLevel()
    {
        // Arrange
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Warning,
            Message = "Test"
        };

        // Assert
        Assert.Equal(LogLevel.Warning, entry.Level);
    }

    [Fact]
    public void LogEntry_HasMessage()
    {
        // Arrange
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = LogLevel.Information,
            Message = "Test message content"
        };

        // Assert
        Assert.Equal("Test message content", entry.Message);
    }

    [Fact]
    public void LogDirectory_IsNotNull()
    {
        // Arrange
        LoggingService.Initialize();

        // Act
        var logDir = LoggingService.LogDirectory;

        // Assert
        Assert.NotNull(logDir);
    }

    [Fact]
    public void ClearLogs_EmptiesCollection()
    {
        // Arrange
        LoggingService.Initialize();
        var logger = LoggingService.GetLogger<LoggingServiceTests>();
        logger.LogInformation("Test entry");

        // Act
        LoggingService.ClearLogEntries();

        // Assert
        Assert.Empty(LoggingService.LogEntries);
    }

    [Fact]
    public async Task ConcurrentLogging_DoesNotThrow()
    {
        // Arrange
        LoggingService.Initialize();
        var logger = LoggingService.GetLogger<LoggingServiceTests>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                logger.LogInformation("Concurrent message {Index}", index);
            }));
        }

        // Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await Task.WhenAll(tasks);
        });
        Assert.Null(exception);
    }

    [Fact]
    public void LogError_WithException_DoesNotThrow()
    {
        // Arrange
        LoggingService.Initialize();
        var logger = LoggingService.GetLogger<LoggingServiceTests>();
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - no exception thrown
        var recordedException = Record.Exception(() =>
        {
            logger.LogError(exception, "Error occurred");
        });
        Assert.Null(recordedException);
    }

    [Fact]
    public void StructuredLogging_WithParameters_DoesNotThrow()
    {
        // Arrange
        LoggingService.Initialize();
        var logger = LoggingService.GetLogger<LoggingServiceTests>();

        // Act & Assert - no exception thrown
        var exception = Record.Exception(() =>
        {
            logger.LogInformation("User {UserId} performed {Action}", 123, "login");
        });
        Assert.Null(exception);
    }
}
