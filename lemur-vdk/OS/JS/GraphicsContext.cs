using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lemur.JS
{
    public record GraphicsContext(string InstanceID, string TargetControl, int PixelFormatBpp)
    {
        internal int Width, Height;
        private readonly List<byte> renderTexture = new();
        WriteableBitmap bitmap;
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
        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            renderTexture.Clear();
            for (int i = 0; i < Width * Height * PixelFormatBpp; ++i)
                renderTexture.Add(255);

            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                bitmap = new WriteableBitmap(Width, Height, 1, 1, PixelFormats.Bgra32, null);
            });
        }
       
        public void WritePixelIndexed(int x, int y, int index)
        {
            var col = palette[index];
            WritePixel(x, y, col[0], col[1], col[2], col[3]);
        }
        public void WritePixel(int x, int y, byte r, byte g, byte b, byte a)
        {
            var index = (y * Width + x) * PixelFormatBpp;

            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            renderTexture[index + 0] = b;
            renderTexture[index + 1] = g;
            renderTexture[index + 2] = r;
            renderTexture[index + 3] = a;
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
        private readonly byte[] cached_color = new byte[8];
        public void ExtractColorToCache(int color)
        {
            cached_color[0 + 0] = (byte)((color >> 24) & 0xFF);
            cached_color[0 + 1] = (byte)((color >> 16) & 0xFF);
            cached_color[0 + 2] = (byte)((color >> 8) & 0xFF);
            cached_color[0 + 3] = (byte)(color & 0xFF);
        }
        public void Draw(System.Windows.Controls.Image image)
        {
            if (image == null)
                return;

            var pixelCount = renderTexture.Count / PixelFormatBpp;
            if (pixelCount <= 1)
                return;

            bitmap.Lock();

            var stride = bitmap.BackBufferStride;

            if (renderTexture.Count == stride * Height)
            {
                IntPtr pBackBuffer = bitmap.BackBuffer;

                Marshal.Copy(renderTexture.ToArray(), 0, pBackBuffer, renderTexture.Count);

                bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            }

            bitmap.Unlock();
            image.Source = bitmap;
        }

        internal void ClearColor(int color)
        {
            ExtractColorToCache(color);
            for (int i = 0; i < Width * Height * PixelFormatBpp; i += 4)
            {
                renderTexture[i + 0] = cached_color[0];
                renderTexture[i + 1] = cached_color[1];
                renderTexture[i + 2] = cached_color[2];
                renderTexture[i + 3] = cached_color[3];
            }
        }
    }
}

