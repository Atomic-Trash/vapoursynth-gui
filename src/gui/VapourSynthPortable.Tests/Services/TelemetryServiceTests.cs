using VapourSynthPortable.Services;

namespace VapourSynthPortable.Tests.Services;

public class TelemetryServiceTests
{
    #region TrackEvent Tests

    [Fact]
    public void TrackEvent_AddsEventToCollection()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackEvent("TestEvent");
        var report = service.GenerateReport();

        // Assert
        Assert.True(report.TotalEvents > 0);
        Assert.Contains(report.EventCounts, e => e.Key == "TestEvent");
    }

    [Fact]
    public void TrackEvent_WithProperties_IncludesProperties()
    {
        // Arrange
        var service = new TelemetryService();
        var props = new Dictionary<string, string> { ["Key1"] = "Value1", ["Key2"] = "Value2" };

        // Act
        service.TrackEvent("EventWithProps", props);
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.RecentEvents, e => e.Name == "EventWithProps" && e.Properties != null);
    }

    [Fact]
    public void TrackEvent_MultipleCalls_IncrementsCount()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackEvent("RepeatEvent");
        service.TrackEvent("RepeatEvent");
        service.TrackEvent("RepeatEvent");
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(3, report.EventCounts["RepeatEvent"]);
    }

    #endregion

    #region TrackMetric Tests

    [Fact]
    public void TrackMetric_AddsMetricToCollection()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackMetric("TestMetric", 42.5);
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.MetricAverages, m => m.Key == "TestMetric");
        Assert.Equal(42.5, report.MetricAverages["TestMetric"]);
    }

    [Fact]
    public void TrackMetric_MultipleValues_CalculatesAverage()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackMetric("AverageMetric", 10);
        service.TrackMetric("AverageMetric", 20);
        service.TrackMetric("AverageMetric", 30);
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(20, report.MetricAverages["AverageMetric"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void TrackMetric_HandlesExtremeValues(double value)
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackMetric("ExtremeMetric", value);
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.MetricAverages, m => m.Key == "ExtremeMetric");
    }

    #endregion

    #region TrackException Tests

    [Fact]
    public void TrackException_IncrementsExceptionCount()
    {
        // Arrange
        var service = new TelemetryService();
        var exception = new InvalidOperationException("Test exception");

        // Act
        service.TrackException(exception);
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(1, report.TotalExceptions);
    }

    [Fact]
    public void TrackException_AddsExceptionEvent()
    {
        // Arrange
        var service = new TelemetryService();
        var exception = new ArgumentNullException("testParam");

        // Act
        service.TrackException(exception);
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.EventCounts, e => e.Key == "Exception");
        Assert.Contains(report.RecentEvents, e => e.IsException);
    }

    [Fact]
    public void TrackException_WithProperties_MergesProperties()
    {
        // Arrange
        var service = new TelemetryService();
        var exception = new Exception("Test");
        var props = new Dictionary<string, string> { ["Context"] = "UnitTest" };

        // Act
        service.TrackException(exception, props);
        var report = service.GenerateReport();

        // Assert
        var exEvent = report.RecentEvents.FirstOrDefault(e => e.IsException);
        Assert.NotNull(exEvent);
        Assert.Contains("Context", exEvent.Properties!.Keys);
        Assert.Contains("ExceptionType", exEvent.Properties.Keys);
    }

    [Fact]
    public void TrackException_MultipleExceptions_CountsAll()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        service.TrackException(new Exception("First"));
        service.TrackException(new Exception("Second"));
        service.TrackException(new Exception("Third"));
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(3, report.TotalExceptions);
    }

    #endregion

    #region TrackOperation Tests

    [Fact]
    public void TrackOperation_TracksStartAndComplete()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        using (service.TrackOperation("TestOperation"))
        {
            Thread.Sleep(10); // Small delay to ensure measurable duration
        }
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.EventCounts, e => e.Key == "TestOperation.Started");
        Assert.Contains(report.EventCounts, e => e.Key == "TestOperation.Completed");
    }

    [Fact]
    public void TrackOperation_TracksDuration()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        using (service.TrackOperation("TimedOperation"))
        {
            Thread.Sleep(50);
        }
        var report = service.GenerateReport();

        // Assert
        Assert.Contains(report.MetricAverages, m => m.Key == "TimedOperation.Duration");
        Assert.True(report.MetricAverages["TimedOperation.Duration"] >= 40); // Allow some tolerance
    }

    #endregion

    #region GenerateReport Tests

    [Fact]
    public void GenerateReport_ReturnsValidReport()
    {
        // Arrange
        var service = new TelemetryService();
        service.TrackEvent("Event1");
        service.TrackMetric("Metric1", 100);

        // Act
        var report = service.GenerateReport();

        // Assert
        Assert.NotNull(report);
        Assert.True(report.GeneratedAt <= DateTime.Now);
        Assert.True(report.Uptime.TotalMilliseconds >= 0);
    }

    [Fact]
    public void GenerateReport_IncludesRecentEvents()
    {
        // Arrange
        var service = new TelemetryService();
        for (int i = 0; i < 10; i++)
        {
            service.TrackEvent($"Event{i}");
        }

        // Act
        var report = service.GenerateReport();

        // Assert
        Assert.True(report.RecentEvents.Count <= 50); // Max 50 recent events
        Assert.True(report.RecentEvents.Count >= 10);
    }

    [Fact]
    public void GenerateReport_EmptyService_ReturnsEmptyReport()
    {
        // Arrange
        var service = new TelemetryService();

        // Act
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(0, report.TotalEvents);
        Assert.Equal(0, report.TotalExceptions);
        Assert.Empty(report.EventCounts);
        Assert.Empty(report.MetricAverages);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllData()
    {
        // Arrange
        var service = new TelemetryService();
        service.TrackEvent("Event1");
        service.TrackMetric("Metric1", 100);
        service.TrackException(new Exception("Test"));

        // Act
        service.Clear();
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(0, report.TotalEvents);
        Assert.Equal(0, report.TotalExceptions);
        Assert.Empty(report.EventCounts);
        Assert.Empty(report.MetricAverages);
    }

    [Fact]
    public void Clear_AllowsNewDataAfterClear()
    {
        // Arrange
        var service = new TelemetryService();
        service.TrackEvent("BeforeClear");
        service.Clear();

        // Act
        service.TrackEvent("AfterClear");
        var report = service.GenerateReport();

        // Assert
        Assert.Equal(1, report.TotalEvents);
        Assert.Contains(report.EventCounts, e => e.Key == "AfterClear");
        Assert.DoesNotContain(report.EventCounts, e => e.Key == "BeforeClear");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void TrackEvent_ThreadSafe_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService();
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                service.TrackEvent($"ParallelEvent{i % 10}");
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
        var report = service.GenerateReport();
        Assert.True(report.TotalEvents > 0);
    }

    [Fact]
    public void TrackMetric_ThreadSafe_DoesNotThrow()
    {
        // Arrange
        var service = new TelemetryService();
        var exceptions = new List<Exception>();

        // Act
        Parallel.For(0, 100, i =>
        {
            try
            {
                service.TrackMetric("ParallelMetric", i);
            }
            catch (Exception ex)
            {
                lock (exceptions) exceptions.Add(ex);
            }
        });

        // Assert
        Assert.Empty(exceptions);
        var report = service.GenerateReport();
        Assert.Contains(report.MetricAverages, m => m.Key == "ParallelMetric");
    }

    #endregion

    #region TelemetryEvent Tests

    [Fact]
    public void TelemetryEvent_HasCorrectTimestamp()
    {
        // Arrange
        var before = DateTime.Now;
        var evt = new TelemetryEvent { Name = "Test" };
        var after = DateTime.Now;

        // Assert
        Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
    }

    [Fact]
    public void TelemetryEvent_DefaultValuesAreCorrect()
    {
        // Arrange & Act
        var evt = new TelemetryEvent();

        // Assert
        Assert.Equal("", evt.Name);
        Assert.Null(evt.Properties);
        Assert.False(evt.IsException);
    }

    #endregion

    #region TelemetryMetric Tests

    [Fact]
    public void TelemetryMetric_HasCorrectDefaults()
    {
        // Arrange & Act
        var metric = new TelemetryMetric();

        // Assert
        Assert.Equal("", metric.Name);
        Assert.Equal(0, metric.Value);
    }

    #endregion

    #region TelemetryReport Tests

    [Fact]
    public void TelemetryReport_HasCorrectDefaults()
    {
        // Arrange & Act
        var report = new TelemetryReport();

        // Assert
        Assert.True(report.GeneratedAt <= DateTime.Now);
        Assert.Empty(report.EventCounts);
        Assert.Empty(report.MetricAverages);
        Assert.Empty(report.RecentEvents);
    }

    #endregion
}
