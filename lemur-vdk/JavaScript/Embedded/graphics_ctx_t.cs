using Lemur.FS;
using Lemur.Windowing;
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

namespace Lemur.JS.Embedded {
    public class graphics_ctx_t {
        internal int formatBpp;
        internal int width, height;
        private byte[] renderTexture = [];
        private WriteableBitmap bitmap;
        internal readonly WeakReference<Image> image;

        public static readonly IReadOnlyList<byte[]> Palette = [
            // _________________________
            //  | R  | G  |  B  |  A  |
            //  ------------------------
            [255, 0, 0, 255],        // Red 0
            [255, 128, 0, 255],        // Orange 1
            [255, 255, 0, 255],        // Yellow 2
            [128, 255, 0, 255],        // Lime Green 3
            [0, 255, 0, 255],        // Green 4
            [0, 255, 128, 255],        // Spring Green 5
            [0, 255, 255, 255],        // Cyan 6
            [0, 128, 255, 255],        // Sky Blue 7 
            [0, 0, 255, 255],        // Blue 8
            [128, 0, 255, 255],        // Purple 9 
            [255, 0, 255, 255],        // Magenta 10
            [255, 0, 128, 255],        // Pink 11
            [192, 192, 192, 255],        // Light Gray 12
            [128, 128, 128, 255],        // Medium Gray 13
            [64, 64, 64, 255],        // Dark Gray 14
            [0, 0, 0, 255],        // Black 15
            [255, 255, 255, 255],        // White 16
            [255, 69, 0, 255],        // Red-Orange 17
            [255, 215, 0, 255],        // Gold 18
            [0, 128, 0, 255],        // Dark Green 19
            [0, 128, 128, 255],        // Teal 20
            [0, 0, 128, 255],        // Navy 21
            [255, 20, 147, 255],        // Deep Pink 22
            [0, 250, 154, 255]         // Medium Spring Green 23
        ];

        /// <summary>
        /// this embedded type allows users to attach a WPF 'Image' control to a drawable bitmap render surface and 
        /// provides utility to draw, load and save images.
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="targetControl"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="PixelFormatBpp"></param>
        public graphics_ctx_t(string pid, string targetControl, int width, int height, int PixelFormatBpp = 4) {

            this.width = width;
            this.height = height;
            this.formatBpp = PixelFormatBpp;
            resize(this.width, this.height);

            Image image = null;
            Computer.Current.Window.Dispatcher.Invoke(() => {
                var proc = Computer.Current.ProcessManager.GetProcess(pid);

                if (proc is null) {
                    Notifications.Now("Invalid PID passed into `graphics_ctx_t`.");
                    return;
                }

                var content = proc.UI;
                var app = content.Engine.AppModule;
                var control = app.GetUserContent();

                if (control is null) {
                    Notifications.Now($"{targetControl} {image} target control not found when creating graphics context.");
                    return;
                }

                var ctrl = app_t.FindControl(control, targetControl);

                if (ctrl is null || ctrl as Image == null) {
                    Notifications.Now($"{targetControl} {image} target control not found when creating graphics context.");
                    return;
                }

                image = (ctrl as Image)!;

            });
            this.image = new(image);

        }
        /// <summary>
        /// This cannot be called from JavaScript.
        /// </summary>
        /// <param name="image"></param>
        public void m_Draw(Image image) {
            if (image == null)
                return;

            var pixelCount = renderTexture.Length / formatBpp;
            
            if (pixelCount <= 1)
                return;

            bitmap.Lock();

            var stride = bitmap.BackBufferStride;

            if (renderTexture.Length == stride * height) {
                nint pBackBuffer = bitmap.BackBuffer;

                Marshal.Copy(renderTexture, 0, pBackBuffer, renderTexture.Length);

                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }

            bitmap.Unlock();
            image.Source = bitmap;
        }
        /// <summary>
        /// resize the render surface.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void resize(double width, double height) {
            this.width = (int)width;
            this.height = (int)height;
            renderTexture = new byte[this.width * this.height * 4];

            for (int i = 0; i < this.width * this.height * formatBpp; ++i)
                renderTexture[i] = 255;

            Computer.Current.Window.Dispatcher.Invoke(() => {
                bitmap = new WriteableBitmap(this.width, this.height, 1, 1, PixelFormats.Bgra32, null);
            });
        }
        /// <summary>
        /// write a pixel to the render surface from the indexed color palette
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="index"></param>
        public void writePixelIndexed(double x, double y, double index) {
            var col = Palette[(int)index];
            writePixel(x, y, col[0], col[1], col[2], col[3]);
        }
        /// <summary>
        /// write a pixel to the render surface using r, g, b, a byte values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="a"></param>
        public void writePixel(double x, double y, byte r, byte g, byte b, byte a) {
            var index = (int)((y * width + x) * formatBpp);

            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            renderTexture[index + 0] = b;
            renderTexture[index + 1] = g;
            renderTexture[index + 2] = r;
            renderTexture[index + 3] = a;
        }
        /// <summary>
        /// the actual draw call to copy the pixel data to the specified render image.
        /// </summary>
        /// <returns></returns>
        public bool flush() {
            Computer.Current?.Window?.Dispatcher?.Invoke(() => {
                if (this.image.TryGetTarget(out var image))
                    m_Draw(image);
            });
            return true;
        }
        /// <summary>
        /// sets the entire renderTexture to a solid color from the palette by index.
        /// </summary>
        /// <param name="index"></param>
        public unsafe void clearColor(double index) {
            var rgbacolor = Palette[(int)index].ToArray(); // starts as rgba
            var bgraColor = new byte[] {
                rgbacolor[2],
                rgbacolor[1],
                rgbacolor[0],
                rgbacolor[3],
            };


            for (int i = 0; i < width * height; i++)
                fixed (byte* ptr = renderTexture)
                    Marshal.Copy(bgraColor, 0, (nint)ptr + i * formatBpp, formatBpp);
        }
        /// <summary>
        /// draw a filled rectangle with specified arguments
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="rot"></param>
        /// <param name="colorIndex"></param>
        public void drawRect(double x, double y, double height, double width, double rot, double colorIndex) {
            double cosR = Math.Cos(rot);
            double sinR = Math.Sin(rot);

            // Adjust the coordinates to rotate around the center
            double centerX = x + width / 2;
            double centerY = y + height / 2;

            for (double i = x; i < x + width; i++) {
                // Calculate the relative position from the center
                double relativeX = i - centerX;

                for (double j = y; j < y + height; j++) {
                    double relativeY = j - centerY;

                    double rotatedX = (double)(relativeX * cosR - relativeY * sinR);
                    double rotatedY = (double)(relativeX * sinR + relativeY * cosR);

                    double finalX = rotatedX + centerX;
                    double finalY = rotatedY + centerY;

                    writePixelIndexed(Math.Round(finalX), Math.Round(finalY), colorIndex);
                }
            }
        }
        /// <summary>
        /// draw a filled circle with specified arguments.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="h"></param>
        /// <param name="w"></param>
        /// <param name="r"></param>
        /// <param name="colorIndex"></param>
        public void drawCircle(double x, double y, double h, double w, double r, double colorIndex) {
            double cosR = Math.Cos(r);
            double sinR = Math.Sin(r);

            double radius = Math.Min(h, w) / 2;
            double centerX = x + w / 2;
            double centerY = y + h / 2;

            for (double i = centerX - radius; i <= centerX + radius; i++) {
                for (double j = centerY - radius; j <= centerY + radius; j++) {
                    double rotatedX = (double)Math.Round((i - centerX) * cosR - (j - centerY) * sinR) + centerX;
                    double rotatedY = (double)Math.Round((i - centerX) * sinR + (j - centerY) * cosR) + centerY;

                    if (Math.Sqrt((rotatedX - centerX) * (rotatedX - centerX) + (rotatedY - centerY) * (rotatedY - centerY)) <= radius) {
                        writePixelIndexed(Math.Round(rotatedX), Math.Round(rotatedY), colorIndex);
                    }
                }
            }
        }
        /// <summary>
        /// Saves the current render surface to an image of '.bmp' type at filePath
        /// </summary>
        /// <param name="filePath"></param>
        public void saveToImage(string filePath) {
            try {
                using Bitmap bitmap = new((int)width, (int)height);
                for (double y = 0; y < height; y++) {
                    for (double x = 0; x < width; x++) {
                        double index = (y * width + x) * formatBpp;
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
            catch (Exception ex) {
                Notifications.Now($"Error saving image: {ex.Message}");
            }
        }
        /// <summary>
        /// Loads up an image from filePath and writes it to the current render surface
        /// </summary>
        /// <param name="filePath"></param>
        public void loadFromImage(string filePath) {
            try {
                Computer.Current.Window.Dispatcher.Invoke(() => {
                    using var bmp = new Bitmap(FileSystem.GetResourcePath(filePath));

                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        bmp.GetHbitmap(),
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    bitmap = new(bitmapSource);

                    Notifications.Now("Bitmap loaded successfully.");

                    if (image.TryGetTarget(out var imageControl))
                        m_Draw(imageControl);
                    else Notifications.Now("Image control not found when loading image."); 
                });
            }
            catch (Exception ex) {
                Notifications.Now($"Error loading image: {ex.Message}");
            }
        }
    }
}

