using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lemur.GUI;
using Lemur.FS;
using Image = System.Windows.Controls.Image;
using System.Security.Permissions;
using System.Reflection;

namespace Lemur.JS
{
    public class app
    {
        public delegate bool SetPropertyHandler(PropertyInfo? propertyInfo, object target, object? value);
        public delegate object? AppEvent(string target, object? value);
        public static Dictionary<string, AppEvent> ExposedEvents = new();
        public static Dictionary<string, SetPropertyHandler> SetPropertyHandlers = new()
        {
            {"Visibility", (propertyInfo, target, value) => {
                if (value is int i) {
                    Visibility val = (Visibility)i;
                    propertyInfo?.SetValue(target, val);
                    return true;
                }
                return false;
            }},

        };
        private string id;
        public app()
        {
            ExposedEvents["draw_pixels"] = DrawPixelsEvent; // somewhat deprecated, use the dedicated graphics module instead.
            ExposedEvents["draw_image"] = DrawImageEvent;
            ExposedEvents["set_content"] = SetContent;
            ExposedEvents["get_content"] = GetContent;
        }

        public static void SetProperty(object target, string propertyName, object? value)
        {
            if (target == null)
            {
                Notifications.Now("Target control in 'SetProperty' was null.");
                return;
            }

            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            // the property had no special handler.
            // this could mean that the property is unsupported and it may throw an exception
            // but it probably means it's the normal case of a supported set of args coming from js,
            // like ActualWidth taking a double/long or whatever.
            if (!SetPropertyHandlers.TryGetValue(propertyName, out var handler))
            {
                propertyInfo?.SetValue(target, value);
            }
            else
            {
                if (handler.Invoke(propertyInfo, target, value))
                    return;

                // failed in setting the property
                Notifications.Now($"{propertyName} failed to set. this likely means 'app.setProperty' recieved some bad arguments, or invalid for the particular property.");
                return;
            }
        }
        public static object? GetProperty(object target, string propertyName)
        {
            if (target == null)
            {
                Notifications.Now("Target control in 'GetProperty' was null.");
                return null;
            }
            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            return propertyInfo?.GetValue(target);
        }
        private object? GetContent(string controlName, object? value)
        {
            object? output = null;

            Computer.Current.Window?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent(Computer.Current);
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                if (control.GetType() == typeof(TextBox) || control.GetType() == typeof(TextBlock))
                {
                    output = GetProperty(control, "Text");
                }
                else
                {
                    output = GetProperty(control, "Content");
                }
            });

            return output;
        }
        public UserControl? GetUserContent(Computer computer)
        {
            var window = computer?.Window;

            var resizableWins = computer.UserWindows?.Where(W => W.Key == id);

            if (resizableWins != null && resizableWins.Any())
            {
                UserWindow win = resizableWins.First().Value;
                var frame = win.ContentsFrame;
                if (frame.Content is UserControl userContent)
                    return userContent;
            }

            return null;
        }
        public static FrameworkElement? FindControl(UserControl userControl, string controlName)
        {

            FrameworkElement element = null;
            var contentProperty = userControl?.GetType()?.GetProperty("Content");

            if (contentProperty != null)
            {
                var content = contentProperty.GetValue(userControl);

                if (content != null)
                {
                    if (content is FrameworkElement contentElement && contentElement.Name == controlName)
                    {
                        return contentElement;
                    }

                    return SearchVisualTree(content, controlName);
                }
            }
            return element;
        }
        public static FrameworkElement? SearchVisualTree(object element, string controlName)
        {
            if (element is FrameworkElement frameworkElement && frameworkElement.Name == controlName)
            {
                return frameworkElement;
            }

            if (element is DependencyObject dependencyObject)
            {
                int childCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
                for (int i = 0; i < childCount; i++)
                {
                    var childElement = VisualTreeHelper.GetChild(dependencyObject, i);
                    var result = SearchVisualTree(childElement, controlName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
        private object? SetContent(string control, object? value)
        {
            object? output = null;

            var wnd = Computer.Current.Window;

            wnd?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent(Computer.Current);

                if (userControl == null)
                    return;

                var _control = FindControl(userControl, control);

                if (_control == null)
                    return;

                if (_control.GetType() == typeof(TextBox) || _control.GetType() == typeof(TextBlock))
                {
                    SetProperty(_control, "Text", value);
                }
                else
                {
                    SetProperty(_control, "Content", value);
                }
            });
            return output;
        }
        public static BitmapImage BitmapImageFromBase64(string base64String)
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(base64String);

                using (MemoryStream memoryStream = new MemoryStream(imageBytes))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.EndInit();
                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that may occur during conversion
                Console.WriteLine("Exception during base64 to BitmapImage conversion: " + ex.Message);
                return null;
            }
        }
        public object? DrawImageEvent(string target_control, object? value)
        {
            if (value is null)
                return null;

            Computer.Current.Window?.Dispatcher.Invoke(() =>
            {
                var control = GetUserContent(Computer.Current);

                var image = FindControl(control, target_control);

                if (image is Image img)
                {
                    if (value is string Base64Image && BitmapImageFromBase64(Base64Image) is BitmapImage bitmap)
                    {
                        img.Source = bitmap;
                    }
                }
            });
            return null;
        }
        public object? DrawPixelsEvent(string target_control, object? value)
        {
            if (value is null || value.ToString().Contains("undefined"))
                return null;


            List<byte> colorData = new();

            interop.ForEachCast<int>(value.ToEnumerable(), (item) => colorData.Add((byte)item));

            Computer.Current.Window?.Dispatcher.Invoke(() =>
            {
                var control = GetUserContent(Computer.Current);
                if (control?.Content is Grid grid)
                {
                    if (grid != null)
                    {
                        var image = FindControl(control, target_control) as Image;

                        if (image != null)
                        {
                            Draw(colorData, image);
                        }
                    }
                }
            });

            return null;
        }
        public static void Draw(List<byte> colorData, System.Windows.Controls.Image image)
        {
            var bytesPerPixel = 4;
            var pixelCount = colorData.Count / bytesPerPixel;
            if (pixelCount <= 1)
                return;

            var width = (int)Math.Sqrt(pixelCount);
            var height = pixelCount / width;

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.Lock();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * width + x) * bytesPerPixel;
                    byte a = colorData[pixelIndex];
                    byte r = colorData[pixelIndex + 1];
                    byte g = colorData[pixelIndex + 2];
                    byte b = colorData[pixelIndex + 3];

                    byte[] pixelData = new byte[] { b, g, r, a };
                    Marshal.Copy(pixelData, 0, bitmap.BackBuffer + pixelIndex, bytesPerPixel);
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            image.Source = bitmap;
        }
        public object? getProperty(string controlName, object? property)
        {
            object? output = null;

            Computer.Current.Window?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent(Computer.Current);
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                output = GetProperty(control, property as string);
            });

            return output;
        }
        public void setProperty(string controlName, object? property, object? value)
        {
            object? output = null;

            Computer.Current.Window?.Dispatcher.Invoke((Delegate)(() =>
            {
                var userControl = GetUserContent(Computer.Current);
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                SetProperty(control, property as string, value);
            }));

        }
        public object? pushEvent(string targetControl, string eventType, object? data)
        {
            if (ExposedEvents.TryGetValue(eventType, out var handler))
            {
                return handler.Invoke(targetControl, data);
            }
            return null;
        }
        public async void eventHandler(string targetControl, string methodName, int type)
        {
            if (Computer.Current.UserWindows.TryGetValue(id, out var app))
                await app.JavaScriptEngine?.CreateEventHandler(id, targetControl, methodName, type);
        }
        public async void start(string path)
            {
                _ = Computer.Current.Window.Dispatcher.Invoke(async () => await Computer.Current.OpenCustom(path));
            }
        public void loadApps(object? path)
        {
            string directory = FileSystem.Root;

            if (path is string dir && !string.IsNullOrEmpty(dir))
                directory = dir;

            if (FileSystem.GetResourcePath(directory) is string AbsPath && Directory.Exists(AbsPath))
            {
                Action<string, string> procDir = (root, file) => {
                    if (Path.GetExtension(file) is string ext && ext == ".app")
                        Computer.Current.InstallJSWPF(Path.GetFileName(file));
                    if (Path.GetExtension(file) is string _ext && _ext == ".web")
                        Computer.Current.InstallJSHTML(Path.GetFileName(file));
                };

                FileSystem.ProcessDirectoriesAndFilesRecursively(AbsPath, procDir, /* proc file */ (_, _) => { });
            }
        }
        public void install(string dir)
        {
            if (dir.Contains(".web"))
            {
                Computer.Current.InstallJSHTML(dir);
                return;
            }

            if (dir.Contains(".app"))
            {
                Computer.Current.InstallJSWPF(dir);
            }
        }
        public void uninstall(string dir)
        {
            ComputerWindow window = Computer.Current.Window;

            // js/html app
            if (dir.Contains(".web"))
            {
                Computer.Current.Uninstall(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                Computer.Current.Uninstall(dir);
                return;
            }

            Notifications.Now("Incorrect path for uninstall");

        }

        internal void __SetId(string id)
        {
            this.id = id;
        }
    }
}

