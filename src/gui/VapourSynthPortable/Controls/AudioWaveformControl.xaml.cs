using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Controls;

public partial class AudioWaveformControl : UserControl
{
    private WaveformData? _waveformData;
    private readonly AudioWaveformService _waveformService;
    private CancellationTokenSource? _loadCts;
    private readonly Random _random = new();

    public static readonly DependencyProperty WaveformColorProperty =
        DependencyProperty.Register(nameof(WaveformColor), typeof(Color), typeof(AudioWaveformControl),
            new PropertyMetadata(Color.FromRgb(0x4A, 0xCF, 0x6A), OnPropertyChanged));

    public static readonly DependencyProperty SourcePathProperty =
        DependencyProperty.Register(nameof(SourcePath), typeof(string), typeof(AudioWaveformControl),
            new PropertyMetadata(null, OnSourcePathChanged));

    public static readonly DependencyProperty InPointProperty =
        DependencyProperty.Register(nameof(InPoint), typeof(double), typeof(AudioWaveformControl),
            new PropertyMetadata(0.0, OnPropertyChanged));

    public static readonly DependencyProperty OutPointProperty =
        DependencyProperty.Register(nameof(OutPoint), typeof(double), typeof(AudioWaveformControl),
            new PropertyMetadata(1.0, OnPropertyChanged));

    public Color WaveformColor
    {
        get => (Color)GetValue(WaveformColorProperty);
        set => SetValue(WaveformColorProperty, value);
    }

    public string? SourcePath
    {
        get => (string?)GetValue(SourcePathProperty);
        set => SetValue(SourcePathProperty, value);
    }

    public double InPoint
    {
        get => (double)GetValue(InPointProperty);
        set => SetValue(InPointProperty, value);
    }

    public double OutPoint
    {
        get => (double)GetValue(OutPointProperty);
        set => SetValue(OutPointProperty, value);
    }

    public AudioWaveformControl()
    {
        InitializeComponent();
        _waveformService = new AudioWaveformService();
        Loaded += (s, e) => DrawWaveform();
        Unloaded += (s, e) => _loadCts?.Cancel();
    }

    private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioWaveformControl control)
        {
            control.LoadWaveformData();
        }
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioWaveformControl control)
        {
            control.DrawWaveform();
        }
    }

    private void WaveformCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        DrawWaveform();
    }

    private async void LoadWaveformData()
    {
        if (string.IsNullOrEmpty(SourcePath))
        {
            _waveformData = null;
            DrawWaveform();
            return;
        }

        // Cancel any pending load
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            // Extract real waveform using FFmpeg
            _waveformData = await _waveformService.ExtractWaveformAsync(
                SourcePath,
                samplesPerSecond: 100,
                ct: _loadCts.Token);

            DrawWaveform();
        }
        catch (OperationCanceledException)
        {
            // Ignore - cancelled due to new source
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioWaveform] Failed to load waveform: {ex.Message}");
            _waveformData = null;
            DrawWaveform();
        }
    }

    public void SetWaveformData(WaveformData? data)
    {
        _waveformData = data;
        DrawWaveform();
    }

    public void SetWaveformData(float[] data)
    {
        // Legacy support - convert float array to WaveformData
        var samples = new WaveformSample[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            samples[i] = new WaveformSample
            {
                Min = Math.Min(0, data[i]),
                Max = Math.Max(0, data[i]),
                Rms = Math.Abs(data[i]) * 0.707f
            };
        }

        _waveformData = new WaveformData
        {
            FilePath = SourcePath ?? "",
            Duration = data.Length / 100.0,
            SampleRate = 100,
            Samples = samples
        };
        DrawWaveform();
    }

    private void DrawWaveform()
    {
        WaveformCanvas.Children.Clear();

        double width = WaveformCanvas.ActualWidth;
        double height = WaveformCanvas.ActualHeight;

        if (width <= 0 || height <= 0 || _waveformData == null || _waveformData.Samples.Length == 0)
        {
            // Draw placeholder
            DrawPlaceholderWaveform(width, height);
            return;
        }

        // Get resampled waveform for the current view
        double startTime = InPoint * _waveformData.Duration;
        double endTime = OutPoint * _waveformData.Duration;
        int numBars = Math.Max(1, (int)width);

        var samples = _waveformData.GetResampledForWidth(numBars, startTime, endTime);

        if (samples.Length == 0)
        {
            DrawPlaceholderWaveform(width, height);
            return;
        }

        var waveformBrush = new SolidColorBrush(WaveformColor);
        var waveformDarkBrush = new SolidColorBrush(Color.FromArgb(100, WaveformColor.R, WaveformColor.G, WaveformColor.B));

        double centerY = height / 2;
        double barWidth = Math.Max(1, width / samples.Length);

        // Draw waveform bars
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            double x = i * barWidth;

            // Draw peak bar (lighter) - shows full peak-to-peak range
            double peakHeight = sample.PeakToPeak * centerY;
            if (peakHeight > 0.5)
            {
                var peakBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, peakHeight),
                    Fill = waveformDarkBrush
                };
                Canvas.SetLeft(peakBar, x);
                Canvas.SetTop(peakBar, centerY - peakHeight / 2);
                WaveformCanvas.Children.Add(peakBar);
            }

            // Draw RMS bar (brighter) - shows average level
            double rmsHeight = sample.Rms * centerY * 2;
            if (rmsHeight > 0.5)
            {
                var rmsBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, rmsHeight),
                    Fill = waveformBrush
                };
                Canvas.SetLeft(rmsBar, x);
                Canvas.SetTop(rmsBar, centerY - rmsHeight / 2);
                WaveformCanvas.Children.Add(rmsBar);
            }
        }

        // Draw center line
        var centerLine = new Line
        {
            X1 = 0,
            Y1 = centerY,
            X2 = width,
            Y2 = centerY,
            Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            StrokeThickness = 1
        };
        WaveformCanvas.Children.Add(centerLine);
    }

    private void DrawPlaceholderWaveform(double width, double height)
    {
        if (width <= 0 || height <= 0) return;

        var centerY = height / 2;
        var placeholderBrush = new SolidColorBrush(Color.FromArgb(60, WaveformColor.R, WaveformColor.G, WaveformColor.B));

        // Draw a simple placeholder pattern
        var points = new PointCollection();
        int numPoints = Math.Max(10, (int)(width / 3));

        for (int i = 0; i <= numPoints; i++)
        {
            double x = i * width / numPoints;
            double amplitude = Math.Sin(i * 0.5) * Math.Sin(i * 0.15) * centerY * 0.6;
            points.Add(new Point(x, centerY - amplitude));
        }

        for (int i = numPoints; i >= 0; i--)
        {
            double x = i * width / numPoints;
            double amplitude = Math.Sin(i * 0.5) * Math.Sin(i * 0.15) * centerY * 0.6;
            points.Add(new Point(x, centerY + amplitude));
        }

        var polygon = new Polygon
        {
            Points = points,
            Fill = placeholderBrush
        };
        WaveformCanvas.Children.Add(polygon);
    }
}
