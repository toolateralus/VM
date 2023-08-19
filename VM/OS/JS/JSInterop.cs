using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VM.GUI;
using VM.Network;
using VM;
using VM.Types;
using System.Runtime.Serialization.Formatters.Binary;
using VM.FS;
using System.Text.RegularExpressions;

namespace VM.JS
{
    public class JSInterop
    {
        public Computer Computer;
        public Action<string, object?>? OnModuleExported;
        public Func<string, object?>? OnModuleImported;
        public Action<int>? OnComputerExit;

        public JSInterop(Computer computer)
        {
            this.Computer = computer;
            EventActions.TryAdd("draw_pixels", DrawPixelsEvent);
            EventActions.TryAdd("draw_image", DrawImageEvent);
            EventActions.TryAdd("set_content", SetContent);
            EventActions.TryAdd("get_content", GetContent);
        }
        
        public object getentries(string path)
        {
            if (File.Exists(path))
                return path;

            if (!Directory.Exists(path))
            {
                return "";
            }

            return Directory.GetFileSystemEntries(path);
        }
        public void disconnect()
        {
            Computer.Network.StopClient();
            Notifications.Now($"Disconnected from {NetworkConfiguration.LAST_KNOWN_SERVER_IP}::{NetworkConfiguration.LAST_KNOWN_SERVER_PORT}");
        }
        public void reawaken_console()
        {
            CommandPrompt cmd = null;

            cmd = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

            Computer.Window?.Dispatcher?.Invoke(() => { 

                var history = CommandPrompt.LastSentInput;

                if (cmd is null)
                {
                    Notifications.Now("No console was open, so reading is impossible");
                    return;
                }
         
                cmd.Dispatcher.Invoke(() => {
                    cmd.output.Text = history;
                });

            });
        }
        public string? read()
        {
            CommandPrompt cmd = null;
            cmd = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);
            var waiting = true;
            string result = "";
            if (cmd is null)
            {
                Notifications.Now("No console was open, so reading is impossible");
                return null;
            }
            cmd.Dispatcher.Invoke(() => {

                cmd.OnSend += end;
                void end(string output)
                {
                    result = output;
                    waiting = false;
                }
                while (waiting)
                {
                    Thread.Sleep(1);
                }
            });
            return result;

           
        }
        public double random(double max)
        {
            return System.Random.Shared.NextDouble() * max;
        }
        public static void forEach<T>(IEnumerable<object> source, Action<T> action)
        {
            try
            {
                foreach (var item in source)
                {
                    T instance = Cast<T>(item, out var success);

                    if (success)
                        action(instance);
                }
            }
            catch (Exception e)
            {
                if (e is not ObjectDisposedException ode)
                {
                    Notifications.Exception(e);
                }
            }
        }
        public static T Cast<T>(object item, out bool success)
        {
            success = false;
            if (item is T instance)
            {
                success = true;
                return instance;
            }
            return default;
        }
        public object toBytes(string background)
        {
            return Convert.FromBase64String(background);
        }
        public string toBase64(object ints)
        {
            List<byte> bytes = new List<byte>();

            forEach<int>(ints.ToEnumerable(), (i) => bytes.Add((byte)i));

            return Convert.ToBase64String(bytes.ToArray());
        }
        public async void call(string message)
        {
            if (!Computer.CommandLine.TryCommand(message))
                await Computer.JavaScriptEngine.Execute(message);
        }
        public bool start(string path)
        {
            if (path.Contains(".app"))
            {
                Task.Run(async() => { await Computer.OpenCustom(path); });
                return true;
            }
            return false;
        }
        public void print(object message)
        {
            try
            {
                Computer.Window?.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine(message);
                    Notifications.Now(message?.ToString() ?? "null");
                });
            }
            catch(Exception e)
            {
                Notifications.Exception(e);
            }
        }
        public void export(string id, object? obj)
        {
            OnModuleExported?.Invoke(id, obj);
        }
        public void exit(int code)
        {
            OnComputerExit?.Invoke(code);
        }
        public void uninstall(string dir)
        {
            ComputerWindow window = Computer.Window;

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
        public JObject GetConfig() => Computer.Config;
        public async void install(string dir)
        {
            ComputerWindow window = Computer.Window;

            if (window == null)
            {
                Notifications.Now("Window was null during install, something's gone wrong.");
                return;
            }

            // js/html app
            if (dir.Contains(".web"))
            {
                window.InstallJSHTML(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.InstallJSWPF(dir);
            }
        }

        public void alias(string alias, string path)
        {

            if (path.Split('.') is string[] arr)
            {
                if (arr.Length > 1)
                {
                    if (arr[1] != "js")
                    {
                        Notifications.Now("invalid file extension for alias");
                        return;
                    }
                    // valid .js extension
                }
                else
                {
                    // needs appended .js
                    path = path += ".js";
                }
            }

            Computer.CommandLine.Aliases[alias] = Runtime.GetResourcePath(path) ?? "not found";
        }
        /// <summary>
        /// this returns the callback, no need for extra listening
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async void sleep(int ms)
        {
            await Task.Delay(ms);
        }
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
        public string fromFile(string path)
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
        public void write_file(string path, object? data)
        {
            if (string.IsNullOrEmpty(path))
            {
                Notifications.Exception(e : new ArgumentNullException("Tried to write a file with a null or empty path, this is not allowed."));
                return;
            }
                    
            if (!Path.IsPathFullyQualified(path))
                path = Path.Combine(Computer.FS_ROOT, path);

            string? dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, data?.ToString() ?? "");
        }
        public bool file_exists(string path)
        {
            return Runtime.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath) ? File.Exists(AbsPath) : false;
        }

        public void setAliasDirectory(string path, string regex = "")
        {
            if (Runtime.GetResourcePath(path) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
            {
                // validated path.
                path = AbsPath;
                Computer.Config["ALIAS_PATH"] = path;
            }
            else
            {
                Notifications.Now("Attempted to set command directory to an emtpy or null string");
                return;
            } 
            if (File.Exists(path) && !Directory.Exists(path))
            {
                Notifications.Now("Attempted to set command directory to an existing file or a nonexistent directory");
                return;
            }
            Action<string, string> procFile = (rootDir, file) =>
            {
                string name = "";
                if (regex != "")
                {
                    name = Regex.Match(file, regex).Value;
                }
                else
                {
                    name = Path.GetFileName(file).Replace(Path.GetExtension(file), "");
                }

                Computer.CommandLine.Aliases[name] = file;
            };
            Action<string, string> procDir = delegate { }; 
            FileSystem.ProcessDirectoriesAndFilesRecursively(path, /*UNUSED*/ procDir, procFile);
        }

        public object? getProperty(string id, string controlName, object? property)
        {
            object? output = null;

            Computer.Window?.Dispatcher.Invoke((Delegate)(() =>
            {
                var userControl = GetUserContent(id, Computer);
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                output = GetProperty(control, property as string);
            }));

            return output;
        }
        public void setProperty(string id, string controlName, object? property, object? value)
        {
            object? output = null;

            Computer.Window?.Dispatcher.Invoke((Delegate)(() =>
            {
                var userControl = GetUserContent(id, Computer);
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                SetProperty(control, property as string, value);
            }));

        }

        #region C# Methods

        public T FindElementInUserControl<T>(UserControl userControl, string elementName) where T : FrameworkElement
        {
            var elementType = typeof(T);
            var contentProperty = userControl.GetType().GetProperty("Content");

            if (contentProperty != null)
            {
                var content = contentProperty.GetValue(userControl);

                if (content != null)
                {
                    foreach (var property in content.GetType().GetProperties())
                    {
                        if (elementType.IsAssignableFrom(property.PropertyType) && property.Name == elementName && property.GetValue(content) is T Instance)
                        {
                            return Instance;
                        }
                    }
                }
            }

            return default;
        }
        public static void SetProperty(object target, string propertyName, object? value)
        {
            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            propertyInfo?.SetValue(target, value);
        }
        public static object GetProperty(object target, string propertyName)
        {
            var targetType = target.GetType();
            var propertyInfo = targetType.GetProperty(propertyName);

            return propertyInfo != null ? propertyInfo.GetValue(target) : null;
        }
        private object? GetContent(string id, string controlName, object? value)
        {
            object? output = null;

            Computer.Window?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent(id, Computer);
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
        public static UserControl? GetUserContent(string javascript_controL_class_instance_id, Computer computer)
        {
            var window = computer.Window;
            var resizableWins = window?.USER_WINDOW_INSTANCES?.Where(W => W.Key == javascript_controL_class_instance_id);
            UserControl userContent = null;

            if (resizableWins != null && resizableWins.Any())
            {
                var win = resizableWins.First().Value;

                if (win != null)
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
                }
            }

            return userContent;
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
        private object? SetContent(string id, string control, object? value)
        {
            object? output = null;

            var wnd = Computer.Window;

            wnd?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent(id, Computer);

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
        public object? DrawImageEvent(string id, string target_control, object? value)
        {
            if (value is null)
                return null;

            Computer.Window?.Dispatcher.Invoke(() =>
            {
                var control = GetUserContent(id, Computer);

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
        public object? DrawPixelsEvent(string id, string target_control, object? value)
        {
            if (value is null || value.ToString().Contains("undefined"))
                return null;


            List<byte> colorData = new();

            JSInterop.forEach<int>(value.ToEnumerable(), (item) => colorData.Add((byte)item));

            Computer.Window?.Dispatcher.Invoke(() =>
            {
                var control = GetUserContent(id, Computer);
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
        public static void Draw(List<byte> colorData, Image image)
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
        #endregion
        public static Dictionary<string, Func<string, string, object?, object?>> EventActions = new();
        public object? pushEvent(string id, string targetControl, string eventType, object? data)
        {
            if (EventActions.TryGetValue(eventType, out var handler))
            {
                return handler.Invoke(id, targetControl, data);
            }
            return null;
        }
        public void eventHandler(string identifier, string targetControl, string methodName, int type)
        {
            var window = Computer.Window;

            if (window.USER_WINDOW_INSTANCES.TryGetValue(identifier, out var app))
                app.JavaScriptEngine?.CreateEventHandler(identifier, targetControl, methodName, type);
        }
        public void loadApps(object? path)
        {
            string directory = Computer.FS_ROOT;
            if (path is string dir && !string.IsNullOrEmpty(dir))
            {
                directory = dir;
            }
            if (Runtime.GetResourcePath(directory) is string AbsPath && Directory.Exists(AbsPath))
            {
                Action<string, string> procDir = (root, file) => { 
                    if (Path.GetExtension(file) is string ext && ext == ".app")
                    {
                        Computer.Window.InstallJSWPF(Path.GetFileName(file));
                    }
                    if (Path.GetExtension(file) is string _ext && _ext == ".web")
                    {
                        Computer.Window.InstallJSHTML(Path.GetFileName(file));
                    }
                };

                FileSystem.ProcessDirectoriesAndFilesRecursively(AbsPath, procDir , /* proc file */ (_,_) => { });
            }
        }
    }
}
