using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VapourSynthPortable.Controls.Automation;

namespace VapourSynthPortable.Controls;

public partial class ScopesControl : UserControl
{
    protected override AutomationPeer OnCreateAutomationPeer()
        => new ScopesControlAutomationPeer(this);

    private readonly Random _random = new();
    private ScopeMode _currentMode = ScopeMode.Waveform;

    // Simulated frame data (would come from actual video frames)
    private byte[]? _frameData;
    private int _frameWidth;
    private int _frameHeight;

    public ScopesControl()
    {
        InitializeComponent();
        Loaded += ScopesControl_Loaded;
    }

    private void ScopesControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Generate sample data for demonstration
        GenerateSampleData();
        UpdateScopes();
    }

    private void ScopeMode_Changed(object sender, RoutedEventArgs e)
    {
        // Null check for initialization
        if (WaveformRadio == null) return;

        if (WaveformRadio.IsChecked == true)
            _currentMode = ScopeMode.Waveform;
        else if (ParadeRadio.IsChecked == true)
            _currentMode = ScopeMode.Parade;
        else if (VectorRadio.IsChecked == true)
            _currentMode = ScopeMode.Vector;

        UpdateScopeVisibility();
        UpdateScopes();
    }

    private void UpdateScopeVisibility()
    {
        // Null check for initialization
        if (WaveformCanvas == null) return;

        WaveformCanvas.Visibility = _currentMode == ScopeMode.Waveform ? Visibility.Visible : Visibility.Collapsed;
        ParadeCanvas.Visibility = _currentMode == ScopeMode.Parade ? Visibility.Visible : Visibility.Collapsed;
        VectorCanvas.Visibility = _currentMode == ScopeMode.Vector ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScopes();
    }

    public void UpdateFromFrame(byte[] rgbData, int width, int height)
    {
        _frameData = rgbData;
        _frameWidth = width;
        _frameHeight = height;
        UpdateScopes();
    }

    private void GenerateSampleData()
    {
        // Generate sample frame data for demo purposes
        _frameWidth = 256;
        _frameHeight = 144;
        _frameData = new byte[_frameWidth * _frameHeight * 3];

        for (int y = 0; y < _frameHeight; y++)
        {
            for (int x = 0; x < _frameWidth; x++)
            {
                int idx = (y * _frameWidth + x) * 3;

                // Create a gradient with some variation
                double normalX = (double)x / _frameWidth;
                double normalY = (double)y / _frameHeight;

                // Simulate a typical video frame distribution
                int baseR = (int)(normalX * 200 + _random.Next(40));
                int baseG = (int)(normalY * 180 + _random.Next(50));
                int baseB = (int)((normalX + normalY) / 2 * 160 + _random.Next(60));

                _frameData[idx] = (byte)Math.Clamp(baseR, 0, 255);
                _frameData[idx + 1] = (byte)Math.Clamp(baseG, 0, 255);
                _frameData[idx + 2] = (byte)Math.Clamp(baseB, 0, 255);
            }
        }
    }

    private void UpdateScopes()
    {
        if (_frameData == null || _frameWidth == 0 || _frameHeight == 0)
        {
            // Show placeholders when no data
            if (ScopePlaceholder != null) ScopePlaceholder.Visibility = Visibility.Visible;
            if (HistogramPlaceholder != null) HistogramPlaceholder.Visibility = Visibility.Visible;
            return;
        }

        // Hide placeholders when we have data
        if (ScopePlaceholder != null) ScopePlaceholder.Visibility = Visibility.Collapsed;
        if (HistogramPlaceholder != null) HistogramPlaceholder.Visibility = Visibility.Collapsed;

        DrawGraticule();

        switch (_currentMode)
        {
            case ScopeMode.Waveform:
                DrawWaveform();
                break;
            case ScopeMode.Parade:
                DrawParade();
                break;
            case ScopeMode.Vector:
                DrawVectorscope();
                break;
        }

        DrawHistogram();
    }

    private void DrawGraticule()
    {
        GraticuleCanvas.Children.Clear();

        double width = WaveformCanvas.ActualWidth;
        double height = WaveformCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var lineBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        // Draw horizontal lines at 0%, 25%, 50%, 75%, 100%
        double[] levels = [0, 0.25, 0.5, 0.75, 1.0];
        foreach (double level in levels)
        {
            double y = height - (level * height);
            var line = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = width,
                Y2 = y,
                Stroke = lineBrush,
                StrokeThickness = 1,
                StrokeDashArray = [2, 4]
            };
            GraticuleCanvas.Children.Add(line);
        }
    }

    private void DrawWaveform()
    {
        WaveformCanvas.Children.Clear();

        double canvasWidth = WaveformCanvas.ActualWidth;
        double canvasHeight = WaveformCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0 || _frameData == null) return;

        // Create luminance bins for each column
        int numColumns = Math.Min((int)canvasWidth, 256);
        var columnData = new List<byte>[numColumns];
        for (int i = 0; i < numColumns; i++)
            columnData[i] = [];

        // Sample frame data
        for (int y = 0; y < _frameHeight; y++)
        {
            for (int x = 0; x < _frameWidth; x++)
            {
                int idx = (y * _frameWidth + x) * 3;
                byte r = _frameData[idx];
                byte g = _frameData[idx + 1];
                byte b = _frameData[idx + 2];

                // Calculate luminance (BT.709)
                byte luma = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);

                int column = x * numColumns / _frameWidth;
                if (column < numColumns)
                    columnData[column].Add(luma);
            }
        }

        // Draw waveform
        var greenBrush = new SolidColorBrush(Color.FromArgb(180, 0, 255, 0));
        double pointSize = Math.Max(1, canvasWidth / numColumns);

        for (int col = 0; col < numColumns; col++)
        {
            foreach (byte luma in columnData[col])
            {
                double x = col * canvasWidth / numColumns;
                double y = canvasHeight - (luma / 255.0 * canvasHeight);

                var rect = new Rectangle
                {
                    Width = pointSize,
                    Height = 1,
                    Fill = greenBrush
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                WaveformCanvas.Children.Add(rect);
            }
        }
    }

    private void DrawParade()
    {
        ParadeCanvas.Children.Clear();

        double canvasWidth = ParadeCanvas.ActualWidth;
        double canvasHeight = ParadeCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0 || _frameData == null) return;

        double sectionWidth = canvasWidth / 3;
        int numColumns = Math.Min((int)sectionWidth, 85);

        var rData = new List<byte>[numColumns];
        var gData = new List<byte>[numColumns];
        var bData = new List<byte>[numColumns];

        for (int i = 0; i < numColumns; i++)
        {
            rData[i] = [];
            gData[i] = [];
            bData[i] = [];
        }

        // Sample frame data
        for (int y = 0; y < _frameHeight; y++)
        {
            for (int x = 0; x < _frameWidth; x++)
            {
                int idx = (y * _frameWidth + x) * 3;
                int column = x * numColumns / _frameWidth;
                if (column < numColumns)
                {
                    rData[column].Add(_frameData[idx]);
                    gData[column].Add(_frameData[idx + 1]);
                    bData[column].Add(_frameData[idx + 2]);
                }
            }
        }

        // Draw RGB parade
        var brushes = new[]
        {
            new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)),
            new SolidColorBrush(Color.FromArgb(180, 0, 255, 0)),
            new SolidColorBrush(Color.FromArgb(180, 0, 100, 255))
        };

        var dataArrays = new[] { rData, gData, bData };
        double[] offsets = [0, sectionWidth, sectionWidth * 2];

        for (int channel = 0; channel < 3; channel++)
        {
            var data = dataArrays[channel];
            var brush = brushes[channel];
            double xOffset = offsets[channel];

            for (int col = 0; col < numColumns; col++)
            {
                foreach (byte val in data[col])
                {
                    double x = xOffset + col * sectionWidth / numColumns;
                    double y = canvasHeight - (val / 255.0 * canvasHeight);

                    var rect = new Rectangle
                    {
                        Width = Math.Max(1, sectionWidth / numColumns),
                        Height = 1,
                        Fill = brush
                    };
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    ParadeCanvas.Children.Add(rect);
                }
            }
        }

        // Draw separator lines
        var separatorBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
        for (int i = 1; i < 3; i++)
        {
            var line = new Line
            {
                X1 = sectionWidth * i,
                Y1 = 0,
                X2 = sectionWidth * i,
                Y2 = canvasHeight,
                Stroke = separatorBrush,
                StrokeThickness = 1
            };
            ParadeCanvas.Children.Add(line);
        }
    }

    private void DrawVectorscope()
    {
        VectorCanvas.Children.Clear();

        double canvasWidth = VectorCanvas.ActualWidth;
        double canvasHeight = VectorCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0 || _frameData == null) return;

        double centerX = canvasWidth / 2;
        double centerY = canvasHeight / 2;
        double radius = Math.Min(centerX, centerY) - 10;

        // Draw graticule circle
        var graticule = new Ellipse
        {
            Width = radius * 2,
            Height = radius * 2,
            Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            StrokeThickness = 1
        };
        Canvas.SetLeft(graticule, centerX - radius);
        Canvas.SetTop(graticule, centerY - radius);
        VectorCanvas.Children.Add(graticule);

        // Draw color target boxes (skin tone line, primary/secondary colors)
        DrawColorTargets(centerX, centerY, radius);

        // Draw crosshairs
        var crossBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
        VectorCanvas.Children.Add(new Line
        {
            X1 = centerX - radius, Y1 = centerY,
            X2 = centerX + radius, Y2 = centerY,
            Stroke = crossBrush, StrokeThickness = 1
        });
        VectorCanvas.Children.Add(new Line
        {
            X1 = centerX, Y1 = centerY - radius,
            X2 = centerX, Y2 = centerY + radius,
            Stroke = crossBrush, StrokeThickness = 1
        });

        // Plot chrominance values
        int sampleStep = Math.Max(1, (_frameWidth * _frameHeight) / 5000);
        var pointBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));

        for (int i = 0; i < _frameData.Length / 3; i += sampleStep)
        {
            int idx = i * 3;
            if (idx + 2 >= _frameData.Length) break;

            byte r = _frameData[idx];
            byte g = _frameData[idx + 1];
            byte b = _frameData[idx + 2];

            // Convert RGB to YCbCr
            double y = 0.299 * r + 0.587 * g + 0.114 * b;
            double cb = 128 - 0.169 * r - 0.331 * g + 0.5 * b;
            double cr = 128 + 0.5 * r - 0.419 * g - 0.081 * b;

            // Normalize to vectorscope coordinates
            double normalCb = (cb - 128) / 128;
            double normalCr = (cr - 128) / 128;

            double x = centerX + normalCb * radius;
            double plotY = centerY - normalCr * radius;

            var dot = new Ellipse
            {
                Width = 2,
                Height = 2,
                Fill = pointBrush
            };
            Canvas.SetLeft(dot, x - 1);
            Canvas.SetTop(dot, plotY - 1);
            VectorCanvas.Children.Add(dot);
        }
    }

    private void DrawColorTargets(double centerX, double centerY, double radius)
    {
        // Standard vectorscope color targets at 75%
        var targets = new[]
        {
            (Name: "R", Angle: -76.0, Sat: 0.63, Color: Colors.Red),
            (Name: "Y", Angle: -167.0, Sat: 0.45, Color: Colors.Yellow),
            (Name: "G", Angle: 104.0, Sat: 0.59, Color: Colors.Green),
            (Name: "C", Angle: 76.0, Sat: 0.63, Color: Colors.Cyan),
            (Name: "B", Angle: 13.0, Sat: 0.45, Color: Colors.Blue),
            (Name: "M", Angle: -76.0 + 180, Sat: 0.59, Color: Colors.Magenta)
        };

        foreach (var target in targets)
        {
            double rad = target.Angle * Math.PI / 180;
            double x = centerX + Math.Cos(rad) * radius * target.Sat;
            double y = centerY - Math.Sin(rad) * radius * target.Sat;

            // Draw small target box
            var box = new Rectangle
            {
                Width = 12,
                Height = 12,
                Stroke = new SolidColorBrush(Color.FromArgb(100, target.Color.R, target.Color.G, target.Color.B)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(box, x - 6);
            Canvas.SetTop(box, y - 6);
            VectorCanvas.Children.Add(box);
        }

        // Skin tone line (approximately 123 degrees)
        double skinAngle = 123 * Math.PI / 180;
        var skinLine = new Line
        {
            X1 = centerX,
            Y1 = centerY,
            X2 = centerX + Math.Cos(skinAngle) * radius,
            Y2 = centerY - Math.Sin(skinAngle) * radius,
            Stroke = new SolidColorBrush(Color.FromArgb(60, 255, 200, 150)),
            StrokeThickness = 2,
            StrokeDashArray = [4, 2]
        };
        VectorCanvas.Children.Add(skinLine);
    }

    private void DrawHistogram()
    {
        HistogramCanvas.Children.Clear();

        double canvasWidth = HistogramCanvas.ActualWidth;
        double canvasHeight = HistogramCanvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0 || _frameData == null) return;

        // Calculate histogram bins
        int[] rHist = new int[256];
        int[] gHist = new int[256];
        int[] bHist = new int[256];
        int[] lumaHist = new int[256];

        int pixelCount = _frameData.Length / 3;
        for (int i = 0; i < pixelCount; i++)
        {
            int idx = i * 3;
            byte r = _frameData[idx];
            byte g = _frameData[idx + 1];
            byte b = _frameData[idx + 2];
            byte luma = (byte)(0.2126 * r + 0.7152 * g + 0.0722 * b);

            rHist[r]++;
            gHist[g]++;
            bHist[b]++;
            lumaHist[luma]++;
        }

        // Find max for normalization
        int maxVal = 1;
        for (int i = 0; i < 256; i++)
        {
            maxVal = Math.Max(maxVal, rHist[i]);
            maxVal = Math.Max(maxVal, gHist[i]);
            maxVal = Math.Max(maxVal, bHist[i]);
        }

        double barWidth = canvasWidth / 256;

        // Draw RGB histograms with transparency
        var histData = new[]
        {
            (Hist: rHist, Brush: new SolidColorBrush(Color.FromArgb(100, 255, 0, 0))),
            (Hist: gHist, Brush: new SolidColorBrush(Color.FromArgb(100, 0, 255, 0))),
            (Hist: bHist, Brush: new SolidColorBrush(Color.FromArgb(100, 0, 100, 255)))
        };

        foreach (var (hist, brush) in histData)
        {
            var points = new PointCollection { new Point(0, canvasHeight) };

            for (int i = 0; i < 256; i++)
            {
                double x = i * barWidth;
                double height = (hist[i] / (double)maxVal) * (canvasHeight - 10);
                points.Add(new Point(x, canvasHeight - height));
            }

            points.Add(new Point(canvasWidth, canvasHeight));

            var polygon = new Polygon
            {
                Points = points,
                Fill = brush
            };
            HistogramCanvas.Children.Add(polygon);
        }

        // Draw luminance histogram outline
        var lumaPoints = new PointCollection();
        int lumaMax = lumaHist.Max();
        for (int i = 0; i < 256; i++)
        {
            double x = i * barWidth;
            double height = (lumaHist[i] / (double)lumaMax) * (canvasHeight - 10);
            lumaPoints.Add(new Point(x, canvasHeight - height));
        }

        var lumaLine = new Polyline
        {
            Points = lumaPoints,
            Stroke = new SolidColorBrush(Color.FromArgb(150, 200, 200, 200)),
            StrokeThickness = 1
        };
        HistogramCanvas.Children.Add(lumaLine);
    }
}

public enum ScopeMode
{
    Waveform,
    Parade,
    Vector
}
