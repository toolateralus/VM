using Microsoft.ClearScript.V8;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using VM;
using VM;
using VM.JS;
using VM.UserInterface;

namespace VM.GUI
{
    public partial class ComputerWindow : Window
    {
        public Computer Computer;
        public readonly List<string> LoadedInstalledApplications = new();
        public readonly Dictionary<string, UserWindow> USER_WINDOW_INSTANCES = new();
        public int TopMostZIndex { get; internal set; } = 0;
        public ComputerWindow(Computer pc)
        {
            InitializeComponent();
            desktopBackground.Source = LoadImage(Runtime.GetResourcePath("Background.png") ?? "background.png");
            Keyboard.AddKeyDownHandler(this, Computer_KeyDown);
            Computer = pc;
            Closing += OnClosingCustom;
            IDLabel.Content = $"computer {Computer.ID}";
            CompositionTarget.Rendering += (e, o) => UpdateComputerTime();

        }
        private async void OnClosingCustom(object? sender, CancelEventArgs e)
        {
            foreach (var item in USER_WINDOW_INSTANCES)
                item.Value.Close();

            Dispatcher.BeginInvokeShutdown(System.Windows.Threading.DispatcherPriority.Normal);

            while (!Dispatcher.HasShutdownFinished)
                await Task.Delay(1);
        }
        private void Computer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            foreach (var userWindow in Computer.Window.USER_WINDOW_INSTANCES)
            {
                if (userWindow.Value?.JavaScriptEngine?.EventHandlers == null)
                    continue;

                foreach (var eventHandler in userWindow.Value.JavaScriptEngine.EventHandlers)
                {
                    InvokeKeyEvent(sender, e, eventHandler);
                }
            }

            switch (e.Key)
            {
                case Key.OemTilde:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var cmd = new CommandPrompt();
                        OpenApp(cmd, "Cmd");
                        cmd.LateInit(Computer);
                    }
                    break;
            }
        }
        private static void InvokeKeyEvent(object sender, KeyEventArgs e, JSEventHandler eventHandler)
        {
            if (eventHandler.Event == XAML_EVENTS.KEY_DOWN)
            {
                eventHandler.OnKeyDown?.Invoke(sender, e);
            }
            if (eventHandler.Event == XAML_EVENTS.KEY_UP)
            {
                eventHandler.OnKeyUp?.Invoke(sender, e);
            }
        }
        private void ShutdownClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
            Close();
        }
        private void RemoveTaskbarButton(string title)
        {
            System.Collections.IList list = TaskbarStackPanel.Children;
            for (int i = 0; i < list.Count; i++)
            {
                object? item = list[i];
                if (item is Button button && button.Content as string == title)
                {
                    TaskbarStackPanel.Children.Remove(button);
                    break;
                }
            }
        }
        private Button GetOSThemeButton(double width = double.NaN, double height = double.NaN)
        {
            var btn = new Button()
            {
                Background = Computer.Theme.Background,
                BorderBrush = Computer.Theme.Border,
                BorderThickness = Computer.Theme.BorderThickness,
                FontFamily = Computer.Theme.Font,
                FontSize = Computer.Theme.FontSize,
                Width = width,
                Height = height,
            };
            return btn;
        }
        private Button GetDesktopIconButton(string appName)
        {
            var btn = GetOSThemeButton(width: 60, height: 60);

            btn.Margin = new Thickness(15, 15, 15, 15);
            btn.Content = appName;
            btn.Name = appName.Split(".")[0];
            return btn;
        }
        private Button GetTaskbarButton(string title, RoutedEventHandler Toggle)
        {
            var btn = GetOSThemeButton(width: 65);

            btn.Content = title;
            btn.Click += Toggle;
            return btn;
        }
        private void UpdateComputerTime()
        {
            DateTime now = DateTime.Now;
            string formattedDateTime = now.ToString("MM/dd/yy || h:mm");
            TimeLabel.Content = formattedDateTime;
        }
        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            return bitmapImage;
        }
        private static void SetupIcon(string name, Button btn, Type type) 
        {
            if (GetIcon(type) is BitmapImage img)
            {
                btn.Background = new ImageBrush(img);
            }

            btn.Margin = new Thickness(15, 15, 15, 15);

            var contentBorder = new Border
            {
                Background = new ImageBrush(GetIcon(type)),
                CornerRadius = new CornerRadius(10),
                ToolTip = name,
            };

            btn.Content = contentBorder;
        }
        private static BitmapImage? GetIcon(Type type) 
        {
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                if (property.Name.Contains("DesktopIcon") &&
                    property.PropertyType == typeof(string) &&
                    property.GetValue(null) is string path &&
                    !string.IsNullOrEmpty(path))
                {
                    return LoadImage(path);
                }
            }

            return null;
        }
        /// <summary>
        /// performs init on LateInit method, explaied in tooltipf or IsValidType (a static method in this class)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="computer"></param>
        private static void AssignComputer(object instance, Computer computer)
        {
            var methods = instance.GetType().GetMethods();

            foreach (var method in methods)
            {
                if (method.Name.Contains("LateInit") &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(Computer))
                {
                    method.Invoke(instance, new[] { computer });
                }
            }
        }
        /// <summary>
        /// we rely on this <code>('public void LateInit(Computer pc){..}') </code>method being declared in the UserControl to attach the OS to the app
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        /// 
        private static bool IsValidType(MemberInfo[] members)
        {
            foreach (var member in members)
            {
                if (member.Name.Contains("LateInit"))
                {
                    return true;
                }
            }
            return false;
        }
        public void OpenApp(UserControl control, string title = "window", Brush? background = null, Brush? foreground = null, JavaScriptEngine engine = null)
        {
            if (Computer.Disposing)
                return;

            // basically app count, a way for us to force to top.
            TopMostZIndex++;

            background ??= Computer.Theme.Background;
            foreground ??= Computer.Theme.Foreground;

            var window = new UserWindow
            {
                Background = background,
                Foreground = foreground,
            };

            var frame = new ResizableWindow(this)
            {
                Content = window,
                Width = Math.Max(window.MinWidth, window.Width),
                Height = Math.Max(window.MinHeight, window.Height),
                Margin = window.Margin,
                Background = window.Background,
                Foreground = window.Foreground,
            };

            window.InitializeUserContent(frame, control, engine);

            USER_WINDOW_INSTANCES[title] = window;

            Desktop.Children.Add(frame);

            window.ToggleMaximize(null, null);
            Button btn = GetTaskbarButton(title, window.ToggleVisibility);

            TaskbarStackPanel.Children.Add(btn);

            window.OnClosed += () =>
            {
                USER_WINDOW_INSTANCES.Remove(title);
                RemoveTaskbarButton(title);
            };
        }
        public void InstallJSWPF(string type)
        {
            if (Computer.Disposing)
                return;
            Dispatcher.Invoke(() => { 
                LoadedInstalledApplications.Add(type);

                var btn = GetDesktopIconButton(type);
              
                btn.MouseDoubleClick += OnDesktopIconPressed;

                async void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    await Computer.OpenCustom(type);
                }

                DesktopIconPanel.Children.Add(btn);
                DesktopIconPanel.UpdateLayout();
            
            });
        }
        public void InstallJSHTML(string type)
        {
            if (Computer.Disposing)
                return;

            Dispatcher.Invoke(() =>
            {
                LoadedInstalledApplications.Add(type);

                var btn = GetDesktopIconButton(type);

                btn.MouseDoubleClick += OnDesktopIconPressed;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    var app = new UserWebApplet();

                    // we add the appropriate extension within navigate.
                    app.Path = type.Replace(".web", "");
                    OpenApp(app);
                }

                DesktopIconPanel.Children.Add(btn);
                DesktopIconPanel.UpdateLayout();
            });

        }
        public void InstallCSWPF(string exePath, Type type)
        {
            var name = exePath.Split('.')[0];

            var btn = GetDesktopIconButton(name);

            btn.MouseDoubleClick += OnDesktopIconPressed;

            void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
            {
                var members = type.GetMethods();

                if (IsValidType(members) && Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                {
                    AssignComputer(instance, Computer);
                    OpenApp(userControl, name);
                }
            }

            SetupIcon(name, btn, type);

            DesktopIconPanel.Children.Add(btn);
            DesktopIconPanel.UpdateLayout();
        }
        public void Uninstall(string name)
        {
            Dispatcher.Invoke(() =>
            {
                LoadedInstalledApplications.Remove(name);

                System.Collections.IList list = DesktopIconPanel.Children;

                for (int i = 0; i < list.Count; i++)
                {
                    object? item = list[i];
                    if (item is Button btn && btn.Name == name.Replace(".app", "").Replace(".web", ""))
                    {
                        DesktopIconPanel.Children.Remove(btn);
                    }
                }
            });
        }

        public object taskManager = null;

        private void TaskManagerClick(object sender, RoutedEventArgs e)
        {
            if (taskManager != null)
                return;

            var grid = new Grid
            {
                Background = Brushes.MediumAquamarine,
                MinWidth = 150,
                MaxWidth = 300,
                MinHeight = 600,
                MaxHeight = 800,
                Margin = new(5, 5, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new(5, 5, 5, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };

            grid.Children.Add(stack);

            HashSet<object> existing = new();

            EventHandler UpdateTaskManager = new(async (_,_) =>
            {
                var apps = Computer.Window.USER_WINDOW_INSTANCES;
                foreach (var item in apps)
                {
                    if (!existing.Contains(item))
                    {
                        existing.Add(item);
                        var _btn = GetTaskbarButton(item.Key, (_, _) => {
                            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                                item.Value.Close();
                            else item.Value.ToggleMaximize(sender, e);
                        });
                        _btn.MinWidth = 100;
                        _btn.MinHeight = 50;
                        _btn.Margin = new(15, 15, 15, 15);
                        stack.Children.Add(_btn);
                    }
                }
            });

            Action onClose = new(delegate {
                Desktop?.Children?.Remove(grid);
                taskManager = null;
                CompositionTarget.Rendering -= UpdateTaskManager;
            });

            var btn = GetTaskbarButton("close", (_, _) =>
            {
                onClose();
            });

            stack.Children.Add(btn);

            taskManager = new();

            Desktop.Children.Add(grid);

            Canvas.SetZIndex(grid, TopMostZIndex);

            CompositionTarget.Rendering += UpdateTaskManager;
        }
    }
}
