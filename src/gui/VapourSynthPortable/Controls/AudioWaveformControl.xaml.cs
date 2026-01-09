using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VapourSynthPortable.Controls;

public partial class AudioWaveformControl : UserControl
{
    private float[]? _waveformData;
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
        Loaded += (s, e) => DrawWaveform();
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
            GenerateSampleWaveform();
            DrawWaveform();
            return;
        }

        // Try to load waveform from audio file
        try
        {
            _waveformData = await ExtractWaveformAsync(SourcePath);
        }
        catch
        {
            // Fall back to sample data
            GenerateSampleWaveform();
        }

        DrawWaveform();
    }

    private void GenerateSampleWaveform()
    {
        // Generate realistic-looking sample waveform for demonstration
        const int sampleCount = 1000;
        _waveformData = new float[sampleCount];

        // Create multiple frequency components for realistic look
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleCount;

            // Base wave with multiple harmonics
            double wave = Math.Sin(t * 50 * Math.PI) * 0.3 +
                         Math.Sin(t * 120 * Math.PI) * 0.2 +
                         Math.Sin(t * 200 * Math.PI) * 0.1;

            // Add envelope
            double envelope = Math.Sin(t * 5 * Math.PI) * 0.5 + 0.5;
            envelope *= Math.Sin(t * 12 * Math.PI) * 0.3 + 0.7;

            // Add some noise
            double noise = (_random.NextDouble() - 0.5) * 0.15;

            _waveformData[i] = (float)Math.Clamp((wave * envelope + noise), -1, 1);
        }
    }

    private static async Task<float[]> ExtractWaveformAsync(string filePath)
    {
        // This would use FFmpeg to extract peak data
        // For now, return simulated data based on file hash
        await Task.Delay(10);

        var hash = filePath.GetHashCode();
        var random = new Random(hash);
        const int sampleCount = 1000;
        var data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleCount;
            double wave = Math.Sin(t * (40 + hash % 30) * Math.PI) * 0.4 +
                         Math.Sin(t * (100 + hash % 50) * Math.PI) * 0.3;
            double envelope = Math.Sin(t * (4 + hash % 5) * Math.PI) * 0.4 + 0.6;
            double noise = (random.NextDouble() - 0.5) * 0.2;
            data[i] = (float)Math.Clamp((wave * envelope + noise), -1, 1);
        }

        return data;
    }

    public void SetWaveformData(float[] data)
    {
        _waveformData = data;
        DrawWaveform();
    }

    private void DrawWaveform()
    {
        WaveformCanvas.Children.Clear();

        double width = WaveformCanvas.ActualWidth;
        double height = WaveformCanvas.ActualHeight;

        if (width <= 0 || height <= 0 || _waveformData == null || _waveformData.Length == 0)
        {
            // Draw placeholder
            DrawPlaceholderWaveform(width, height);
            return;
        }

        // Calculate visible range
        int totalSamples = _waveformData.Length;
        int startSample = (int)(InPoint * totalSamples);
        int endSample = (int)(OutPoint * totalSamples);
        int visibleSamples = endSample - startSample;

        if (visibleSamples <= 0) return;

        // Determine how many samples to show per pixel
        double samplesPerPixel = visibleSamples / width;
        int numBars = Math.Max(1, (int)width);

        var waveformBrush = new SolidColorBrush(WaveformColor);
        var waveformDarkBrush = new SolidColorBrush(Color.FromArgb(100, WaveformColor.R, WaveformColor.G, WaveformColor.B));

        double centerY = height / 2;
        double barWidth = Math.Max(1, width / numBars);

        // Draw waveform bars
        for (int i = 0; i < numBars; i++)
        {
            int sampleStart = startSample + (int)(i * samplesPerPixel);
            int sampleEnd = Math.Min(totalSamples - 1, startSample + (int)((i + 1) * samplesPerPixel));

            if (sampleStart >= totalSamples) break;

            // Find min and max in this range
            float min = 0, max = 0;
            float rms = 0;
            int count = 0;

            for (int j = sampleStart; j <= sampleEnd && j < totalSamples; j++)
            {
                float val = _waveformData[j];
                min = Math.Min(min, val);
                max = Math.Max(max, val);
                rms += val * val;
                count++;
            }

            if (count > 0)
            {
                rms = (float)Math.Sqrt(rms / count);
            }

            double x = i * barWidth;

            // Draw peak bar (lighter)
            double peakHeight = (max - min) * centerY;
            var peakBar = new Rectangle
            {
                Width = Math.Max(1, barWidth - 0.5),
                Height = Math.Max(1, peakHeight),
                Fill = waveformDarkBrush
            };
            Canvas.SetLeft(peakBar, x);
            Canvas.SetTop(peakBar, centerY - peakHeight / 2);
            WaveformCanvas.Children.Add(peakBar);

            // Draw RMS bar (brighter)
            double rmsHeight = rms * centerY * 1.5;
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
