namespace VapourSynthPortable.Models;

public class VideoInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameCount { get; set; }
    public double Fps { get; set; }
    public string Format { get; set; } = "";
    public int BitsPerSample { get; set; }
    public string ColorFamily { get; set; } = "";

    public string Resolution => $"{Width}x{Height}";
    public string FrameInfo => $"{FrameCount} frames @ {Fps:F3} fps";
}
