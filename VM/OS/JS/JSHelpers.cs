using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.GUI;


namespace VM.OS.JS
{


    public class JSInterop
    {

        public Computer computer;
        public Action<string, object?>? OnModuleExported;
        public Func<string, object?>? OnModuleImported;
        public Action<int>? OnComputerExit;

        public JSInterop(Computer computer)
        {
            this.computer = computer;
            EventActions.Add("draw_pixels", DrawPixelsEvent);
            EventActions.Add("draw_image", DrawImageEvent);
        }
        public BitmapImage BitmapImageFromBase64(string base64String)
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

        public object? DrawImageEvent(string id, object? value)
        {
            if (value is null)
                return null;

            var control = GetUserContent(id);

            control?.Dispatcher.Invoke(() =>
            {
                if (control?.Content is Grid grid)
                {
                    if (grid != null)
                    {
                        var image = FindImageInGrid(grid);

                        if (image != null)
                        {
                            if (value is string Base64Image && BitmapImageFromBase64(Base64Image) is BitmapImage bitmap)
                            {
                                image.Source = bitmap;
                            }
                        }
                    }
                }
            });


            return null;
        }
        public object? DrawPixelsEvent(string id, object? value)
        {
            if (value is null)
                return null;

            var control = GetUserContent(id);

            List<byte> colorData = new();

            forEach<int>(value.ToEnumerable(), (item) => colorData.Add((byte)item));

            control?.Dispatcher.Invoke(() =>
            {
                if (control?.Content is Grid grid)
                {
                    if (grid != null)
                    {
                        var image = FindImageInGrid(grid);

                        if (image != null)
                        {
                            Draw(colorData, image);
                        }
                    }
                }
            });

            return null;
        }

        private static void Draw(List<byte> colorData, Image image)
        {
            var bytesPerPixel = 4;
            var pixelCount = colorData.Count / bytesPerPixel;

            var width = (int)Math.Sqrt(pixelCount);
            var height = pixelCount / width;

            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.Lock();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = (y * width + x) * bytesPerPixel;
                    byte b = colorData[pixelIndex];
                    byte g = colorData[pixelIndex + 1];
                    byte r = colorData[pixelIndex + 2];
                    byte a = colorData[pixelIndex + 3];

                    byte[] pixelData = new byte[] { b, g, r, a };
                    Marshal.Copy(pixelData, 0, bitmap.BackBuffer + pixelIndex, bytesPerPixel);
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            image.Source = bitmap;
        }

        private void forEach<T>(IEnumerable<object> source, Action<T> action)
        {
            try
            {
                foreach (var item in source)
                {
                    T instance = Get<T>(item, out var success);

                    if (success)
                        action(instance);
                }
            }
            catch (Exception e)
            {
                if (e is not ObjectDisposedException ode)
                {
                    Notifications.Now(e.Message);
                }
            }
        }

        private static T Get<T>(object item, out bool success)
        {
            success = false;
            if (item is T instance)
            {
                success = true;
                return instance;
            }
            return default;
        }

        private Image FindImageInGrid(Grid grid)
        {
            Image result = null;
            grid.Dispatcher.Invoke(() =>
            {
                foreach (var element in grid.Children)
                {
                    if (element is Image image)
                    {
                        result = image;
                    }
                }
            });

            return result;
        }

        #region REFLECTION

        public static void SetProperty(object target, string propertyName, object value)
        {
            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(target, value);
            }
        }

        public static object GetProperty(object target, string propertyName)
        {
            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(target);
            }

            return null;
        }

        public static object CallMethod(object target, string methodName, params object[] parameters)
        {
            var targetType = target.GetType();
            var methodInfo = targetType.GetMethod(methodName);

            if (methodInfo != null)
            {
                return methodInfo.Invoke(target, parameters);
            }

            return null;
        }

        public static object GetField(object target, string fieldName)
        {
            var targetType = target.GetType();
            var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(target);
            }

            return null;
        }

        public static void SetField(object target, string fieldName, object value)
        {
            var targetType = target.GetType();
            var fieldInfo = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(target, value);
            }
        }
        #endregion
        private UserControl? GetUserContent(string javascript_controL_class_instance_id)
        {
            var window = Runtime.GetWindow(computer);
            var resizableWins = window.Windows.Where(W => W.Key == javascript_controL_class_instance_id);
            UserControl userContent = null;

            window.Dispatcher.Invoke(() =>
            {
                if (resizableWins.Any())
                {
                    var win = resizableWins.First().Value.Content as UserWindow;

                    if (win != null)
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            var contentGrid = win.Content as Grid;

                            if (contentGrid != null)
                            {
                                var frame = contentGrid.Children.OfType<Frame>().FirstOrDefault();

                                if (frame != null)
                                {
                                    userContent = frame.Content as UserControl;
                                }
                            }
                        });
                    }
                }
            });

            return userContent;
        }
        #region System
        public void print(object message)
        {
            Runtime.GetWindow(computer).Dispatcher.Invoke(() =>
            {
                Debug.WriteLine(message);

                var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(computer);

                if (commandPrompt == default)
                {
                    Notifications.Now(message?.ToString() ?? "Invalid Print.");
                    return;
                }

                commandPrompt.DrawTextBox($"\n {message}");
            });

        }
        public void export(string id, object? obj)
        {
            OnModuleExported?.Invoke(id, obj);
        }
        public void exit(int code)
        {
            OnComputerExit?.Invoke(code);
        }
        #endregion
        #region XAML/JS interop

        public void uninstall(string dir)
        {
            ComputerWindow window = Runtime.GetWindow(computer);

            // js/html app
            if (dir.Contains(".web"))
            {
                window.Uninstall(dir); 
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.Uninstall(dir);
                return;
            }

            Notifications.Now("Incorrect path for uninstall");

        }

        public async void install(string dir)
        {
            ComputerWindow window = Runtime.GetWindow(computer);

            // js/html app
            if (dir.Contains(".web"))
            {
                window.InstallJSHTML(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.InstallWPF(dir);
            }
        }
        public void alias(string alias, string path)
        {
            computer.OS.CommandLine.Aliases.Add(alias, Runtime.GetResourcePath(path + ".js") ?? "not found");
        }
        public static Dictionary<string, Func<string, object?, object?>> EventActions = new();
        /// <summary>
        /// this returns the callback, no need for extra listening
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public object? pushEvent(string id, string eventType, object? data)
        {
            if (EventActions.TryGetValue(eventType, out var handler))
            {
                return handler.Invoke(id, data);
            }
            return null;
        }
        public void addEventHandler(string identifier, string methodName, int type)
        {
            _ = computer.OS.JavaScriptEngine.CreateEventHandler(identifier, methodName, type);
        }
        #endregion
        #region IO
        public object? require(string path)
        {
            return OnModuleImported?.Invoke(path);
        }
        public object? read_file(string path)
        {
            if (!File.Exists(path))
            {
                if (Runtime.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
                {
                    return File.ReadAllText(AbsPath);
                }

                return null;
            }
            return File.ReadAllText(path);
        }
        public string imageFromFile(string path)
        {
            byte[] imageData = null;

            if (!File.Exists(path))
            {
                if (Runtime.GetResourcePath(path) is string absPath && !string.IsNullOrEmpty(absPath))
                {
                    imageData = File.ReadAllBytes(absPath);
                }
            }
            else
            {
                imageData = File.ReadAllBytes(path);
            }

            if (imageData != null)
            {
                return Convert.ToBase64String(imageData);
            }

            return null;
        }
        public void write_file(string path, string data)
        {

            if (Runtime.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
            {
                File.WriteAllText(AbsPath, data);
                return;
            }

            File.WriteAllText(path, data);
        }
        public bool file_exists(string path)
        {
            return File.Exists(path);
        }
        #endregion
    }
}
