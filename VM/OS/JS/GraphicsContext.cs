using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VM.JS
{
    public record GraphicsContext(string InstanceID, string TargetControl, int PixelFormatBpp)
    {
        public int Width, Height;
        private readonly List<byte> renderTexture = new();
        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            renderTexture.Clear();
            for (int i = 0; i < Width * Height * PixelFormatBpp; ++i)
                renderTexture.Add(255);
        } 
        public void WritePixel(int x, int y, int color)
        {
            byte r, g, b, a;
            ExtractColor(color, out r, out g, out b, out a);

            var index = (y * Width + x) * PixelFormatBpp;

            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                Notifications.Now("GraphicsContext.WritePixel - index out of bounds. your x and y input must be non negative and within bounds of your context");
                return;
            }

            renderTexture[index] = a;
            renderTexture[index + 1] = r;
            renderTexture[index + 2] = g;
            renderTexture[index + 3] = b;
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

        internal void clearColor(int color)
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

