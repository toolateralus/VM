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
using Image = System.Windows.Controls.Image;

namespace Lemur.JS.Embedded
{
    public class app_t : embedable
    {
        public app_t(Computer computer) : base(computer)
        {
            ExposedEvents["draw_pixels"] = DrawPixelsEvent; // somewhat deprecated, use the dedicated graphics module instead.
            ExposedEvents["draw_image"] = DrawImageEvent;
            ExposedEvents["set_content"] = SetContent;
            ExposedEvents["get_content"] = GetContent;
        }
        internal Thread bgThread;
        private ConcurrentQueue<Action> deferredJobs = [];
        internal string processID;
        private bool Disposing { get; set; }
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
        public void ReleaseThread()
        {
            if (!Disposing)
            {
                Disposing = true;
                bgThread?.Join();
            }
        }
        private async void __bg_threadLoop()
        {
            DateTime lastTime = DateTime.Now;

            while (!Disposing)
            {
                if (deferredJobs.TryDequeue(out var job))
                {
                    using var cts = new CancellationTokenSource();
                    var task = Task.Run(job.Invoke, cts.Token);
                    await Task.WhenAny(task, Task.Delay(100_000)).ConfigureAwait(false);
                    lastTime = DateTime.Now;
                } 
                else
                {
                    await Task.Delay(16).ConfigureAwait(false);

                    // kill thread if no jobs for 30 seconds
                    if (deferredJobs.IsEmpty && (DateTime.Now - lastTime).TotalSeconds > 10)
                    {
                        ReleaseThread();
                        bgThread = null;
                    }


                }
            }
        }
        public void setRow(string element, int row)
        {
            Current.Window.Dispatcher.Invoke(() =>
            {
                var control = FindControl(GetUserContent(), element);

                if (control is null)
                    return;

                Grid.SetRow(control, row);
            });
        }
        public void setColumn(string element, int row)
        {

            Current.Window.Dispatcher.Invoke(() => { 
                var control = FindControl(GetUserContent(), element);

                if (control is null)
                    return;

                Grid.SetColumn(control, row);
            });
        }
        public void setColumnSpan(string element, int row)
        {
            Current.Window.Dispatcher.Invoke(() =>
            {
                var control = FindControl(GetUserContent(), element);

                if (control is null)
                    return;

                Grid.SetColumnSpan(control, row);
            });
        }
        public void setRowSpan(string element, int row)
        {
            Current.Window.Dispatcher.Invoke(() =>
            {
                var control = FindControl(GetUserContent(), element);

                if (control is null)
                    return;

                Grid.SetRowSpan(control, row);
            });
        }
        public bool addChild(string parentName, string childTypeString, string childName)
        {
            var didAdd = false;

            childTypeString = "System.Windows.Controls." + childTypeString.Trim(); 

            Current.Window?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent();
                var parentControl = FindControl(userControl, parentName);

                if (parentControl == null)
                {
                    Notifications.Now($"{parentName} : control not found");
                    return;
                }

                var childrenProperty = parentControl.GetType().GetProperty("Children");
                if (childrenProperty == null)
                {
                    Notifications.Now($"{parentName} target parent had no children property");
                    return;
                }

                var children = childrenProperty.GetValue(parentControl) as System.Collections.IList;

                if (children == null) {
                    Notifications.Now("No 'Children' property found");
                    return;
                }

                Type? childType = typeof(Control).Assembly.DefinedTypes.FirstOrDefault(type => type.FullName.Contains(childTypeString));

                if (childType == null || !typeof(FrameworkElement).IsAssignableFrom(childType)) {
                    Notifications.Now($"Invalid type string provided for createChild {childType}");
                    return;
                }

                var newChild = Activator.CreateInstance(childType) as FrameworkElement;
                
                if (newChild == null) {
                    Notifications.Now("addChild child element was null on instantiation");
                    return;
                }

                newChild.Name = childName;
                children.Add(newChild);
                didAdd = children.Contains(newChild);
            });

            return didAdd;
        }
        public bool removeChild(string parent, string childName)
        {
            var didRemove = false;

            Current.Window?.Dispatcher.Invoke(() =>
            {
                var userControl = GetUserContent();
                var control = FindControl(userControl, parent);

                if (control == null)
                    return;

                var childrenProperty = control.GetType().GetProperty("Children");
                if (childrenProperty != null)
                {
                    var children = childrenProperty.GetValue(control) as System.Collections.IEnumerable;
                    if (children != null)
                    {
                        object toRemove = null;
                        foreach (var child in children)
                        {
                            if (child is FrameworkElement fwE && fwE.Name == childName)
                            {
                                toRemove = child;
                                break;
                            }
                        }

                        if (toRemove != null)
                        {
                            var removeMethod = children.GetType().GetMethod("Remove");
                            if (removeMethod != null)
                            {
                                removeMethod.Invoke(children, new[] { toRemove });
                                didRemove = !children.Cast<object>().Contains(toRemove);
                            }
                        }
                    }
                }
            });

            return didRemove;
        }
        public void deferEval(string code, int delay, string? identifier = null)
        {
            WakeUpBackgroundThread();

            deferredJobs.Enqueue((Action)(async () =>
            {
                await Task.Delay(delay);

                var computer = GetComputer();

                var proc = computer.ProcessManager.GetProcess((string)this.processID);

                var engine = proc?.UI?.Engine;

                // for command line apps.
                if (proc is null || engine is null)
                    engine = computer.JavaScript;

                if (identifier != null)
                    await engine.Execute($"{identifier} = {code}").ConfigureAwait(false);
                else
                    _ = await engine.Execute(code).ConfigureAwait(false) ;
            }));
        }
        public void defer(string methodName, int delayMs, params object[]? args)
        {
            WakeUpBackgroundThread();

            deferredJobs.Enqueue((Action)(async () =>
            {
                

                await Task.Delay(delayMs).ConfigureAwait(true);

                if (GetComputer().ProcessManager.GetProcess((string)this.processID) is not Process p)
                {
                    Notifications.Now($"Failed to defer {methodName} because the process was not found.");
                    return;
                }

                var engine = p.UI?.Engine;

                var callHandle = $"{this.processID}.{methodName}";

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
                catch(Exception e)
                {
                    Notifications.Exception(e);
                }
            }));
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

            try
            {
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
            catch (Exception e)
            {
                Notifications.Exception(e);
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

            GetComputer().Window?.Dispatcher.Invoke(() =>
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
            var window = GetComputer().ProcessManager.GetProcess(processID)?.UI;

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

            var wnd = GetComputer().Window;

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

            GetComputer().Window?.Dispatcher.Invoke(() =>
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

            GetComputer().Window?.Dispatcher.Invoke(() =>
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

            GetComputer().Window?.Dispatcher.Invoke(() =>
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

            GetComputer().Window?.Dispatcher.Invoke((Delegate)(() =>
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
            var pc = GetComputer();
            var procMgr = pc.ProcessManager;
            var proc = procMgr.GetProcess(processID);
            if (proc is Process p)
                Task.Run(async () => await procMgr.CreateEventHandler(proc.UI.Engine, processID, targetControl, methodName, type));
        }
        public void close(string pid)
        {
            GetComputer().ProcessManager.TerminateProcess(pid);
        }
        public string start(string path, params object[] args)
        {
            string pid = "PROC_START_FAILURE";

            GetComputer().Window.Dispatcher.Invoke(start_app);

            async void start_app()
            {
                // this way of fetching a pid is very presumptuous and bad.
                pid = $"p{__procId + 1}"; // the next to be created process. 
                GetComputer().OpenCustom(path, args);
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
                            GetComputer().InstallNative(Path.GetFileName(file));

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
            var t = "typeof<";
            if (dir.StartsWith(t))
            {
                dir = dir.Replace(t, string.Empty);
                dir = dir.Replace(">", string.Empty);

                Type type = Type.GetType(dir);
                Current.InstallFromType(dir, type);

            }


            if (dir.Contains(".app"))
            {
                GetComputer().InstallNative(dir);
            }
        }
        public void uninstall(string dir)
        {
            DesktopWindow window = GetComputer().Window;

            // js/html app
            if (dir.Contains(".web"))
            {
                GetComputer().Uninstall(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                GetComputer().Uninstall(dir);
                return;
            }

            Notifications.Now("Incorrect path for uninstall");

        }
        internal void __Attach__Process__ID(string id)
        {
            this.processID = id;
        }
    }
}

