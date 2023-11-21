using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Lemur.JS
{
    public record GraphicsContext(string InstanceID, string TargetControl, int PixelFormatBpp)
    {
        internal int Width, Height;
        private readonly List<byte> renderTexture = new();
        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            renderTexture.Clear();
            for (int i = 0; i < Width * Height * PixelFormatBpp; ++i)
                renderTexture.Add(255);
        }

        static readonly List<byte[]> palette = new()
        {
            new byte[]{255, 255, 0, 0}, // Red 0
            new byte[]{255, 255, 128, 0}, // Orange 1
            new byte[]{255, 255, 255, 0}, // Yellow 2
            new byte[]{255, 128, 255, 0}, // Lime Green 3
            new byte[]{255, 0, 255, 0}, // Green 4
            new byte[]{255, 0, 255, 128}, // Spring Green 5
            new byte[]{255, 0, 255, 255}, // Cyan 6
            new byte[]{255, 0, 128, 255}, // Sky Blue 7 
            new byte[]{255, 0, 0, 255}, // Blue 8
            new byte[]{255, 128, 0, 255}, // Purple 9 
            new byte[]{255, 255, 0, 255}, // Magenta 10
            new byte[]{255, 255, 0, 128}, // Pink 11
            new byte[]{255, 192, 192, 192}, // Light Gray 12
            new byte[]{255, 128, 128, 128}, // Medium Gray 13
            new byte[]{255, 64, 64, 64}, // Dark Gray 14
            new byte[]{255, 0, 0, 0}, // Black 15
            new byte[]{255, 255, 255, 255}, // White 16
            new byte[]{255, 255, 69, 0}, // Red-Orange 17
            new byte[]{255, 255, 215, 0}, // Gold 18
            new byte[]{255, 0, 128, 0}, // Dark Green 19
            new byte[]{255, 0, 128, 128}, // Teal 20
            new byte[]{255, 0, 0, 128}, // Navy 21
            new byte[]{255, 255, 20, 147}, // Deep Pink 22
            new byte[]{255, 0, 250, 154} // Medium Spring Green 23
        };
        public void WritePixelIndexed(int x, int y, int index)
        {
            var col = palette[index];
            WritePixel(x, y, col[0], col[1], col[2], col[3]);
        }
        public void WritePixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            var index = (y * Width + x) * PixelFormatBpp;

            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            renderTexture[index] = a;
            renderTexture[index + 1] = r;
            renderTexture[index + 2] = g;
            renderTexture[index + 3] = b;
        }
        public void WritePixelPacked(int x, int y, int color)
        {
            byte r, g, b, a;
            ExtractColor(color, out r, out g, out b, out a);
            WritePixel(x,y, r, g, b, a);
        }
        public static void ExtractColor(int color, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)((color >> 24) & 0xFF);
            g = (byte)((color >> 16) & 0xFF);
            b = (byte)((color >> 8) & 0xFF);
            a = (byte)(color & 0xFF);
        }
        public void Draw(System.Windows.Controls.Image image)
        {
            // expected during disposal
            if (image is null)
                return;

            var pixelCount = renderTexture.Count / PixelFormatBpp;

            // pixel engine first frame 0 pixels
            if (pixelCount <= 1)
                return;

            var bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.Lock();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int pixelIndex = (y * Width + x) * PixelFormatBpp;
                    byte a = renderTexture[pixelIndex];
                    byte r = renderTexture[pixelIndex + 1];
                    byte g = renderTexture[pixelIndex + 2];
                    byte b = renderTexture[pixelIndex + 3];

                    byte[] pixelData = new byte[] { b, g, r, a };

                    Marshal.Copy(pixelData, 0, bitmap.BackBuffer + pixelIndex, PixelFormatBpp);
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));

            bitmap.Unlock();

            image.Source = bitmap;
        }
        internal void ClearColor(int color)
        {
            ExtractColor(color, out var r, out var g, out var b, out var a);
            for (int i = 0; i < Width * Height * PixelFormatBpp; i += 4)
            {
                renderTexture[i + 0] = r;
                renderTexture[i + 1] = g;
                renderTexture[i + 2] = b;
                renderTexture[i + 3] = a;
            }
        }
    }
}

