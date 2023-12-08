using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lemur.Windowing;
using Lemur.FS;
using Key = System.Windows.Input.Key;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lemur.GUI
{
    public enum AppType
    {
        JsXaml,
        NativeCs,
        JsHtml,
    }
    public partial class ComputerWindow : Window, IDisposable
    {
        private Timer clock;
        public bool Disposing;

        public static event Action<Key, bool> OnKeyDown;

        public int TopMostZIndex { get; internal set; }
        public ComputerWindow()
        {
            InitializeComponent();

            desktopBackground.Source = LoadImage(FileSystem.GetResourcePath("Background.png") ?? "background.png");

            Keyboard.AddPreviewKeyDownHandler(this, Computer_KeyDown);

            // what a horrible hack, i have no idea why this is needed
            // we can't get events for the desktop
            Keyboard.AddPreviewKeyDownHandler(Desktop, Computer_KeyDown);
            
            clock = new Timer(delegate
            {
                Dispatcher.Invoke(this.UpdateComputerTime);
            });

            clock.Change(0, TimeSpan.FromSeconds(10).Milliseconds);

        }


        public Button MakeButton(double width = double.NaN, double height = double.NaN)
        {
            var btn = new Button()
            {
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

            string regexPattern = @"[_a-zA-Z][_a-zA-Z0-9]*"; 

            // Validate the input with the regex
            string[] splitName = appName.Split(".");
            if (splitName.Length > 0)
            {
                string validName = splitName[0];
                if (Regex.IsMatch(validName, regexPattern))
                {
                    btn.Name = validName;
                }
                else
                {
                    Notifications.Now("Invalid application names");
                }
            }
            else
            {
                Notifications.Now("Invalid application names");
            }

            return btn;
        }
        public Button MakeTaskbarButton(string pID, string title, Action Toggle)
        {
            var btn = MakeButton(width: Math.Max(100, 8 * title.Length));

            btn.BorderBrush = Brushes.Gray;

            var ctx = new ContextMenu();
            btn.ContextMenu = ctx;

            MenuItem close = new()
            {
                Header = "close app"
            };

            close.Click += Close_Click;

            MenuItem toggle = new()
            {
                Header = "Minimize/Maximize"
            };
            toggle.Click += (_, _) => Toggle?.Invoke();

            void Close_Click(object sender, RoutedEventArgs e)
            {
                Computer.Current.CloseApp(pID);
                TaskbarStackPanel.Children.Remove(btn);
            }

            ctx.Items.Add(close);
            ctx.Items.Add(toggle);

            btn.Content = title;
            btn.Click += (_,_) => Toggle?.Invoke();
            return btn;
        }


        private int ctrlTabIndex;
        public void Computer_KeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown?.Invoke(e.Key, e.IsDown);

            // -- to users: --
            // add any global hotkeys here.
            // js already has support for fetching them though.
            switch (e.Key)
            {
                case Key.OemTilde:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var cmd = new CommandPrompt();
                        Computer.Current.OpenApp(cmd, "Cmd", Computer.GetNextProcessID());
                    }
                    break;

                case Key.Tab:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        if (Computer.Current.UserWindows.Count == 0)
                            return;

                        ctrlTabIndex = Math.Clamp(ctrlTabIndex, 0, Computer.Current.UserWindows.Count - 1);
                        
                        var windowElement = Computer.Current.UserWindows.ElementAt(ctrlTabIndex);
                        
                        ctrlTabIndex += 1;

                        if (ctrlTabIndex > Computer.Current.UserWindows.Count - 1)
                            ctrlTabIndex = 0;

                        var ownerWindow = windowElement.Value?.Owner;
                        ownerWindow?.BringToTopOfDesktop();
                    }
                    break;
            }
        }
        
        public void ShutdownClick(object sender, RoutedEventArgs e)
        {
            if (Computer.ProcessLookupTable.Count > 0)
            {
                var answer = MessageBox.Show("Are you sure you want to shut down? all unsaved changes will be lost.", 
                                            "Shutdown", 
                                            MessageBoxButton.YesNoCancel, 
                                            MessageBoxImage.Warning);
                if (answer == MessageBoxResult.Yes)
                {
                    App.Current.Shutdown();
                    Close();
                }
                return;
            }

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
        public static void AssignComputer(object instance, ResizableWindow? resizableWindow = null)
        {
            var method = instance.GetType().GetMethods()
             .FirstOrDefault(method =>
                 method.Name.Contains("LateInit") &&
                 
                 ((method.GetParameters().Length > 0 && method.GetParameters()[0].ParameterType == typeof(Computer)) ||

                 method.GetParameters().Length > 1 &&
                 method.GetParameters()[0].ParameterType == typeof(Computer) ||
                 method.GetParameters()[1].ParameterType == typeof(ResizableWindow))
             );

            var parameters = method.GetParameters();

            method?.Invoke(instance, parameters.Length == 1
                ? new[] { Computer.Current }
                : new object[] { Computer.Current, resizableWindow }
            );

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
        internal UserWindow OpenAppUI(string pID, string appdotapp, out ResizableWindow resizableWindow)
        {
            TopMostZIndex++;

            var window = new UserWindow();

            // TODO: add a way for users to add buttons and toolbars easily through
            // their js code, that would be very helpful.

            // does the resizing, moving, closing, minimizing, windowing stuff.
            resizableWindow = new ResizableWindow()
            {
                Content = window,
                Width=200,
                Height=200,
                Margin = window.Margin,
            };

            Button btn = MakeTaskbarButton(pID, appdotapp, resizableWindow.ToggleVisibility);

            TaskbarStackPanel.Children.Add(btn);
            Desktop.Children.Add(resizableWindow);

            return window;
        }

        // Todo: we shouldn't be relying on the button being present to determine whether an app is installed or not lol.
        internal void RemoveDesktopIcon(string name)
        {

            Dispatcher.Invoke(() =>
            {
                List<Button> toRemove = new();

                foreach (var item in DesktopIconPanel.Children)
                {
                    if (item is not Button btn)
                        continue;

                    string id = "_not_an_app_name";

                    if (btn.Content is string nam)
                        id = nam;
                    else
                        id = btn.Name;


                    name = name.Replace(".app", "").Replace(".web", "").ToLower();

                    id = id.Replace(".app", "").Replace(".web", "").ToLower();

                    if (name == id)
                        toRemove.Add(btn);
                }

                foreach (var btn in toRemove)
                    DesktopIconPanel.Children.Remove(btn);

           });
        }
        public void InstallIcon(AppType type, string appName, Type? runtime_type = null)
        {
            void InstallJSWPFIcon(Button btn, string type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;

                var contextMenu = new ContextMenu();

                MenuItem jsSource = new() {
                    Header = "view source : JavaScript",
                };

                jsSource.Click += (sender, @event) => JsSource_Click(sender, @event, appName);

                MenuItem xamlSource = new() {
                    Header = "view source : XAML",
                };

                xamlSource.Click += (sender, @event) => XamlSource_Click(sender, @event, appName);

                

                contextMenu.Items.Add(jsSource);
                contextMenu.Items.Add(xamlSource);

                btn.ContextMenu = contextMenu;

                async void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                  => await Computer.Current.OpenCustom(type);

                SetupIcon(type, btn);
            }
            void WebAppDesktopIcon(Button btn, string type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    var app = new UserWebApplet
                    {
                        Path = type.Replace(".web", "")
                    };
                    Computer.Current.OpenApp(app, type, Computer.GetNextProcessID());
                }

                SetupIcon(type, btn);
            }
            void InstallDesktopIconNative(Button btn, string name, Type type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;
                SetupIcon(appName, btn, type);

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    if (Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                    {
                        Computer.Current.OpenApp(userControl, name, Computer.GetNextProcessID());
                    } else
                    {
                        Notifications.Now("Failed to create instance of native application. the app is likely misconfigured");
                    }
                }
            }

            Dispatcher?.Invoke(() => {

                var btn = MakeDesktopButton(appName);

                switch (type)
                {
                    case AppType.JsXaml:
                        InstallJSWPFIcon(btn, appName);
                        break;
                    case AppType.NativeCs:
                        if (runtime_type != null)
                        InstallDesktopIconNative(btn, appName, runtime_type);
                        break;
                    case AppType.JsHtml:
                        WebAppDesktopIcon(btn, appName);
                        break;
                }

                MenuItem uninstall = new()
                {
                    Header = "uninstall app"
                };

                uninstall.Click += (sender, @event) =>
                {
                    var answer = MessageBox.Show($"are you sure you want to uninstall {appName}?", "uninstall?", MessageBoxButton.YesNo);

                    if (answer == MessageBoxResult.Yes)
                        Computer.Current.Uninstall(appName);
                };
                btn.ContextMenu ??= new();
                btn.ContextMenu.Items.Add(uninstall);

                DesktopIconPanel.UpdateLayout();
                DesktopIconPanel.Children.Add(btn);
            });

        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void XamlSource_Click(object sender, RoutedEventArgs e, string appName)
        {
            var name = appName.Replace(".app", ".xaml");
            var editor = new TextEditor(name);
            Computer.Current.OpenApp(editor, $"{appName}.xaml", Computer.GetNextProcessID());
        }

        private void JsSource_Click(object sender, RoutedEventArgs e, string appName)
        {
            var name = appName.Replace(".app", ".xaml.js");
            var editor = new TextEditor(name);
            Computer.Current.OpenApp(editor, $"{appName}.xaml.js", Computer.GetNextProcessID());
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
                    clock.Dispose();
                    Content = null;
                    Close();
                }

                Disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void ClearNotificaionsClicked(object sender, RoutedEventArgs e)
        {
            Notifications.Clear();
        }
    }
}
