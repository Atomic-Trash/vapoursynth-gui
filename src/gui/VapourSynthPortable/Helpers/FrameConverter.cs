using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VapourSynthPortable.Helpers;

public static class FrameConverter
{
    /// <summary>
    /// Converts Y4M (YUV4MPEG2) format data containing a single RGB24 frame to WriteableBitmap.
    /// </summary>
    public static WriteableBitmap FromY4M(byte[] y4mData)
    {
        using var stream = new MemoryStream(y4mData);
        using var reader = new BinaryReader(stream);

        // Parse Y4M header
        var header = ReadY4MHeader(reader);

        if (header.Width == 0 || header.Height == 0)
            throw new InvalidDataException("Invalid Y4M header");

        // Skip to frame data (after "FRAME" marker)
        SkipToFrameData(reader);

        // Read RGB24 frame data
        var frameSize = header.Width * header.Height * 3;
        var frameData = reader.ReadBytes(frameSize);

        return FromRgb24(frameData, header.Width, header.Height);
    }

    /// <summary>
    /// Converts raw RGB24 data to WriteableBitmap.
    /// </summary>
    public static WriteableBitmap FromRgb24(byte[] rgbData, int width, int height)
    {
        // RGB24 from VapourSynth is in RGB order, WPF wants BGR for Bgr24
        // So we need to swap R and B channels, or use Rgb24 format

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);

        bitmap.Lock();
        try
        {
            var backBuffer = bitmap.BackBuffer;
            var stride = bitmap.BackBufferStride;
            var srcStride = width * 3;

            // Copy row by row to handle stride differences
            for (int y = 0; y < height; y++)
            {
                var srcOffset = y * srcStride;
                var destOffset = y * stride;
                System.Runtime.InteropServices.Marshal.Copy(
                    rgbData, srcOffset,
                    backBuffer + destOffset,
                    srcStride);
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }

        bitmap.Freeze(); // Make thread-safe
        return bitmap;
    }

    /// <summary>
    /// Converts raw BGR24 data to WriteableBitmap.
    /// </summary>
    public static WriteableBitmap FromBgr24(byte[] bgrData, int width, int height)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);

        bitmap.Lock();
        try
        {
            var backBuffer = bitmap.BackBuffer;
            var stride = bitmap.BackBufferStride;

            System.Runtime.InteropServices.Marshal.Copy(bgrData, 0, backBuffer, Math.Min(bgrData.Length, stride * height));
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.Unlock();
        }

        bitmap.Freeze();
        return bitmap;
    }

    private static Y4MHeader ReadY4MHeader(BinaryReader reader)
    {
        var header = new Y4MHeader();
        var headerLine = new StringBuilder();

        // Read until newline
        while (true)
        {
            var b = reader.ReadByte();
            if (b == 0x0A) break; // newline
            headerLine.Append((char)b);
        }

        var parts = headerLine.ToString().Split(' ');

        foreach (var part in parts)
        {
            if (part.StartsWith("W"))
                header.Width = int.Parse(part.Substring(1));
            else if (part.StartsWith("H"))
                header.Height = int.Parse(part.Substring(1));
            else if (part.StartsWith("F"))
            {
                var fps = part.Substring(1).Split(':');
                if (fps.Length == 2)
                {
                    header.FpsNum = int.Parse(fps[0]);
                    header.FpsDen = int.Parse(fps[1]);
                }
            }
            else if (part.StartsWith("C"))
                header.Colorspace = part.Substring(1);
        }

        return header;
    }

    private static void SkipToFrameData(BinaryReader reader)
    {
        // Look for "FRAME" marker followed by newline
        var frameMarker = new byte[] { (byte)'F', (byte)'R', (byte)'A', (byte)'M', (byte)'E' };
        var buffer = new byte[5];
        var stream = reader.BaseStream;

        while (stream.Position < stream.Length - 5)
        {
            stream.Read(buffer, 0, 5);

            if (buffer.SequenceEqual(frameMarker))
            {
                // Skip to after newline
                while (stream.Position < stream.Length)
                {
                    var b = reader.ReadByte();
                    if (b == 0x0A) return; // Found newline after FRAME
                }
            }

            // Move back 4 bytes to continue scanning
            stream.Position -= 4;
        }

        throw new InvalidDataException("FRAME marker not found in Y4M data");
    }

    private struct Y4MHeader
    {
        public int Width;
        public int Height;
        public int FpsNum;
        public int FpsDen;
        public string Colorspace;
    }
}
