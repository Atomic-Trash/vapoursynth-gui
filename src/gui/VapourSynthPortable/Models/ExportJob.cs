using CommunityToolkit.Mvvm.ComponentModel;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Models;

public enum ExportJobStatus
{
    Pending,
    Encoding,
    Completed,
    Failed,
    Cancelled
}

public partial class ExportJob : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N")[..8];

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _inputPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private ExportJobStatus _status = ExportJobStatus.Pending;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusText = "Pending";

    [ObservableProperty]
    private double _fps;

    [ObservableProperty]
    private double _bitrate;

    [ObservableProperty]
    private double _speed;

    [ObservableProperty]
    private string _timeRemaining = "--:--:--";

    [ObservableProperty]
    private DateTime _startTime;

    [ObservableProperty]
    private DateTime _endTime;

    [ObservableProperty]
    private long _outputFileSize;

    [ObservableProperty]
    private ExportSettings _settings = new();

    public string StatusIcon => Status switch
    {
        ExportJobStatus.Pending => "\uE823",   // Clock
        ExportJobStatus.Encoding => "\uE768", // Play
        ExportJobStatus.Completed => "\uE73E", // Checkmark
        ExportJobStatus.Failed => "\uE711",    // Error
        ExportJobStatus.Cancelled => "\uE711", // Error
        _ => "\uE823"
    };

    public string StatusColor => Status switch
    {
        ExportJobStatus.Pending => "#888",
        ExportJobStatus.Encoding => "#0078D4",
        ExportJobStatus.Completed => "#10B981",
        ExportJobStatus.Failed => "#EF4444",
        ExportJobStatus.Cancelled => "#F59E0B",
        _ => "#888"
    };

    public string Duration
    {
        get
        {
            if (StartTime == default) return "";
            var end = EndTime != default ? EndTime : DateTime.Now;
            var duration = end - StartTime;
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    public string OutputFileSizeFormatted
    {
        get
        {
            if (OutputFileSize <= 0) return "";
            string[] sizes = ["B", "KB", "MB", "GB"];
            int order = 0;
            double size = OutputFileSize;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    public void UpdateProgress(EncodingProgressEventArgs args)
    {
        Progress = args.Progress;
        Fps = args.Fps;
        Bitrate = args.Bitrate;
        Speed = args.Speed;
        TimeRemaining = args.TimeRemaining;
        StatusText = $"{Progress:F1}% - {Fps:F1} fps - ETA: {TimeRemaining}";
    }
}
