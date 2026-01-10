using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using VapourSynthPortable.Services;

namespace VapourSynthPortable.Controls;

public partial class AudioWaveformControl : UserControl
{
    private WaveformData? _waveformData;
    private StereoWaveformData? _stereoData;
    private readonly AudioWaveformService _waveformService;
    private readonly ILogger<AudioWaveformControl> _logger;
    private CancellationTokenSource? _loadCts;

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

    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(nameof(DisplayMode), typeof(WaveformDisplayMode), typeof(AudioWaveformControl),
            new PropertyMetadata(WaveformDisplayMode.Mono, OnPropertyChanged));

    public static readonly DependencyProperty ShowClippingProperty =
        DependencyProperty.Register(nameof(ShowClipping), typeof(bool), typeof(AudioWaveformControl),
            new PropertyMetadata(true, OnPropertyChanged));

    public static readonly DependencyProperty ClippingColorProperty =
        DependencyProperty.Register(nameof(ClippingColor), typeof(Color), typeof(AudioWaveformControl),
            new PropertyMetadata(Color.FromRgb(0xFF, 0x44, 0x44), OnPropertyChanged));

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

    public WaveformDisplayMode DisplayMode
    {
        get => (WaveformDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public bool ShowClipping
    {
        get => (bool)GetValue(ShowClippingProperty);
        set => SetValue(ShowClippingProperty, value);
    }

    public Color ClippingColor
    {
        get => (Color)GetValue(ClippingColorProperty);
        set => SetValue(ClippingColorProperty, value);
    }

    /// <summary>
    /// Gets the current peak level in dB
    /// </summary>
    public float PeakLevelDb => _stereoData?.MaxPeakDb ?? _waveformData?.Samples.Max(s => s.PeakDb) ?? -96f;

    /// <summary>
    /// Gets whether the audio has clipping
    /// </summary>
    public bool HasClipping => _stereoData?.ClippingCount > 0 ||
                               (_waveformData?.Samples.Any(s => s.IsClipping) ?? false);

    public AudioWaveformControl()
    {
        InitializeComponent();
        _waveformService = new AudioWaveformService();
        _logger = LoggingService.GetLogger<AudioWaveformControl>();
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
            _stereoData = null;
            DrawWaveform();
            return;
        }

        // Cancel any pending load
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            if (DisplayMode == WaveformDisplayMode.Stereo)
            {
                // Extract stereo waveform
                _stereoData = await _waveformService.ExtractStereoWaveformAsync(
                    SourcePath,
                    samplesPerSecond: 100,
                    ct: _loadCts.Token);
                _waveformData = null;
            }
            else
            {
                // Extract mono waveform
                _waveformData = await _waveformService.ExtractWaveformAsync(
                    SourcePath,
                    samplesPerSecond: 100,
                    ct: _loadCts.Token);
                _stereoData = null;
            }

            DrawWaveform();
        }
        catch (OperationCanceledException)
        {
            // Ignore - cancelled due to new source
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load waveform from {SourcePath}", SourcePath);
            _waveformData = null;
            _stereoData = null;
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

        // Check if we have data
        bool hasStereoData = _stereoData != null && _stereoData.LeftChannel.Length > 0;
        bool hasMonoData = _waveformData != null && _waveformData.Samples.Length > 0;

        if (width <= 0 || height <= 0 || (!hasStereoData && !hasMonoData))
        {
            DrawPlaceholderWaveform(width, height);
            return;
        }

        if (DisplayMode == WaveformDisplayMode.Stereo && hasStereoData)
        {
            DrawStereoWaveform(width, height);
        }
        else
        {
            DrawMonoWaveform(width, height);
        }
    }

    private void DrawMonoWaveform(double width, double height)
    {
        WaveformSample[] samples;

        if (_stereoData != null)
        {
            // Use mono mix from stereo data
            double startTime = InPoint * _stereoData.Duration;
            double endTime = OutPoint * _stereoData.Duration;
            int numBars = Math.Max(1, (int)width);
            var (left, right) = _stereoData.GetResampledForWidth(numBars, startTime, endTime);

            // Mix to mono
            samples = new WaveformSample[left.Length];
            for (int i = 0; i < left.Length; i++)
            {
                samples[i] = new WaveformSample
                {
                    Min = (left[i].Min + right[i].Min) / 2f,
                    Max = (left[i].Max + right[i].Max) / 2f,
                    Rms = (float)Math.Sqrt((left[i].Rms * left[i].Rms + right[i].Rms * right[i].Rms) / 2f)
                };
            }
        }
        else if (_waveformData != null)
        {
            double startTime = InPoint * _waveformData.Duration;
            double endTime = OutPoint * _waveformData.Duration;
            int numBars = Math.Max(1, (int)width);
            samples = _waveformData.GetResampledForWidth(numBars, startTime, endTime);
        }
        else
        {
            return;
        }

        if (samples.Length == 0) return;

        var waveformBrush = new SolidColorBrush(WaveformColor);
        var waveformDarkBrush = new SolidColorBrush(Color.FromArgb(100, WaveformColor.R, WaveformColor.G, WaveformColor.B));
        var clippingBrush = new SolidColorBrush(ClippingColor);

        double centerY = height / 2;
        double barWidth = Math.Max(1, width / samples.Length);

        for (int i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            double x = i * barWidth;
            bool isClipping = ShowClipping && sample.IsClipping;

            // Draw peak bar
            double peakHeight = sample.PeakToPeak * centerY;
            if (peakHeight > 0.5)
            {
                var peakBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, peakHeight),
                    Fill = isClipping ? clippingBrush : waveformDarkBrush
                };
                Canvas.SetLeft(peakBar, x);
                Canvas.SetTop(peakBar, centerY - peakHeight / 2);
                WaveformCanvas.Children.Add(peakBar);
            }

            // Draw RMS bar
            double rmsHeight = sample.Rms * centerY * 2;
            if (rmsHeight > 0.5)
            {
                var rmsBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, rmsHeight),
                    Fill = isClipping ? clippingBrush : waveformBrush
                };
                Canvas.SetLeft(rmsBar, x);
                Canvas.SetTop(rmsBar, centerY - rmsHeight / 2);
                WaveformCanvas.Children.Add(rmsBar);
            }
        }

        // Draw center line
        DrawCenterLine(width, height / 2);
    }

    private void DrawStereoWaveform(double width, double height)
    {
        if (_stereoData == null) return;

        double startTime = InPoint * _stereoData.Duration;
        double endTime = OutPoint * _stereoData.Duration;
        int numBars = Math.Max(1, (int)width);

        var (leftSamples, rightSamples) = _stereoData.GetResampledForWidth(numBars, startTime, endTime);

        if (leftSamples.Length == 0) return;

        var waveformBrush = new SolidColorBrush(WaveformColor);
        var waveformDarkBrush = new SolidColorBrush(Color.FromArgb(100, WaveformColor.R, WaveformColor.G, WaveformColor.B));
        var clippingBrush = new SolidColorBrush(ClippingColor);

        double channelHeight = height / 2;
        double leftCenterY = channelHeight / 2;
        double rightCenterY = channelHeight + channelHeight / 2;
        double barWidth = Math.Max(1, width / leftSamples.Length);

        // Draw left channel (top half)
        for (int i = 0; i < leftSamples.Length; i++)
        {
            var sample = leftSamples[i];
            double x = i * barWidth;
            bool isClipping = ShowClipping && sample.IsClipping;

            double peakHeight = sample.PeakToPeak * leftCenterY;
            if (peakHeight > 0.5)
            {
                var peakBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, peakHeight),
                    Fill = isClipping ? clippingBrush : waveformDarkBrush
                };
                Canvas.SetLeft(peakBar, x);
                Canvas.SetTop(peakBar, leftCenterY - peakHeight / 2);
                WaveformCanvas.Children.Add(peakBar);
            }

            double rmsHeight = sample.Rms * leftCenterY * 2;
            if (rmsHeight > 0.5)
            {
                var rmsBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, rmsHeight),
                    Fill = isClipping ? clippingBrush : waveformBrush
                };
                Canvas.SetLeft(rmsBar, x);
                Canvas.SetTop(rmsBar, leftCenterY - rmsHeight / 2);
                WaveformCanvas.Children.Add(rmsBar);
            }
        }

        // Draw right channel (bottom half)
        for (int i = 0; i < rightSamples.Length; i++)
        {
            var sample = rightSamples[i];
            double x = i * barWidth;
            bool isClipping = ShowClipping && sample.IsClipping;

            double peakHeight = sample.PeakToPeak * (channelHeight / 2);
            if (peakHeight > 0.5)
            {
                var peakBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, peakHeight),
                    Fill = isClipping ? clippingBrush : waveformDarkBrush
                };
                Canvas.SetLeft(peakBar, x);
                Canvas.SetTop(peakBar, rightCenterY - peakHeight / 2);
                WaveformCanvas.Children.Add(peakBar);
            }

            double rmsHeight = sample.Rms * (channelHeight / 2) * 2;
            if (rmsHeight > 0.5)
            {
                var rmsBar = new Rectangle
                {
                    Width = Math.Max(1, barWidth - 0.5),
                    Height = Math.Max(1, rmsHeight),
                    Fill = isClipping ? clippingBrush : waveformBrush
                };
                Canvas.SetLeft(rmsBar, x);
                Canvas.SetTop(rmsBar, rightCenterY - rmsHeight / 2);
                WaveformCanvas.Children.Add(rmsBar);
            }
        }

        // Draw channel divider and center lines
        DrawCenterLine(width, leftCenterY);
        DrawCenterLine(width, rightCenterY);

        // Draw channel separator
        var separator = new Line
        {
            X1 = 0,
            Y1 = channelHeight,
            X2 = width,
            Y2 = channelHeight,
            Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            StrokeThickness = 1
        };
        WaveformCanvas.Children.Add(separator);

        // Draw channel labels
        var leftLabel = new System.Windows.Controls.TextBlock
        {
            Text = "L",
            Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            FontSize = 9
        };
        Canvas.SetLeft(leftLabel, 2);
        Canvas.SetTop(leftLabel, 2);
        WaveformCanvas.Children.Add(leftLabel);

        var rightLabel = new System.Windows.Controls.TextBlock
        {
            Text = "R",
            Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            FontSize = 9
        };
        Canvas.SetLeft(rightLabel, 2);
        Canvas.SetTop(rightLabel, channelHeight + 2);
        WaveformCanvas.Children.Add(rightLabel);
    }

    private void DrawCenterLine(double width, double y)
    {
        var centerLine = new Line
        {
            X1 = 0,
            Y1 = y,
            X2 = width,
            Y2 = y,
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
