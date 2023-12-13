using Lemur.FS;
using Lemur.GUI;
using Lemur.Windowing;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace Lemur.JS.Embedded
{
    public enum PrimitiveShape
        {
            Rectangle,
            Triangle,
            Circle,
        }
    public class GraphicsContext
    {
        internal int PixelFormatBpp;
        internal int Width, Height;
        private byte[] renderTexture = Array.Empty<byte>();
        private WriteableBitmap bitmap;
        private WriteableBitmap skybox;
        internal readonly WeakReference<Image> image;
        private byte[] cached_color = new byte[4];

        public static readonly IReadOnlyList<byte[]> Palette =
        [
        // _________________________
        //  | B  | R  |  G  |  A  |
        //  ------------------------
            [255,  0,    0,    255],        // Red 0
            [255,  128,  0,    255],        // Orange 1
            [255,  255,  0,    255],        // Yellow 2
            [128,  255,  0,    255],        // Lime Green 3
            [0,    255,  0,    255],        // Green 4
            [0,    255,  128,  255],        // Spring Green 5
            [0,    255,  255,  255],        // Cyan 6
            [0,    128,  255,  255],        // Sky Blue 7 
            [0,    0,    255,  255],        // Blue 8
            [128,  0,    255,  255],        // Purple 9 
            [255,  0,    255,  255],        // Magenta 10
            [255,  0,    128,  255],        // Pink 11
            [192,  192,  192,  255],        // Light Gray 12
            [128,  128,  128,  255],        // Medium Gray 13
            [64,   64,   64,   255],        // Dark Gray 14
            [0,    0,    0,    255],        // Black 15
            [255,  255,  255,  255],        // White 16
            [255,  69,   0,    255],        // Red-Orange 17
            [255,  215,  0,    255],        // Gold 18
            [0,    128,  0,    255],        // Dark Green 19
            [0,    128,  128,  255],        // Teal 20
            [0,    0,    128,  255],        // Navy 21
            [255,  20,   147,  255],        // Deep Pink 22
            [0,    250,  154,  255]         // Medium Spring Green 23
        ];

        public GraphicsContext(string pid, string targetControl, int width, int height, int PixelFormatBpp = 4)
        {

            Width = width;
            Height = height;
            this.PixelFormatBpp = PixelFormatBpp;
            resize(Width, Height);

            Image image = null;
            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                var content = Computer.Current.ProcessManager.GetProcess(pid).UI;
                var app = content.Engine.AppModule;
                var control = app.GetUserContent();

                image = Embedded.app_t.FindControl(control, targetControl) as Image;

                if (image == null)
                {
                    Notifications.Now($"{targetControl} {image} target control not found when creating graphics context.");
                    return;
                }
            });
            this.image = new(image);

        }
        
        public void writePixel(double x, double y, double color)
        {
            writePixelPacked(x, y, color);
        }
        public void resize(double width, double height)
        {
            Width = (int)width;
            Height = (int)height;
            renderTexture = new byte[Width * Height * 4];

            for (int i = 0; i < Width * Height * PixelFormatBpp; ++i)
                renderTexture[i] = 255;

            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                bitmap = new WriteableBitmap(Width, Height, 1, 1, PixelFormats.Bgra32, null);
            });
        }
        public void writePixelIndexed(double x, double y, double index)
        {
            var col = Palette[(int)index];
            writePixel(x, y, col[0], col[1], col[2], col[3]);
        }
        public void writePixel(double x, double y, byte r, byte g, byte b, byte a)
        {
            var index = (int)((y * Width + x) * PixelFormatBpp);

            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            renderTexture[index + 0] = b;
            renderTexture[index + 1] = g;
            renderTexture[index + 2] = r;
            renderTexture[index + 3] = a;
        }
        public void writePixelPacked(double x, double y, double color)
        {
            byte r, g, b, a;
            ExtractColor(color, out r, out g, out b, out a);
            writePixel(x, y, r, g, b, a);
        }
        public static void ExtractColor(double color, out byte r, out byte g, out byte b, out byte a)
        {
            var col = (byte)color;

            r = (byte)(col >> 24 & 0xFF);
            g = (byte)(col >> 16 & 0xFF);
            b = (byte)(col >> 8 & 0xFF);
            a = (byte)(col & 0xFF);
        }
        public void ExtractColorToCache(double color)
        {
            var col = (int)color;
            cached_color[0 + 0] = (byte)(col >> 24 & 0xFF);
            cached_color[0 + 1] = (byte)(col >> 16 & 0xFF);
            cached_color[0 + 2] = (byte)(col >> 8 & 0xFF);
            cached_color[0 + 3] = (byte)(col & 0xFF);
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
        public bool flushCtx()
        {
            Computer.Current?.Window?.Dispatcher?.Invoke(() =>
            {
                if (this.image.TryGetTarget(out var image))
                    Draw(image);
            });

            return true;
        }
        public unsafe void clearColor(double color)
        {
            ExtractColorToCache(color);
            for (int i = 0; i < Width * Height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(cached_color, 0, (nint)ptr + i * PixelFormatBpp, PixelFormatBpp);
        }
        public unsafe void clearColorIndex(double index)
        {
            cached_color = Palette[(int)index].ToArray();

            for (int i = 0; i < Width * Height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(cached_color, 0, (nint)ptr + i * PixelFormatBpp, PixelFormatBpp);
        }
        public void drawFilledShape(double x, double y, double h, double w, double r, double colorIndex, PrimitiveShape primitiveShape)
        {
            switch (primitiveShape)
            {
                case PrimitiveShape.Rectangle:
                    writeFilledRectangle(x, y, h, w, r, colorIndex);
                    break;
                case PrimitiveShape.Circle:
                    writeFilledCircle(x, y, h, w, r, colorIndex);
                    break;
                case PrimitiveShape.Triangle:
                    writeFilledTriangle(x, y, h, w, r, colorIndex);
                    break;
                default:
                    throw new NotSupportedException($"The shape {primitiveShape} is not supported");
            }
        }
        public void writeFilledRectangle(double x, double y, double h, double w, double r, double colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            // Adjust the coordinates to rotate around the center
            double centerX = x + w / 2;
            double centerY = y + h / 2;

            for (double i = x; i < x + w; i++)
            {
                // Calculate the relative position from the center
                double relativeX = i - centerX;

                for (double j = y; j < y + h; j++)
                {
                    double relativeY = j - centerY;

                    double rotatedX = (double)(relativeX * cosR - relativeY * sinR);
                    double rotatedY = (double)(relativeX * sinR + relativeY * cosR);

                    double finalX = rotatedX + centerX;
                    double finalY = rotatedY + centerY;

                    writePixelIndexed(finalX, finalY, colorIndex);
                }
            }
        }
        public void writeFilledCircle(double x, double y, double h, double w, double r, double colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            double radius = Math.Min(h, w) / 2;
            double centerX = x + w / 2;
            double centerY = y + h / 2;

            for (double i = centerX - radius; i <= centerX + radius; i++)
            {
                for (double j = centerY - radius; j <= centerY + radius; j++)
                {
                    double rotatedX = (double)Math.Round((i - centerX) * cosR - (j - centerY) * sinR) + centerX;
                    double rotatedY = (double)Math.Round((i - centerX) * sinR + (j - centerY) * cosR) + centerY;

                    if (Math.Sqrt((rotatedX - centerX) * (rotatedX - centerX) + (rotatedY - centerY) * (rotatedY - centerY)) <= radius)
                    {
                        writePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }
        public void writeFilledTriangle(double x, double y, double h, double w, double r, double colorIndex)
        {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            double centerX = x + w / 2;
            double centerY = y + h / 2;

            for (double i = x; i < x + w; i++)
            {
                for (double j = y; j < y + h; j++)
                {
                    double relativeX = i - centerX;
                    double relativeY = j - centerY;

                    double rotatedX = (double)(relativeX * cosR - relativeY * sinR) + centerX;
                    double rotatedY = (double)(relativeX * sinR + relativeY * cosR) + centerY;

                    if (IsPointInsideTri(rotatedX, rotatedY, x, y, x + w, y, x + w / 2, y + h))
                    {
                        writePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }
        public bool IsPointInsideTri(double x, double y, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double denominator = (y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3);
            double a = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) / denominator;
            double b = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) / denominator;
            double c = 1 - a - b;

            return a >= 0 && a <= 1 && b >= 0 && b <= 1 && c >= 0 && c <= 1;
        }
        public void drawSkybox()
        {
            lock(bitmap)
            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                if (skybox != null)
                {
                    // let's do the most performant copy of pixel data possible from skybox to bitmap
                    // https://stackoverflow.com/questions/6484357/copying-from-a-bitmapsource-to-a-writeablebitmap

                    var rect = new Int32Rect(0, 0, skybox.PixelWidth, skybox.PixelHeight);
                    skybox.CopyPixels(rect, renderTexture, Width * PixelFormatBpp, 0);
                    bitmap = skybox.Clone();
                } 
                else
                {
                    Notifications.Now("No skybox was loaded. call 'Graphics.loadSkybox(string path)'");
                }

            });
          
        }
        public void saveToImage(string filePath)
        {
            try
            {
                using Bitmap bitmap = new((int)Width, (int)Height);
                for (double y = 0; y < Height; y++)
                {
                    for (double x = 0; x < Width; x++)
                    {
                        double index = (y * Width + x) * PixelFormatBpp;
                        byte r = renderTexture[(int)index + 2];
                        byte g = renderTexture[(int)index + 1];
                        byte b = renderTexture[(int)index];
                        byte a = renderTexture[(int)index + 3];
                        System.Drawing.Color color = System.Drawing.Color.FromArgb(a, r, g, b);
                        bitmap.SetPixel((int)x, (int)y, color);
                    }
                }

                FileSystem.Write(filePath, "");

                filePath = FileSystem.GetResourcePath(filePath);

                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Notifications.Now($"Error saving image: {ex.Message}");
            }
        }
        public void loadSkybox(string filePath)
        {
            try
            {
                Computer.Current.Window.Dispatcher.Invoke(() =>
                {
                    using var bmp = new Bitmap(FileSystem.GetResourcePath(filePath));

                    // Resize the skybox image to match the dimensions of your render texture
                    bmp.SetResolution((int)Width, (int)Height);

                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        bmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    skybox = new(bitmapSource);

                    if (image.TryGetTarget(out var imageControl))
                        Draw(imageControl);
                });
            }
            catch (Exception ex)
            {
                Notifications.Now($"Error loading skybox: {ex.Message}");
            }
        }
        public void loadFromImage(string filePath)
        {
            try
            {
                Computer.Current.Window.Dispatcher.Invoke(() =>
                {
                    using var bmp = new Bitmap(FileSystem.GetResourcePath(filePath));

                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        bmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bitmap = new(bitmapSource);

                    if (image.TryGetTarget(out var imageControl))
                        Draw(imageControl);
                });

            }
            catch (Exception ex)
            {
                Notifications.Now($"Error loading image: {ex.Message}");
            }
        }
    }
}

