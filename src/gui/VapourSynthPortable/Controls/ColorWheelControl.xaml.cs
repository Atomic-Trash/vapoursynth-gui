using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VapourSynthPortable.Controls;

public partial class ColorWheelControl : UserControl
{
    private bool _isDragging;
    private const double WheelRadius = 60;
    private const double IndicatorRadius = 7;

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ColorWheelControl),
            new PropertyMetadata("Lift", OnTitleChanged));

    public static readonly DependencyProperty ColorXProperty =
        DependencyProperty.Register(nameof(ColorX), typeof(double), typeof(ColorWheelControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public static readonly DependencyProperty ColorYProperty =
        DependencyProperty.Register(nameof(ColorY), typeof(double), typeof(ColorWheelControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public static readonly DependencyProperty MasterProperty =
        DependencyProperty.Register(nameof(Master), typeof(double), typeof(ColorWheelControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnMasterChanged));

    public static readonly DependencyProperty WheelColorProperty =
        DependencyProperty.Register(nameof(WheelColor), typeof(Color), typeof(ColorWheelControl),
            new PropertyMetadata(Colors.Gray, OnWheelColorChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double ColorX
    {
        get => (double)GetValue(ColorXProperty);
        set => SetValue(ColorXProperty, value);
    }

    public double ColorY
    {
        get => (double)GetValue(ColorYProperty);
        set => SetValue(ColorYProperty, value);
    }

    public double Master
    {
        get => (double)GetValue(MasterProperty);
        set => SetValue(MasterProperty, value);
    }

    public Color WheelColor
    {
        get => (Color)GetValue(WheelColorProperty);
        set => SetValue(WheelColorProperty, value);
    }

    public event EventHandler? ColorChanged;

    public ColorWheelControl()
    {
        InitializeComponent();
        Loaded += ColorWheelControl_Loaded;
    }

    private void ColorWheelControl_Loaded(object sender, RoutedEventArgs e)
    {
        CreateColorWheelGradient();
        UpdateIndicatorPosition();
    }

    private void CreateColorWheelGradient()
    {
        // Create a color wheel bitmap
        int size = 116;
        var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[size * size * 4];

        double centerX = size / 2.0;
        double centerY = size / 2.0;
        double radius = size / 2.0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - centerX;
                double dy = y - centerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                int index = (y * size + x) * 4;

                if (distance <= radius)
                {
                    // Calculate hue from angle
                    double angle = Math.Atan2(dy, dx);
                    double hue = (angle + Math.PI) / (2 * Math.PI); // 0-1

                    // Saturation based on distance from center
                    double saturation = Math.Min(1.0, distance / radius);

                    // Convert HSV to RGB (value = 0.85 for better visibility)
                    var color = HsvToRgb(hue * 360, saturation, 0.85);

                    // Fade out at edges
                    double alpha = distance > radius - 2 ? Math.Max(0, (radius - distance) / 2) : 1.0;

                    pixels[index + 0] = (byte)(color.B * alpha); // B
                    pixels[index + 1] = (byte)(color.G * alpha); // G
                    pixels[index + 2] = (byte)(color.R * alpha); // R
                    pixels[index + 3] = (byte)(255 * alpha * 0.85); // A (85% opacity for better visibility)
                }
                else
                {
                    pixels[index + 0] = 0;
                    pixels[index + 1] = 0;
                    pixels[index + 2] = 0;
                    pixels[index + 3] = 0;
                }
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, size * 4, 0);
        ColorOverlay.Fill = new ImageBrush(bitmap);
    }

    private static Color HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r, g, b;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255));
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorWheelControl control)
        {
            control.TitleText.Text = e.NewValue?.ToString() ?? "";
        }
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorWheelControl control)
        {
            control.UpdateIndicatorPosition();
            control.UpdateWheelColor();
            control.ColorChanged?.Invoke(control, EventArgs.Empty);
        }
    }

    private static void OnMasterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorWheelControl control)
        {
            control.MasterSlider.Value = (double)e.NewValue;
            control.MasterValueText.Text = ((double)e.NewValue).ToString("F2");
            control.ColorChanged?.Invoke(control, EventArgs.Empty);
        }
    }

    private static void OnWheelColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Could update visual appearance based on wheel type
    }

    private void UpdateIndicatorPosition()
    {
        // Convert -1 to 1 range to canvas position
        double x = WheelRadius + (ColorX * (WheelRadius - 5)) - IndicatorRadius;
        double y = WheelRadius + (ColorY * (WheelRadius - 5)) - IndicatorRadius;

        // Clamp to wheel bounds
        double dx = x + IndicatorRadius - WheelRadius;
        double dy = y + IndicatorRadius - WheelRadius;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist > WheelRadius - 5)
        {
            double scale = (WheelRadius - 5) / dist;
            x = WheelRadius + dx * scale - IndicatorRadius;
            y = WheelRadius + dy * scale - IndicatorRadius;
        }

        Canvas.SetLeft(PositionIndicator, x);
        Canvas.SetTop(PositionIndicator, y);
    }

    private void UpdateWheelColor()
    {
        // Calculate color from position
        double dist = Math.Sqrt(ColorX * ColorX + ColorY * ColorY);
        double angle = Math.Atan2(ColorY, ColorX);
        double hue = ((angle + Math.PI) / (2 * Math.PI)) * 360;

        var color = HsvToRgb(hue, Math.Min(1, dist), 0.9);
        PositionIndicator.Fill = new SolidColorBrush(dist > 0.05 ? color : Colors.White);
    }

    private void WheelCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        WheelCanvas.CaptureMouse();
        UpdateColorFromMouse(e.GetPosition(WheelCanvas));
    }

    private void WheelCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        WheelCanvas.ReleaseMouseCapture();
    }

    private void WheelCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            UpdateColorFromMouse(e.GetPosition(WheelCanvas));
        }
    }

    private void WheelCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        // Keep dragging even if mouse leaves, will release on mouse up
    }

    private void UpdateColorFromMouse(Point position)
    {
        // Convert canvas position to -1 to 1 range
        double dx = position.X - WheelRadius;
        double dy = position.Y - WheelRadius;

        // Normalize to wheel radius
        double maxRadius = WheelRadius - 5;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist > maxRadius)
        {
            dx = dx / dist * maxRadius;
            dy = dy / dist * maxRadius;
        }

        ColorX = dx / maxRadius;
        ColorY = dy / maxRadius;
    }

    private void MasterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Master = e.NewValue;
        MasterValueText.Text = e.NewValue.ToString("F2");
    }

    public void Reset()
    {
        ColorX = 0;
        ColorY = 0;
        Master = 0;
    }

    // Get the RGB offset values for color grading
    public (double R, double G, double B) GetRgbOffset()
    {
        // Convert wheel position to RGB offset
        // Using a simple model where X affects R-C (red-cyan) and Y affects B-Y (blue-yellow)
        double angle = Math.Atan2(ColorY, ColorX);
        double magnitude = Math.Sqrt(ColorX * ColorX + ColorY * ColorY);

        // Map angle to RGB
        double r = Math.Cos(angle) * magnitude;
        double g = Math.Cos(angle - 2.094) * magnitude; // 120 degrees
        double b = Math.Cos(angle + 2.094) * magnitude; // -120 degrees

        // Add master offset
        r += Master;
        g += Master;
        b += Master;

        return (r, g, b);
    }
}
