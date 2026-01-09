using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VapourSynthPortable.Controls;

/// <summary>
/// Audio level meter control with dB scale for monitoring audio levels
/// </summary>
public partial class AudioLevelMeterControl : UserControl
{
    // Standard dB scale markings
    private static readonly (float db, string label)[] DbScale =
    [
        (0, "0"),
        (-3, "-3"),
        (-6, "-6"),
        (-10, "-10"),
        (-20, "-20"),
        (-40, "-40"),
        (-60, "-60")
    ];

    private const float MinDb = -60f;
    private const float MaxDb = 0f;

    public static readonly DependencyProperty LeftLevelProperty =
        DependencyProperty.Register(nameof(LeftLevel), typeof(float), typeof(AudioLevelMeterControl),
            new PropertyMetadata(0f, OnLevelChanged));

    public static readonly DependencyProperty RightLevelProperty =
        DependencyProperty.Register(nameof(RightLevel), typeof(float), typeof(AudioLevelMeterControl),
            new PropertyMetadata(0f, OnLevelChanged));

    public static readonly DependencyProperty LeftPeakProperty =
        DependencyProperty.Register(nameof(LeftPeak), typeof(float), typeof(AudioLevelMeterControl),
            new PropertyMetadata(0f, OnLevelChanged));

    public static readonly DependencyProperty RightPeakProperty =
        DependencyProperty.Register(nameof(RightPeak), typeof(float), typeof(AudioLevelMeterControl),
            new PropertyMetadata(0f, OnLevelChanged));

    public static readonly DependencyProperty ShowPeakHoldProperty =
        DependencyProperty.Register(nameof(ShowPeakHold), typeof(bool), typeof(AudioLevelMeterControl),
            new PropertyMetadata(true, OnLevelChanged));

    public static readonly DependencyProperty MeterColorProperty =
        DependencyProperty.Register(nameof(MeterColor), typeof(Color), typeof(AudioLevelMeterControl),
            new PropertyMetadata(Color.FromRgb(0x4A, 0xCF, 0x6A), OnLevelChanged));

    public static readonly DependencyProperty WarningColorProperty =
        DependencyProperty.Register(nameof(WarningColor), typeof(Color), typeof(AudioLevelMeterControl),
            new PropertyMetadata(Color.FromRgb(0xFF, 0xCC, 0x00), OnLevelChanged));

    public static readonly DependencyProperty ClipColorProperty =
        DependencyProperty.Register(nameof(ClipColor), typeof(Color), typeof(AudioLevelMeterControl),
            new PropertyMetadata(Color.FromRgb(0xFF, 0x44, 0x44), OnLevelChanged));

    public static readonly DependencyProperty IsClippingLeftProperty =
        DependencyProperty.Register(nameof(IsClippingLeft), typeof(bool), typeof(AudioLevelMeterControl),
            new PropertyMetadata(false, OnLevelChanged));

    public static readonly DependencyProperty IsClippingRightProperty =
        DependencyProperty.Register(nameof(IsClippingRight), typeof(bool), typeof(AudioLevelMeterControl),
            new PropertyMetadata(false, OnLevelChanged));

    /// <summary>
    /// Left channel level (0.0 to 1.0)
    /// </summary>
    public float LeftLevel
    {
        get => (float)GetValue(LeftLevelProperty);
        set => SetValue(LeftLevelProperty, value);
    }

    /// <summary>
    /// Right channel level (0.0 to 1.0)
    /// </summary>
    public float RightLevel
    {
        get => (float)GetValue(RightLevelProperty);
        set => SetValue(RightLevelProperty, value);
    }

    /// <summary>
    /// Left channel peak hold level (0.0 to 1.0)
    /// </summary>
    public float LeftPeak
    {
        get => (float)GetValue(LeftPeakProperty);
        set => SetValue(LeftPeakProperty, value);
    }

    /// <summary>
    /// Right channel peak hold level (0.0 to 1.0)
    /// </summary>
    public float RightPeak
    {
        get => (float)GetValue(RightPeakProperty);
        set => SetValue(RightPeakProperty, value);
    }

    /// <summary>
    /// Whether to show peak hold indicators
    /// </summary>
    public bool ShowPeakHold
    {
        get => (bool)GetValue(ShowPeakHoldProperty);
        set => SetValue(ShowPeakHoldProperty, value);
    }

    /// <summary>
    /// Color for normal levels
    /// </summary>
    public Color MeterColor
    {
        get => (Color)GetValue(MeterColorProperty);
        set => SetValue(MeterColorProperty, value);
    }

    /// <summary>
    /// Color for warning levels (-6dB to -3dB)
    /// </summary>
    public Color WarningColor
    {
        get => (Color)GetValue(WarningColorProperty);
        set => SetValue(WarningColorProperty, value);
    }

    /// <summary>
    /// Color for clipping levels (above -3dB)
    /// </summary>
    public Color ClipColor
    {
        get => (Color)GetValue(ClipColorProperty);
        set => SetValue(ClipColorProperty, value);
    }

    /// <summary>
    /// Whether left channel is currently clipping
    /// </summary>
    public bool IsClippingLeft
    {
        get => (bool)GetValue(IsClippingLeftProperty);
        set => SetValue(IsClippingLeftProperty, value);
    }

    /// <summary>
    /// Whether right channel is currently clipping
    /// </summary>
    public bool IsClippingRight
    {
        get => (bool)GetValue(IsClippingRightProperty);
        set => SetValue(IsClippingRightProperty, value);
    }

    public AudioLevelMeterControl()
    {
        InitializeComponent();
        Loaded += (s, e) => DrawMeters();
        SizeChanged += (s, e) => DrawMeters();
    }

    private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioLevelMeterControl meter)
        {
            meter.DrawMeters();
        }
    }

    /// <summary>
    /// Set levels from linear values (0.0 to 1.0+)
    /// </summary>
    public void SetLevels(float left, float right)
    {
        LeftLevel = left;
        RightLevel = right;
        IsClippingLeft = left >= 0.99f;
        IsClippingRight = right >= 0.99f;
    }

    /// <summary>
    /// Set levels from dB values
    /// </summary>
    public void SetLevelsDb(float leftDb, float rightDb)
    {
        LeftLevel = DbToLinear(leftDb);
        RightLevel = DbToLinear(rightDb);
        IsClippingLeft = leftDb >= -0.1f;
        IsClippingRight = rightDb >= -0.1f;
    }

    /// <summary>
    /// Set peak hold values
    /// </summary>
    public void SetPeaks(float leftPeak, float rightPeak)
    {
        LeftPeak = leftPeak;
        RightPeak = rightPeak;
    }

    /// <summary>
    /// Reset peak hold and clipping indicators
    /// </summary>
    public void ResetPeaks()
    {
        LeftPeak = 0;
        RightPeak = 0;
        IsClippingLeft = false;
        IsClippingRight = false;
    }

    private void DrawMeters()
    {
        DrawChannelMeter(LeftMeterCanvas, LeftLevel, LeftPeak, IsClippingLeft);
        DrawChannelMeter(RightMeterCanvas, RightLevel, RightPeak, IsClippingRight);
        DrawScale();
    }

    private void DrawChannelMeter(Canvas canvas, float level, float peak, bool isClipping)
    {
        canvas.Children.Clear();

        double width = canvas.ActualWidth;
        double height = canvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        // Convert level to dB
        float levelDb = LinearToDb(level);
        float peakDb = LinearToDb(peak);

        // Draw gradient meter background (shows full scale)
        DrawMeterGradient(canvas, width, height);

        // Draw level bar
        double levelY = DbToY(levelDb, height);
        if (levelY < height)
        {
            var levelRect = new Rectangle
            {
                Width = width,
                Height = height - levelY,
                Fill = GetLevelBrush(levelDb)
            };
            Canvas.SetTop(levelRect, levelY);
            canvas.Children.Add(levelRect);
        }

        // Draw peak hold indicator
        if (ShowPeakHold && peak > 0.001f)
        {
            double peakY = DbToY(peakDb, height);
            var peakLine = new Rectangle
            {
                Width = width,
                Height = 2,
                Fill = GetLevelBrush(peakDb)
            };
            Canvas.SetTop(peakLine, peakY);
            canvas.Children.Add(peakLine);
        }

        // Draw clipping indicator at top
        if (isClipping)
        {
            var clipIndicator = new Rectangle
            {
                Width = width,
                Height = 4,
                Fill = new SolidColorBrush(ClipColor)
            };
            Canvas.SetTop(clipIndicator, 0);
            canvas.Children.Add(clipIndicator);
        }
    }

    private void DrawMeterGradient(Canvas canvas, double width, double height)
    {
        // Draw faint background gradient showing the scale regions
        var clipRegion = new Rectangle
        {
            Width = width,
            Height = DbToY(-3, height),
            Fill = new SolidColorBrush(Color.FromArgb(30, ClipColor.R, ClipColor.G, ClipColor.B))
        };
        Canvas.SetTop(clipRegion, 0);
        canvas.Children.Add(clipRegion);

        var warnRegion = new Rectangle
        {
            Width = width,
            Height = DbToY(-6, height) - DbToY(-3, height),
            Fill = new SolidColorBrush(Color.FromArgb(20, WarningColor.R, WarningColor.G, WarningColor.B))
        };
        Canvas.SetTop(warnRegion, DbToY(-3, height));
        canvas.Children.Add(warnRegion);
    }

    private Brush GetLevelBrush(float db)
    {
        if (db >= -3)
            return new SolidColorBrush(ClipColor);
        if (db >= -6)
            return new SolidColorBrush(WarningColor);
        return new SolidColorBrush(MeterColor);
    }

    private void DrawScale()
    {
        ScaleCanvas.Children.Clear();

        double height = ScaleCanvas.ActualHeight;
        if (height <= 0) return;

        var tickBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        var labelBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));

        foreach (var (db, label) in DbScale)
        {
            double y = DbToY(db, height);

            // Draw tick mark
            var tick = new Line
            {
                X1 = 0,
                Y1 = y,
                X2 = 4,
                Y2 = y,
                Stroke = tickBrush,
                StrokeThickness = 1
            };
            ScaleCanvas.Children.Add(tick);

            // Draw label
            var text = new TextBlock
            {
                Text = label,
                Foreground = labelBrush,
                FontSize = 8
            };
            Canvas.SetLeft(text, 6);
            Canvas.SetTop(text, y - 5);
            ScaleCanvas.Children.Add(text);
        }
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0.0001f) return MinDb;
        float db = 20f * (float)Math.Log10(linear);
        return Math.Max(MinDb, Math.Min(MaxDb, db));
    }

    private static float DbToLinear(float db)
    {
        if (db <= MinDb) return 0f;
        return (float)Math.Pow(10, db / 20f);
    }

    private static double DbToY(float db, double height)
    {
        // Map dB to Y coordinate (0dB at top, MinDb at bottom)
        float normalizedDb = (db - MinDb) / (MaxDb - MinDb);
        return height * (1 - normalizedDb);
    }
}
