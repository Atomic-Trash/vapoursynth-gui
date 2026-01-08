using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VapourSynthPortable.Controls;

public partial class CurvesControl : UserControl
{
    private CurveChannel _currentChannel = CurveChannel.RGB;
    private readonly Dictionary<CurveChannel, List<Point>> _curvePoints = new();
    private Point? _draggedPoint;
    private int _draggedPointIndex = -1;
    private const double PointRadius = 6;

    public event EventHandler<CurveChangedEventArgs>? CurveChanged;

    public CurvesControl()
    {
        InitializeComponent();

        // Initialize curve points for each channel (default is a straight diagonal line)
        foreach (CurveChannel channel in Enum.GetValues<CurveChannel>())
        {
            _curvePoints[channel] = [new Point(0, 0), new Point(1, 1)];
        }

        Loaded += (s, e) => RedrawAll();
    }

    private void Channel_Changed(object sender, RoutedEventArgs e)
    {
        // Null check for initialization
        if (RgbRadio == null) return;

        if (RgbRadio.IsChecked == true) _currentChannel = CurveChannel.RGB;
        else if (RedRadio.IsChecked == true) _currentChannel = CurveChannel.Red;
        else if (GreenRadio.IsChecked == true) _currentChannel = CurveChannel.Green;
        else if (BlueRadio.IsChecked == true) _currentChannel = CurveChannel.Blue;

        RedrawCurve();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _curvePoints[_currentChannel] = [new Point(0, 0), new Point(1, 1)];
        RedrawCurve();
        OnCurveChanged();
    }

    private void CurvesCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawAll();
    }

    private void RedrawAll()
    {
        DrawGrid();
        DrawHistogram();
        RedrawCurve();
    }

    private void DrawGrid()
    {
        GridCanvas.Children.Clear();

        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var lineBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));

        // Draw grid lines (quarters)
        for (int i = 1; i < 4; i++)
        {
            double pos = i / 4.0;

            // Vertical line
            GridCanvas.Children.Add(new Line
            {
                X1 = pos * width, Y1 = 0,
                X2 = pos * width, Y2 = height,
                Stroke = lineBrush, StrokeThickness = 1
            });

            // Horizontal line
            GridCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = pos * height,
                X2 = width, Y2 = pos * height,
                Stroke = lineBrush, StrokeThickness = 1
            });
        }

        // Draw diagonal reference line
        GridCanvas.Children.Add(new Line
        {
            X1 = 0, Y1 = height,
            X2 = width, Y2 = 0,
            Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = [4, 4]
        });
    }

    private void DrawHistogram()
    {
        HistogramCanvas.Children.Clear();

        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        // Generate sample histogram data for demonstration
        var random = new Random(42);
        double[] histogram = new double[256];

        // Create a somewhat realistic distribution
        for (int i = 0; i < 256; i++)
        {
            double x = i / 255.0;
            // Multi-modal distribution
            histogram[i] = Math.Exp(-Math.Pow((x - 0.2) * 5, 2)) * 0.5 +
                          Math.Exp(-Math.Pow((x - 0.5) * 4, 2)) * 0.8 +
                          Math.Exp(-Math.Pow((x - 0.8) * 6, 2)) * 0.3 +
                          random.NextDouble() * 0.1;
        }

        double maxVal = histogram.Max();
        var points = new PointCollection();

        for (int i = 0; i < 256; i++)
        {
            double x = i / 255.0 * width;
            double y = height - (histogram[i] / maxVal * height * 0.8);
            points.Add(new Point(x, y));
        }

        // Close the polygon
        points.Add(new Point(width, height));
        points.Add(new Point(0, height));

        var polygon = new Polygon
        {
            Points = points,
            Fill = new SolidColorBrush(Color.FromArgb(30, 128, 128, 128))
        };
        HistogramCanvas.Children.Add(polygon);
    }

    private void RedrawCurve()
    {
        // Null check for initialization
        if (CurvesCanvas == null) return;

        CurvesCanvas.Children.Clear();

        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var points = _curvePoints[_currentChannel];
        var curveColor = GetCurveColor(_currentChannel);

        // Draw interpolated curve
        var curvePoints = new PointCollection();
        var sortedPoints = points.OrderBy(p => p.X).ToList();

        // Ensure we have endpoints
        if (sortedPoints.Count == 0 || sortedPoints[0].X > 0)
            sortedPoints.Insert(0, new Point(0, 0));
        if (sortedPoints[^1].X < 1)
            sortedPoints.Add(new Point(1, 1));

        // Generate smooth curve using Catmull-Rom spline
        for (int i = 0; i <= 255; i++)
        {
            double t = i / 255.0;
            double y = EvaluateCurve(sortedPoints, t);
            y = Math.Clamp(y, 0, 1);

            curvePoints.Add(new Point(t * width, height - y * height));
        }

        var curve = new Polyline
        {
            Points = curvePoints,
            Stroke = new SolidColorBrush(curveColor),
            StrokeThickness = 2
        };
        CurvesCanvas.Children.Add(curve);

        // Draw control points
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            double x = point.X * width;
            double y = height - point.Y * height;

            bool isEndpoint = (i == 0 || i == points.Count - 1);

            if (isEndpoint)
            {
                // Draw locked endpoints as squares with lock indicator
                var outerSquare = new Rectangle
                {
                    Width = PointRadius * 2,
                    Height = PointRadius * 2,
                    Stroke = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)),
                    ToolTip = "Endpoint locked (Y-axis only)"
                };
                Canvas.SetLeft(outerSquare, x - PointRadius);
                Canvas.SetTop(outerSquare, y - PointRadius);
                CurvesCanvas.Children.Add(outerSquare);

                // Inner fill
                var innerSquare = new Rectangle
                {
                    Width = PointRadius - 2,
                    Height = PointRadius - 2,
                    Fill = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
                Canvas.SetLeft(innerSquare, x - (PointRadius - 2) / 2);
                Canvas.SetTop(innerSquare, y - (PointRadius - 2) / 2);
                CurvesCanvas.Children.Add(innerSquare);
            }
            else
            {
                // Outer circle (border) for regular points
                var outerCircle = new Ellipse
                {
                    Width = PointRadius * 2 + 2,
                    Height = PointRadius * 2 + 2,
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    ToolTip = "Drag to adjust | Right-click to delete"
                };
                Canvas.SetLeft(outerCircle, x - PointRadius - 1);
                Canvas.SetTop(outerCircle, y - PointRadius - 1);
                CurvesCanvas.Children.Add(outerCircle);

                // Inner circle (fill)
                var innerCircle = new Ellipse
                {
                    Width = PointRadius,
                    Height = PointRadius,
                    Fill = new SolidColorBrush(curveColor)
                };
                Canvas.SetLeft(innerCircle, x - PointRadius / 2);
                Canvas.SetTop(innerCircle, y - PointRadius / 2);
                CurvesCanvas.Children.Add(innerCircle);
            }
        }
    }

    private static Color GetCurveColor(CurveChannel channel)
    {
        return channel switch
        {
            CurveChannel.Red => Color.FromRgb(255, 80, 80),
            CurveChannel.Green => Color.FromRgb(80, 255, 80),
            CurveChannel.Blue => Color.FromRgb(80, 128, 255),
            _ => Color.FromRgb(200, 200, 200)
        };
    }

    private double EvaluateCurve(List<Point> points, double x)
    {
        if (points.Count == 0) return x;
        if (points.Count == 1) return points[0].Y;

        // Find the segment
        int i = 0;
        while (i < points.Count - 1 && points[i + 1].X < x)
            i++;

        if (i >= points.Count - 1)
            return points[^1].Y;

        // Get control points for Catmull-Rom spline
        Point p0 = i > 0 ? points[i - 1] : points[i];
        Point p1 = points[i];
        Point p2 = points[i + 1];
        Point p3 = i + 2 < points.Count ? points[i + 2] : points[i + 1];

        // Calculate t within the segment
        double segmentLength = p2.X - p1.X;
        if (segmentLength <= 0) return p1.Y;

        double t = (x - p1.X) / segmentLength;
        t = Math.Clamp(t, 0, 1);

        // Catmull-Rom spline interpolation
        double t2 = t * t;
        double t3 = t2 * t;

        double y = 0.5 * ((2 * p1.Y) +
                         (-p0.Y + p2.Y) * t +
                         (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 +
                         (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3);

        return y;
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var pos = e.GetPosition(CurvesCanvas);
        var points = _curvePoints[_currentChannel];

        // Check if clicking on an existing point
        for (int i = 0; i < points.Count; i++)
        {
            double px = points[i].X * width;
            double py = height - points[i].Y * height;
            double dist = Math.Sqrt(Math.Pow(pos.X - px, 2) + Math.Pow(pos.Y - py, 2));

            if (dist <= PointRadius + 4)
            {
                _draggedPointIndex = i;
                _draggedPoint = points[i];
                CurvesCanvas.CaptureMouse();
                return;
            }
        }

        // Add new point
        double newX = Math.Clamp(pos.X / width, 0, 1);
        double newY = Math.Clamp(1 - pos.Y / height, 0, 1);

        points.Add(new Point(newX, newY));
        _curvePoints[_currentChannel] = points.OrderBy(p => p.X).ToList();

        // Find the new point's index
        for (int i = 0; i < _curvePoints[_currentChannel].Count; i++)
        {
            if (Math.Abs(_curvePoints[_currentChannel][i].X - newX) < 0.001)
            {
                _draggedPointIndex = i;
                _draggedPoint = _curvePoints[_currentChannel][i];
                break;
            }
        }

        CurvesCanvas.CaptureMouse();
        RedrawCurve();
        OnCurveChanged();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var pos = e.GetPosition(CurvesCanvas);

        // Update input/output display
        double inputVal = Math.Clamp(pos.X / width, 0, 1);
        double outputVal = EvaluateCurve(_curvePoints[_currentChannel].OrderBy(p => p.X).ToList(), inputVal);
        outputVal = Math.Clamp(outputVal, 0, 1);

        InputValueText.Text = $"{(int)(inputVal * 255)}";
        OutputValueText.Text = $"{(int)(outputVal * 255)}";

        // Update cursor based on whether hovering over a deletable point
        if (_draggedPointIndex < 0) // Not dragging
        {
            var points = _curvePoints[_currentChannel];
            bool overDeletablePoint = false;

            // Check middle points (not first or last - those are locked)
            for (int i = 1; i < points.Count - 1; i++)
            {
                double px = points[i].X * width;
                double py = height - points[i].Y * height;
                double dist = Math.Sqrt(Math.Pow(pos.X - px, 2) + Math.Pow(pos.Y - py, 2));

                if (dist <= PointRadius + 4)
                {
                    overDeletablePoint = true;
                    break;
                }
            }

            // Show different cursor when over deletable point (right-click to delete)
            CurvesCanvas.Cursor = overDeletablePoint ? Cursors.Hand : Cursors.Cross;
        }

        if (_draggedPointIndex >= 0 && _draggedPoint.HasValue)
        {
            var points = _curvePoints[_currentChannel];

            double newX = Math.Clamp(pos.X / width, 0, 1);
            double newY = Math.Clamp(1 - pos.Y / height, 0, 1);

            // Don't allow moving endpoint X positions
            if (_draggedPointIndex == 0)
                newX = 0;
            else if (_draggedPointIndex == points.Count - 1)
                newX = 1;

            points[_draggedPointIndex] = new Point(newX, newY);
            _curvePoints[_currentChannel] = points.OrderBy(p => p.X).ToList();

            // Update dragged index after sorting
            for (int i = 0; i < _curvePoints[_currentChannel].Count; i++)
            {
                if (Math.Abs(_curvePoints[_currentChannel][i].X - newX) < 0.001 &&
                    Math.Abs(_curvePoints[_currentChannel][i].Y - newY) < 0.001)
                {
                    _draggedPointIndex = i;
                    break;
                }
            }

            RedrawCurve();
            OnCurveChanged();
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _draggedPointIndex = -1;
        _draggedPoint = null;
        CurvesCanvas.ReleaseMouseCapture();
    }

    private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        double width = CurvesCanvas.ActualWidth;
        double height = CurvesCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        var pos = e.GetPosition(CurvesCanvas);
        var points = _curvePoints[_currentChannel];

        // Find point to remove (don't remove first or last)
        for (int i = 1; i < points.Count - 1; i++)
        {
            double px = points[i].X * width;
            double py = height - points[i].Y * height;
            double dist = Math.Sqrt(Math.Pow(pos.X - px, 2) + Math.Pow(pos.Y - py, 2));

            if (dist <= PointRadius + 4)
            {
                points.RemoveAt(i);
                RedrawCurve();
                OnCurveChanged();
                return;
            }
        }
    }

    private void OnCurveChanged()
    {
        CurveChanged?.Invoke(this, new CurveChangedEventArgs
        {
            Channel = _currentChannel,
            LookupTable = GenerateLookupTable(_currentChannel)
        });
    }

    public byte[] GenerateLookupTable(CurveChannel channel)
    {
        var lut = new byte[256];
        var points = _curvePoints[channel].OrderBy(p => p.X).ToList();

        for (int i = 0; i < 256; i++)
        {
            double x = i / 255.0;
            double y = EvaluateCurve(points, x);
            lut[i] = (byte)Math.Clamp((int)(y * 255), 0, 255);
        }

        return lut;
    }

    public void SetCurvePoints(CurveChannel channel, List<Point> points)
    {
        _curvePoints[channel] = points.OrderBy(p => p.X).ToList();
        if (channel == _currentChannel)
            RedrawCurve();
    }

    public List<Point> GetCurvePoints(CurveChannel channel)
    {
        return [.. _curvePoints[channel]];
    }
}

public enum CurveChannel
{
    RGB,
    Red,
    Green,
    Blue
}

public class CurveChangedEventArgs : EventArgs
{
    public CurveChannel Channel { get; set; }
    public byte[] LookupTable { get; set; } = [];
}
