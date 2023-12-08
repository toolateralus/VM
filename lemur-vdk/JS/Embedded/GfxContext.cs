﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lemur;
using Image = System.Windows.Controls.Image;

namespace lemur.JS.Embedded
{
    public class GfxContext
    {
        public GfxContext(string pid, string TargetControl, int PixelFormatBpp)
        {

            Image image = null;
            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                var content = Computer.Current.UserWindows[pid];
                var app = content.JavaScriptEngine.AppModule;
                var control = app.GetUserContent(Computer.Current);
                image = Embedded.app.FindControl(control, TargetControl) as Image ?? throw new InvalidCastException(nameof(control));
            });
            this.image = new(image);
            this.PixelFormatBpp = PixelFormatBpp;
        }

        internal int PixelFormatBpp;
        internal int Width, Height;

        private byte[] renderTexture = Array.Empty<byte>();

        private WriteableBitmap bitmap;
        internal readonly WeakReference<Image> image;
        public static readonly List<byte[]> palette = new()
        {
            new byte[]{255, 0, 0, 255}, // Red 0
            new byte[]{255, 128, 0, 255}, // Orange 1
            new byte[]{255, 255, 0, 255}, // Yellow 2
            new byte[]{128, 255, 0, 255}, // Lime Green 3
            new byte[]{0, 255, 0, 255}, // Green 4
            new byte[]{0, 255, 128, 255}, // Spring Green 5
            new byte[]{0, 255, 255, 255}, // Cyan 6
            new byte[]{0, 128, 255, 255}, // Sky Blue 7 
            new byte[]{0, 0, 255, 255}, // Blue 8
            new byte[]{128, 0, 255, 255}, // Purple 9 
            new byte[]{255, 0, 255, 255}, // Magenta 10
            new byte[]{255, 0, 128, 255}, // Pink 11
            new byte[]{192, 192, 192, 255}, // Light Gray 12
            new byte[]{128, 128, 128, 255}, // Medium Gray 13
            new byte[]{64, 64, 64, 255}, // Dark Gray 14
            new byte[]{0, 0, 0, 255}, // Black 15
            new byte[]{255, 255, 255, 255}, // White 16
            new byte[]{255, 69, 0, 255}, // Red-Orange 17
            new byte[]{255, 215, 0, 255}, // Gold 18
            new byte[]{0, 128, 0, 255}, // Dark Green 19
            new byte[]{0, 128, 128, 255}, // Teal 20
            new byte[]{0, 0, 128, 255}, // Navy 21
            new byte[]{255, 20, 147, 255}, // Deep Pink 22
            new byte[]{0, 250, 154, 255} // Medium Spring Green 23
        };

        private readonly byte[] cached_color = new byte[4];

        public void Resize(int width, int height)
        {
            Width = width;
            Height = height;
            renderTexture = new byte[Width * Height * 4];

            for (int i = 0; i < Width * Height * PixelFormatBpp; ++i)
                renderTexture[i] = 255;

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
            WritePixel(x, y, r, g, b, a);
        }
        public static void ExtractColor(int color, out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)(color >> 24 & 0xFF);
            g = (byte)(color >> 16 & 0xFF);
            b = (byte)(color >> 8 & 0xFF);
            a = (byte)(color & 0xFF);
        }
        public void ExtractColorToCache(int color)
        {
            cached_color[0 + 0] = (byte)(color >> 24 & 0xFF);
            cached_color[0 + 1] = (byte)(color >> 16 & 0xFF);
            cached_color[0 + 2] = (byte)(color >> 8 & 0xFF);
            cached_color[0 + 3] = (byte)(color & 0xFF);
        }
        public void Draw(Image image)
        {
            if (image == null)
                return;

            var pixelCount = renderTexture.Length / PixelFormatBpp;
            if (pixelCount <= 1)
                return;

            bitmap.Lock();

            var stride = bitmap.BackBufferStride;

            if (renderTexture.Length == stride * Height)
            {
                nint pBackBuffer = bitmap.BackBuffer;

                Marshal.Copy(renderTexture, 0, pBackBuffer, renderTexture.Length);

                bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            }

            bitmap.Unlock();
            image.Source = bitmap;
        }

        internal unsafe void ClearColor(int color)
        {
            ExtractColorToCache(color);
            for (int i = 0; i < Width * Height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(cached_color, 0, (nint)ptr + i * PixelFormatBpp, PixelFormatBpp);
        }

        internal unsafe void ClearColorIndex(int index)
        {
            fixed (byte* ptr = cached_color)
                Marshal.Copy(palette[index], 0, (nint)ptr, 3);

            for (int i = 0; i < Width * Height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(cached_color, 0, (nint)ptr + i * PixelFormatBpp, PixelFormatBpp);
        }

        public enum PrimitiveShape
        {
            Rectangle,
            Triangle,
            Circle,
        }

        internal void DrawFilledShape(int x, int y, int h, int w, double r, int colorIndex, PrimitiveShape primitiveShape)
        {
            switch (primitiveShape)
            {
                case PrimitiveShape.Rectangle:
                    WriteFilledRectangle(x, y, h, w, r, colorIndex);
                    break;
                case PrimitiveShape.Circle:
                    WriteFilledCircle(x, y, h, w, r, colorIndex);
                    break;
                case PrimitiveShape.Triangle:
                    WriteFilledTriangle(x, y, h, w, r, colorIndex);
                    break;
                default:
                    throw new NotSupportedException($"The shape {primitiveShape} is not supported");
            }
        }

        private void WriteFilledRectangle(int x, int y, int h, int w, double r, int colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            // Adjust the coordinates to rotate around the center
            int centerX = x + w / 2;
            int centerY = y + h / 2;

            for (int i = x; i < x + w; i++)
            {
                // Calculate the relative position from the center
                int relativeX = i - centerX;

                for (int j = y; j < y + h; j++)
                {
                    int relativeY = j - centerY;

                    int rotatedX = (int)(relativeX * cosR - relativeY * sinR);
                    int rotatedY = (int)(relativeX * sinR + relativeY * cosR);

                    int finalX = rotatedX + centerX;
                    int finalY = rotatedY + centerY;

                    WritePixelIndexed(finalX, finalY, colorIndex);
                }
            }
        }

        private void WriteFilledCircle(int x, int y, int h, int w, double r, int colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            int radius = Math.Min(h, w) / 2;
            int centerX = x + w / 2;
            int centerY = y + h / 2;

            for (int i = centerX - radius; i <= centerX + radius; i++)
            {
                for (int j = centerY - radius; j <= centerY + radius; j++)
                {
                    int rotatedX = (int)Math.Round((i - centerX) * cosR - (j - centerY) * sinR) + centerX;
                    int rotatedY = (int)Math.Round((i - centerX) * sinR + (j - centerY) * cosR) + centerY;

                    if (Math.Sqrt((rotatedX - centerX) * (rotatedX - centerX) + (rotatedY - centerY) * (rotatedY - centerY)) <= radius)
                    {
                        WritePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }

        private void WriteFilledTriangle(int x, int y, int h, int w, double r, int colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            int centerX = x + w / 2;
            int centerY = y + h / 2;

            for (int i = x; i < x + w; i++)
            {
                for (int j = y; j < y + h; j++)
                {
                    int relativeX = i - centerX;
                    int relativeY = j - centerY;

                    int rotatedX = (int)(relativeX * cosR - relativeY * sinR) + centerX;
                    int rotatedY = (int)(relativeX * sinR + relativeY * cosR) + centerY;

                    if (IsPointInsideTriangle(rotatedX, rotatedY, x, y, x + w, y, x + w / 2, y + h))
                    {
                        WritePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }


        private bool IsPointInsideTriangle(int x, int y, int x1, int y1, int x2, int y2, int x3, int y3)
        {
            int denominator = (y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3);
            int a = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) / denominator;
            int b = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) / denominator;
            int c = 1 - a - b;

            return a >= 0 && a <= 1 && b >= 0 && b <= 1 && c >= 0 && c <= 1;
        }
    }
}
