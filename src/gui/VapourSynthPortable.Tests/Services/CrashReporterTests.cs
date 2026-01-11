using System.IO;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class CrashReporterTests
{
    #region Initialize Tests

    [Fact]
    public void Initialize_CreatesDirectory()
    {
        // Act
        CrashReporter.Initialize();

        // Assert
        Assert.True(Directory.Exists(CrashReporter.CrashDirectory));
    }

    [Fact]
    public void Initialize_CanBeCalledMultipleTimes()
    {
        // Act & Assert - should not throw
        CrashReporter.Initialize();
        CrashReporter.Initialize();
        CrashReporter.Initialize();
    }

    [Fact]
    public void CrashDirectory_IsInLocalAppData()
    {
        // Arrange
        CrashReporter.Initialize();

        // Assert
        Assert.Contains("VapourSynthStudio", CrashReporter.CrashDirectory);
        Assert.Contains("crashes", CrashReporter.CrashDirectory);
    }

    #endregion

    #region CreateCrashReport Tests

    [Fact]
    public void CreateCrashReport_CreatesFile()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new InvalidOperationException("Test exception for crash report");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");

        // Assert
        Assert.NotNull(reportPath);
        Assert.True(File.Exists(reportPath));

        // Cleanup
        if (File.Exists(reportPath))
        {
            File.Delete(reportPath);
        }
    }

    [Fact]
    public void CreateCrashReport_ContainsExceptionDetails()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new ArgumentNullException("testParam", "This is a test exception");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("ArgumentNullException", content);
        Assert.Contains("testParam", content);
        Assert.Contains("This is a test exception", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_ContainsEnvironmentInfo()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("ENVIRONMENT", content);
        Assert.Contains("OS Version", content);
        Assert.Contains(".NET Version", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_ContainsSource()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "CustomSource");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("CustomSource", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_HandlesNestedExceptions()
    {
        // Arrange
        CrashReporter.Initialize();
        var inner = new InvalidOperationException("Inner exception");
        var outer = new Exception("Outer exception", inner);

        // Act
        var reportPath = CrashReporter.CreateCrashReport(outer, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("Outer exception", content);
        Assert.Contains("Inner exception", content);
        Assert.Contains("INNER EXCEPTION", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_HandlesAggregateException()
    {
        // Arrange
        CrashReporter.Initialize();
        var exceptions = new[]
        {
            new Exception("First inner"),
            new Exception("Second inner"),
            new Exception("Third inner")
        };
        var aggregate = new AggregateException("Aggregate exception", exceptions);

        // Act
        var reportPath = CrashReporter.CreateCrashReport(aggregate, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("Aggregate exception", content);
        Assert.Contains("First inner", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_IncludesTerminatingFlag()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest", isTerminating: true);
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("Is Terminating: True", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_IncludesTimestamp()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test");
        var before = DateTime.Now;

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("Timestamp:", content);
        Assert.Contains(before.Year.ToString(), content);

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_FileNameContainsTimestamp()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test");
        var today = DateTime.Now.ToString("yyyyMMdd");

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");
        var fileName = Path.GetFileName(reportPath!);

        // Assert
        Assert.StartsWith("crash_", fileName);
        Assert.Contains(today, fileName);
        Assert.EndsWith(".txt", fileName);

        // Cleanup
        File.Delete(reportPath!);
    }

    #endregion

    #region GetCrashReportCount Tests

    [Fact]
    public void GetCrashReportCount_ReturnsZeroWhenEmpty()
    {
        // Arrange
        CrashReporter.Initialize();
        CrashReporter.CleanupOldReports(0); // Remove all existing reports

        // Act
        var count = CrashReporter.GetCrashReportCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetCrashReportCount_CountsReports()
    {
        // Arrange
        CrashReporter.Initialize();
        CrashReporter.CleanupOldReports(0); // Remove all existing reports

        var path1 = CrashReporter.CreateCrashReport(new Exception("Test 1"), "Test");
        var path2 = CrashReporter.CreateCrashReport(new Exception("Test 2"), "Test");

        // Act
        var count = CrashReporter.GetCrashReportCount();

        // Assert
        Assert.Equal(2, count);

        // Cleanup
        File.Delete(path1!);
        File.Delete(path2!);
    }

    #endregion

    #region CleanupOldReports Tests

    [Fact]
    public void CleanupOldReports_KeepsSpecifiedCount()
    {
        // Arrange
        CrashReporter.Initialize();
        CrashReporter.CleanupOldReports(0); // Start fresh

        // Create 5 reports
        var paths = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var path = CrashReporter.CreateCrashReport(new Exception($"Test {i}"), "Test");
            if (path != null) paths.Add(path);
            Thread.Sleep(10); // Ensure different timestamps
        }

        // Act - keep only 3
        CrashReporter.CleanupOldReports(3);

        // Assert
        var remaining = CrashReporter.GetCrashReportCount();
        Assert.Equal(3, remaining);

        // Cleanup remaining
        CrashReporter.CleanupOldReports(0);
    }

    [Fact]
    public void CleanupOldReports_KeepsNewestReports()
    {
        // Arrange
        CrashReporter.Initialize();
        CrashReporter.CleanupOldReports(0);

        // Create reports with different times
        CrashReporter.CreateCrashReport(new Exception("Old"), "Test");
        Thread.Sleep(50);
        var newerPath = CrashReporter.CreateCrashReport(new Exception("Newer"), "Test");

        // Act - keep only 1
        CrashReporter.CleanupOldReports(1);

        // Assert
        Assert.True(File.Exists(newerPath)); // Newer should remain

        // Cleanup
        CrashReporter.CleanupOldReports(0);
    }

    [Fact]
    public void CleanupOldReports_HandlesEmptyDirectory()
    {
        // Arrange
        CrashReporter.Initialize();
        CrashReporter.CleanupOldReports(0);

        // Act & Assert - should not throw
        CrashReporter.CleanupOldReports(10);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateCrashReport_HandlesExceptionWithNullMessage()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception(null);

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");

        // Assert
        Assert.NotNull(reportPath);
        Assert.True(File.Exists(reportPath));

        // Cleanup
        File.Delete(reportPath!);
    }

    [Fact]
    public void CreateCrashReport_HandlesExceptionWithData()
    {
        // Arrange
        CrashReporter.Initialize();
        var exception = new Exception("Test with data");
        exception.Data["Key1"] = "Value1";
        exception.Data["Key2"] = 42;

        // Act
        var reportPath = CrashReporter.CreateCrashReport(exception, "UnitTest");
        var content = File.ReadAllText(reportPath!);

        // Assert
        Assert.Contains("Data:", content);
        Assert.Contains("Key1", content);
        Assert.Contains("Value1", content);

        // Cleanup
        File.Delete(reportPath!);
    }

    #endregion
}
