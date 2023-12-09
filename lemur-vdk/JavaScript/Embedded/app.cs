using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.Windowing;
using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Lemur.Computer;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Image = System.Windows.Controls.Image;

namespace Lemur.JS.Embedded
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
        internal Thread bgThread;
        private ConcurrentQueue<Action> deferredJobs = [];

        private bool Disposing { get;  set; }

        public void ReleaseThread()
        {
            if (!Disposing)
            {
                Disposing = true;
                bgThread?.Join();
            }
        }
        public app()
        {
            ExposedEvents["draw_pixels"] = DrawPixelsEvent; // somewhat deprecated, use the dedicated graphics module instead.
            ExposedEvents["draw_image"] = DrawImageEvent;
            ExposedEvents["set_content"] = SetContent;
            ExposedEvents["get_content"] = GetContent;

           
        }

        private async void __bg_threadLoop()
        {
            DateTime lastTime = DateTime.Now;

            while (!Disposing)
            {
                if (deferredJobs.TryDequeue(out var job))
                {
                    job?.Invoke();
                    lastTime = DateTime.Now;
                } 
                else
                {
                    await Task.Delay(1).ConfigureAwait(false);

                    // kill thread if no jobs for 30 seconds
                    if (deferredJobs.IsEmpty && (DateTime.Now - lastTime).TotalSeconds > 10)
                    {
                        ReleaseThread();
                        bgThread = null;
                    }


                }
            }
        }

        public void deferEval(string code, int delay, string? identifier = null)
        {
            WakeUpBackgroundThread();

            deferredJobs.Enqueue(async () =>
            {
                await Task.Delay(delay);

                var proc = Computer.GetProcess(id);

                var engine = proc?.UI?.JavaScriptEngine;

                // for command line apps.
                if (proc is null || engine is null)
                    engine = Computer.Current.JavaScript;
                
                if (identifier != null)
                    await engine.Execute($"{identifier} = {code}");
                else
                    _ = await engine.Execute(code);
            });
        }

        public void defer(string methodName, int delayMs, params object[]? args)
        {
            WakeUpBackgroundThread();

            deferredJobs.Enqueue(async () =>
            {
                

                await Task.Delay(delayMs).ConfigureAwait(true);

                if (GetProcess(id) is not Process p)
                {
                    Notifications.Now($"Failed to defer {methodName} because the process was not found.");
                    return;
                }

                var engine = p.UI?.JavaScriptEngine;

                

                var callHandle = $"{id}.{methodName}";

                if (engine is null || engine.Disposing)
                    return;

                try
                {

                    if (engine.m_engine_internal.Evaluate<bool>($"{callHandle} === undefined"))
                    {
                        Notifications.Now($"Failed to defer {methodName} because it was not found.");
                        return;
                    }

                    if (args.Length > 0)
                    {
                        var argsString = string.Join(", ", args);
                        engine.m_engine_internal.Evaluate($"{callHandle}({argsString})");
                    }
                    else
                    {
                        engine.m_engine_internal.Evaluate($"{callHandle}()");
                    }
                }
                catch( Exception e)
                {
                    Notifications.Exception(e);
                }
            });
        }

        private void WakeUpBackgroundThread()
        {
            if (bgThread == null)
            {
                Disposing = false;
                bgThread = new(__bg_threadLoop);
                bgThread.Start();
            }
        }

        internal static void SetProperty(object target, string propertyName, object? value)
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
        internal static object? GetProperty(object target, string propertyName)
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
                var userControl = GetUserContent();
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
        public UserControl? GetUserContent()
        {
            var window = Computer.GetProcess(id)?.UI;

            if (window != null)
            {
                var frame = window.ContentsFrame;
                if (frame.Content is UserControl userContent)
                    return userContent;
            }

            return null;
        }
        internal static FrameworkElement? FindControl(UserControl userControl, string controlName)
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
        internal static FrameworkElement? SearchVisualTree(object element, string controlName)
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
                var userControl = GetUserContent();

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
                var control = GetUserContent();

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
                var control = GetUserContent();
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

                    byte[] pixelData = [b, g, r, a];
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
                var userControl = GetUserContent();
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
                var userControl = GetUserContent();
                var control = FindControl(userControl, controlName);

                if (control is null)
                    return;

                SetProperty(control, property as string, value);
            }));

        }

        public object? pushEvent(string targetControl, string eventType, object? data)
        {
            if (ExposedEvents.TryGetValue(eventType, out var handler))
                return handler.Invoke(targetControl, data);
            return null;
        }
        public void eventHandler(string targetControl, string methodName, int type)
        {
            if (Computer.GetProcess(id) is Process p)
                Task.Run(async () => await p.UI.JavaScriptEngine?.CreateEventHandler(id, targetControl, methodName, type));
        }
        public void close(string pid)
        {
            Computer.Current.CloseApp(pid);
        }
        public string start(string path, params object[] args)
        {
            string pid = "PROC_START_FAILURE";

            Computer.Current.Window.Dispatcher.Invoke(start_app);

            async void start_app()
            {
                await Computer.Current.OpenCustom(path, args).ConfigureAwait(false);

                // this way of fetching a pid is very presumptuous and bad.
                pid = $"p{__procId}"; // the last created process. 
            }

            return pid;
        }
        public void loadApps(object? path)
        {
            string directory = FileSystem.Root;

            // search from provided path or if null, search from root
            if (path is string pathString && !string.IsNullOrEmpty(pathString))
                directory = pathString;

            if (FileSystem.GetResourcePath(directory) is string AbsPath && Directory.Exists(AbsPath))
            {
                Action<string, string> procDir = (root, file) =>
                {
                    try
                    {
                        if (Path.GetExtension(file) is string ext && ext == ".app")
                            Computer.Current.InstallJSWPF(Path.GetFileName(file));
                        if (Path.GetExtension(file) is string _ext && _ext == ".web")
                            Computer.Current.InstallJSHTML(Path.GetFileName(file));

                    }
                    catch
                    {
                        Notifications.Now($"Failed to install {file}");
                    }
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

        internal void __Attach__Process__ID(string id)
        {
            this.id = id;
        }
    }
}

