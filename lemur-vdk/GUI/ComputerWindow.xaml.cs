using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lemur.FS;
using Lemur.JS;
using Key = System.Windows.Input.Key;

namespace Lemur.GUI
{
    public enum AppType
    {
        JAVASCRIPT_XAML_WPF,
        CSHARP_XAML_WPF_NATIVE,
        JS_HTML_WEB_APPLET,
    }
    public partial class ComputerWindow : Window, IDisposable
    {
        public Computer Computer;
        public bool Disposing;

        public int TopMostZIndex { get; internal set; } = 0;
        public ComputerWindow(Computer pc)
        {
            InitializeComponent();
            
            desktopBackground.Source = LoadImage(FileSystem.GetResourcePath("Background.png") ?? "background.png");
            
            Keyboard.AddKeyDownHandler(this, Computer_KeyDown);
            
            Computer = pc;
            
            CompositionTarget.Rendering += (e, o) => UpdateComputerTime();
        }
        public Button MakeButton(double width = double.NaN, double height = double.NaN)
        {
            var btn = new Button()
            {
                Background = Computer.theme.Background,
                BorderBrush = Computer.theme.Border,
                BorderThickness = Computer.theme.BorderThickness,
                FontFamily = Computer.theme.Font,
                FontSize = Computer.theme.FontSize,
                Width = width,
                Height = height,
            };
            return btn;
        }
        public Button MakeDesktopButton(string appName)
        {
            var btn = MakeButton(width: 60, height: 60);
            
            btn.Margin = new Thickness(15, 15, 15, 15);
            btn.Content = appName;
            btn.Name = appName.Split(".")[0];
            return btn;
        }
        public Button MakeTaskbarButton(string title, Action Toggle)
        {
            var btn = MakeButton(width: 65);

            btn.Content = title;
            btn.Click += (_,_) => Toggle?.Invoke();
            return btn;
        }
        public void Computer_KeyDown(object sender, KeyEventArgs e)
        {
            foreach (var userWindow in Computer.Windows)
            {
                var handlers = userWindow.Value?.JavaScriptEngine?.EventHandlers;

                if (handlers == null || !handlers.Any())
                    continue;

                var js_handlers = handlers.OfType<InteropEvent>();

                foreach (var eventHandler in js_handlers)
                    InvokeKeyEvent(sender, e, eventHandler);

            }

            switch (e.Key)
            {
                case Key.OemTilde:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var cmd = new CommandPrompt();
                        Computer.OpenApp(cmd, "Cmd");
                        cmd.LateInit(Computer);
                    }
                    break;
            }
        }
        public static void InvokeKeyEvent(object sender, KeyEventArgs e, InteropEvent eventHandler)
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
        public void ShutdownClick(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
            Close();
        }
        public void RemoveTaskbarButton(string title)
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
        public void UpdateComputerTime()
        {
            DateTime now = DateTime.Now;
            string formattedDateTime = now.ToString("MM/dd/yy || h:mm");
            TimeLabel.Content = formattedDateTime;
        }

        public static void SetupIcon(string name, Button btn)
        {
            var theme = Computer.Current.theme;
            
            if (Runtime.GetAppIcon(name) is BitmapImage img)
            {
                btn.Background = new ImageBrush(img);
                var contentBorder = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    ToolTip = name,
                };
                btn.Content = contentBorder;    
            }
            else
            {
                btn.Content = name;
            }

            btn.Margin = theme.BorderThickness;
        }

        public static void SetupIcon(string name, Button btn, Type type) 
        {
            if (GetIcon(type) is BitmapImage img)
            {
                btn.Background = new ImageBrush(img);

                var contentBorder = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    ToolTip = name,
                };

                btn.Content = contentBorder;
            } 
            else
            {
                Notifications.Now("Failed to get image for native app : make sure you have a 'public static string? DesktopIcon => FileSystem.GetResourcePath(\"commandprompt.png\"); type/name/accessible field' in your .xaml.cs class");
            }

            btn.Margin = Computer.Current.theme.BorderThickness;

        }
        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            return bitmapImage;
        }
        public static BitmapImage? GetIcon(Type type) 
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
        public static void AssignComputer(object instance, Computer computer)
        {
            var method = instance.GetType().GetMethods().FirstOrDefault(method => method.Name.Contains("LateInit") &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == typeof(Computer));

            if (method != null)
                method.Invoke(instance, new[] { computer });
        }
        /// <summary>
        /// we rely on this <code>('public void LateInit(Computer pc){..}') </code>method being declared in the UserControl to attach the OS to the app
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        /// 
        public static bool IsValidType(MemberInfo[] members)
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
        internal UserWindow OpenAppUI(string title, ref Brush? background, ref Brush? foreground, out ResizableWindow resizableWindow)
        {
            TopMostZIndex++;

            background ??= Computer.theme.Background;
            foreground ??= Computer.theme.Foreground;

            // hosts the user content and it's utilities
            var window = new UserWindow
            {
                Background = background,
                Foreground = foreground,
                FontFamily = Computer.theme.Font,
                FontSize = Computer.theme.FontSize,
                BorderThickness = Computer.theme.BorderThickness,
            };

            // TODO: add a way for users to add buttons and toolbars easily through
            // their js code, that would be very helpful.

            // does the resizing, moving, closing, minimizing, windowing stuff.
            resizableWindow = new ResizableWindow()
            {
                Content = window,
                Width=200,
                Height=200,
                Margin = window.Margin,
                Background = background,
                Foreground = foreground,

                FontFamily = Computer.theme.Font,
                FontSize = Computer.theme.FontSize,
                BorderThickness = Computer.theme.BorderThickness,
            };

            // hacky ::
            // declaring this field as a hack, to capture the object in the lambda function below.
            // cant capture an out var, but it's ok here, actually crucial to prevent a leak.
            var rsz_win_capture = resizableWindow;
            // hack ::

            Button btn = MakeTaskbarButton(title, resizableWindow.ToggleVisibility);

            TaskbarStackPanel.Children.Add(btn);
            Desktop.Children.Add(resizableWindow);

            window.OnClosed += () =>
            {
                Desktop.Children.Remove(rsz_win_capture);
                Computer?.Windows.Remove(title);
                RemoveTaskbarButton(title);
            };
            return window;
        }

        // Todo: we shouldn't be relying on the button being present to determine whether an app is installed or not lol.
        internal void RemoveDesktopIcon(string name)
        {
            System.Collections.IList list = DesktopIconPanel.Children;

            for (int i = 0; i < list.Count; i++)
            {
                object? item = list[i];

                if (item is Button btn)
                {
                    btn.Dispatcher.Invoke(() =>
                    {
                        if (btn.Name == name.Replace(".app", "").Replace(".web", ""))
                        {
                            DesktopIconPanel.Children.Remove(btn);
                        }
                    });
                }
            }
        }
        public void InstallIcon(AppType type, string name, Type? runtime_type = null)
        {
            void InstallJSWPFIcon(string type)
            {
                var btn = MakeDesktopButton(type);

                btn.MouseDoubleClick += OnDesktopIconPressed;

                async void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                 => await Computer.OpenCustom(type);

                SetupIcon(type, btn);

                DesktopIconPanel.Children.Add(btn);
            }
            void WebAppDesktopIcon(string type)
            {
                var btn = MakeDesktopButton(type);
                btn.MouseDoubleClick += OnDesktopIconPressed;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    var app = new UserWebApplet
                    {
                        Path = type.Replace(".web", "")
                    };
                    Computer.OpenApp(app);
                }

                SetupIcon(type, btn);

                DesktopIconPanel.Children.Add(btn);
            }
            void InstallDesktopIconNative(string name, Type type)
            {
                var btn = MakeDesktopButton(name);

                btn.MouseDoubleClick += OnDesktopIconPressed;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    var members = type.GetMethods();
                    if (Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                    {
                        if (IsValidType(members))
                            AssignComputer(instance, Computer);
                        Computer.OpenApp(userControl, name);
                    } else
                    {
                        Notifications.Now("Failed to create instance of native application. the app is likely misconfigured");
                    }
                }

                SetupIcon(name, btn, type);
                    
                DesktopIconPanel.Children.Add(btn);
            }
            Dispatcher?.Invoke(() => { 

                switch (type)
                {
                    case AppType.JAVASCRIPT_XAML_WPF:
                        InstallJSWPFIcon(name);
                        break;
                    case AppType.CSHARP_XAML_WPF_NATIVE:
                        if (runtime_type != null)
                        InstallDesktopIconNative(name, runtime_type);
                        break;
                    case AppType.JS_HTML_WEB_APPLET:
                        WebAppDesktopIcon(name);
                        break;
                }

                DesktopIconPanel.UpdateLayout();
            });
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    Desktop.Children.Clear();
                    DesktopIconPanel.Children.Clear();
                    Taskbar.Children.Clear();
                    TaskbarStackPanel.Children.Clear();
                    Content = null;
                    Computer = null!;
                    this.Close();
                }

                Disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
