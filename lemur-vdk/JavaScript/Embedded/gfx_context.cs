using Lemur.FS;
using Lemur.GUI;
using Lemur.Windowing;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace Lemur.JS.Embedded
{
    public class GraphicsContext
    {
        public GraphicsContext(Computer computer, string pid, string TargetControl, int PixelFormatBpp)
        {

            Image image = null;
            Computer.Current.Window.Dispatcher.Invoke(() =>
            {
                var content = computer.ProcessManager.GetProcess(pid).UI;
                var app = content.Engine.AppModule;
                var control = app.GetUserContent();

                image = Embedded.app_t.FindControl(control, TargetControl) as Image;

                if (image == null) {
                    Notifications.Now($"{TargetControl} {image} target control not found when creating graphics context.");
                    return;
                }


            });
            this.image = new(image);
            this.PixelFormatBpp = PixelFormatBpp;
        }

        internal int PixelFormatBpp;
        internal int Width, Height;

        private byte[] renderTexture = Array.Empty<byte>();

        private WriteableBitmap bitmap;
        private WriteableBitmap skybox;
        internal  readonly WeakReference<Image> image;
        

        private byte[] cached_color = new byte[4];

        public void Resize(double width, double height)
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



        public void WritePixelIndexed(double x, double y, double index)
        {
            var col = graphics.palette[(int)index];
            WritePixel(x, y, col[0], col[1], col[2], col[3]);
        }
        public void WritePixel(double x, double y, byte r, byte g, byte b, byte a)
        {
            var index = (int)((y * Width + x) * PixelFormatBpp);

            if (x < 0 || y < 0 || x >= Width || y >= Height)
                return;

            renderTexture[index + 0] = b;
            renderTexture[index + 1] = g;
            renderTexture[index + 2] = r;
            renderTexture[index + 3] = a;
        }
        public void WritePixelPacked(double x, double y, double color)
        {
            byte r, g, b, a;
            ExtractColor(color, out r, out g, out b, out a);
            WritePixel(x, y, r, g, b, a);
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

        internal unsafe void ClearColor(double color)
        {
            ExtractColorToCache(color);
            for (int i = 0; i < Width * Height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(cached_color, 0, (nint)ptr + i * PixelFormatBpp, PixelFormatBpp);
        }

        internal unsafe void ClearColorIndex(double index)
        {
            cached_color = graphics.palette[(int)index];

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

        internal  void DrawFilledShape(double x, double y, double h, double w, double r, double colorIndex, PrimitiveShape primitiveShape)
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

        private void WriteFilledRectangle(double x, double y, double h, double w, double r, double colorIndex)
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

                    WritePixelIndexed(finalX, finalY, colorIndex);
                }
            }
        }

        private void WriteFilledCircle(double x, double y, double h, double w, double r, double colorIndex)
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
                        WritePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }

        private void WriteFilledTriangle(double x, double y, double h, double w, double r, double colorIndex)
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

                    if (IsPodoubleInsideTriangle(rotatedX, rotatedY, x, y, x + w, y, x + w / 2, y + h))
                    {
                        WritePixelIndexed(rotatedX, rotatedY, colorIndex);
                    }
                }
            }
        }


        private static bool IsPodoubleInsideTriangle(double x, double y, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double denominator = (y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3);
            double a = ((y2 - y3) * (x - x3) + (x3 - x2) * (y - y3)) / denominator;
            double b = ((y3 - y1) * (x - x3) + (x1 - x3) * (y - y3)) / denominator;
            double c = 1 - a - b;

            return a >= 0 && a <= 1 && b >= 0 && b <= 1 && c >= 0 && c <= 1;
        }
        internal  void DrawSkybox()
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
        internal void SaveToImage(string filePath)
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
        public void LoadSkybox(string filePath)
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
        internal void LoadFromImage(string filePath)
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

