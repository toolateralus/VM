using Lemur.FS;
using Lemur.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Key = System.Windows.Input.Key;

namespace Lemur.GUI
{
    public enum AppType
    {
        Native,
        Extern,
    }
    /// <summary>
    /// The 'MainWindow' for the computer, while not being the main application window.
    /// The full screen window that appears as your computer desktop monitor.
    /// Contains a few methods for generating WPF Controls at runtime, and 
    /// managing the basic UI events.
    /// </summary>
    public partial class DesktopWindow : Window, IDisposable
    {
        public bool Disposing;

        public static event Action<Key, bool> OnKeyDown;
        public int TopMostZIndex { get; internal set; }
        private int ctrlTabIndex;

        Computer computer;
        public DesktopWindow(Computer computer)
        {
            this.computer = computer;

            InitializeComponent();

            //desktopBackground.Source = Computer.LoadImage();

            Keyboard.AddPreviewKeyDownHandler(this, Computer_KeyDown);

            // what a horrible hack, i have no idea why this is needed
            // we can't get events for the desktop
            Keyboard.AddPreviewKeyDownHandler(App.Current.MainWindow, Computer_KeyDown);


            var startTime = DateTime.Now;
            var startupTime = startTime.ToString("\"MM/dd/yy || h:mm:ss\"");

            TimeLabel.ToolTip = "";
            TimeLabel.ToolTipOpening += TimeLabel_ToolTipOpening;

            void TimeLabel_ToolTipOpening(object sender, ToolTipEventArgs e)
            {
                TimeLabel.ToolTip = $"startup time : {startupTime}\ncurrent time : {DateTime.Now.ToString("\"MM/dd/yy || h:mm:ss\"")}\nsession duration : {Math.Floor((DateTime.Now - startTime).TotalMinutes)} minutes";
            }
            CompositionTarget.Rendering += delegate
            {
                DateTime now = DateTime.Now;
                string formattedDateTime = now.ToString("MM/dd/yy || h:mm:ss");
                

                TimeLabel.Content = formattedDateTime;
            };

        }
        internal ContextMenu GetNativeContextMenu(string appName, AppConfig? config = null)
        {
            var contextMenu = new ContextMenu();

            // judges the file extension for JsSource_Click & whether to create a XAML source view button.
            var isTerminal = config?.terminal ?? false;

            MenuItem jsSource = new()
            {
                Header = "source -> javascript",
            };

            jsSource.Click += (sender, @event) =>
            {
                JsSource_Click(sender, @event, appName, isTerminal);
            };
            contextMenu.Items.Add(jsSource);

            // for gui apps, view XAML source button.
            if (!isTerminal || config?.isWpf == true)
            {
                MenuItem xamlSource = new()
                {
                    Header = "source -> xaml",
                };
                xamlSource.Click += (sender, @event) =>
                {
                    XamlSource_Click(sender, @event, appName);
                };
                contextMenu.Items.Add(xamlSource);

            }

            // view .appconfig button
            if (config is not null)
            {
                MenuItem configMenu = new()
                {
                    Header = "source -> config",
                };
                configMenu.Click += delegate
                {
                    string name = appName + ".appconfig";
                    var editor = new Texed(name);
                    Computer.Current.OpenAppGUI(editor, name, computer.ProcessManager.GetNextProcessID());
                };
                contextMenu.Items.Add(configMenu);
            }

            MenuItem folder = new()
            {
                Header = "open containing folder",
            };

            folder.Click += (sender, @event) =>
            {
                var path = FileSystem.GetResourcePath(appName + ".app");
                FileSystem.ChangeDirectory(path);

                var explorer = new Explorer();
                var pid = Computer.Current.ProcessManager.GetNextProcessID();
                Computer.Current.OpenAppGUI(explorer, appName + ".app", pid);

            };
            contextMenu.Items.Add(folder);

            return contextMenu;
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
            var btn = MakeButton(width: 70, height: 70);

            btn.Margin = new Thickness(5, 5, 5, 5);
            btn.Content = appName;

            btn.Style = FindResource("DesktopButtonStyle") as Style; 

            string regexPattern = @"[_a-zA-Z][_azA-Z0-9]*";

            string[] splitName = appName.Split(".");
            if (splitName.Length > 0)
            {
                string validName = splitName[0];
                if (Regex.IsMatch(validName, regexPattern))
                    btn.Name = validName;
                else
                    Notifications.Now("Invalid application names");
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
                Header = "Close App"
            };

            close.Click += Close_Click;

            MenuItem toggle = new()
            {
                Header = "Minimize/Maximize"
            };
            toggle.Click += (_, _) => Toggle?.Invoke();

            void Close_Click(object sender, RoutedEventArgs e)
            {
                Computer.Current.ProcessManager.TerminateProcess(pID);
                TaskbarStackPanel.Children.Remove(btn);
            }

            ctx.Items.Add(close);
            ctx.Items.Add(toggle);

            btn.Content = title;
            btn.Click += (_, _) => Toggle?.Invoke();
            return btn;
        }
        internal UserWindow CreateWindow(string pID, string pClass, out ResizableWindow resizableWindow)
        {
            TopMostZIndex++;

            var window = new UserWindow(computer, pID);

            // TODO: add a way for users to add buttons and toolbars easily through
            // their js code, that would be very helpful.

            // does the resizing, moving, closing, minimizing, windowing stuff.
            resizableWindow = new ResizableWindow
            {
                Content = window,
                Width = 200,
                Height = 200,
                Margin = window.Margin,
            };

            // really hacky silly capture here.
            var w = resizableWindow;


            window.Title.Content = pClass;

            Button btn = MakeTaskbarButton(pID, pClass, resizableWindow.ToggleVisibility);

            TaskbarStackPanel.Children.Add(btn);
            Desktop.Children.Add(resizableWindow);

            return window;
        }
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


                    name = name.Replace(".app", "").ToLower();

                    id = id.Replace(".app", "").ToLower();

                    if (name == id)
                        toRemove.Add(btn);
                }

                foreach (var btn in toRemove)
                    DesktopIconPanel.Children.Remove(btn);

            });
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
        private void ClearNotificaionsClicked(object sender, RoutedEventArgs e)
        {
            Notifications.Clear();
        }
        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void XamlSource_Click(object? sender, RoutedEventArgs e, string appName)
        {
            var name = appName + ".xaml";
            var editor = new Texed(name);
            Computer.Current.OpenAppGUI(editor, name, computer.ProcessManager.GetNextProcessID());
        }
        private void JsSource_Click(object? sender, RoutedEventArgs e, string appName, bool isTerminal)
        {
            string name;
            if (isTerminal)
                name = appName + ".js";
            else
                name = appName + ".xaml.js";

            var editor = new Texed(name);
            Computer.Current.OpenAppGUI(editor, name, computer.ProcessManager.GetNextProcessID());
        }
        public void Computer_KeyDown(object sender, KeyEventArgs e)
        {
            // -- to users: --
            // add any global hotkeys here.
            // js already has support for fetching them though.
            switch (e.Key)
            {
                case Key.OemTilde:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        var cmd = new Terminal();
                        Computer.Current.OpenAppGUI(cmd, "Cmd", computer.ProcessManager.GetNextProcessID());
                    }
                    break;

                case Key.Tab:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {

                        // todo : make a function to do this.
                        var windows = computer.ProcessManager.ProcessClassTable.Values.SelectMany(i => i).ToList();

                        if (windows.Count == 0)
                            return;

                        ctrlTabIndex = Math.Clamp(ctrlTabIndex, 0, windows.Count - 1);

                        var windowElement = windows.ElementAt(ctrlTabIndex);

                        ctrlTabIndex += 1;

                        if (ctrlTabIndex > windows.Count - 1)
                            ctrlTabIndex = 0;

                        var ownerWindow = windowElement?.UI?.ResizableParent;
                        ownerWindow?.BringIntoViewAndToTop();
                    }
                    break;
            }
        }
        public void ShutdownClick(object sender, RoutedEventArgs e)
        {
            if (computer.ProcessManager.ProcessClassTable.Count > 0)
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
        public void Dispose()
        {
            if (!Disposing)
            {
                Desktop.Children.Clear();
                DesktopIconPanel.Children.Clear();
                Taskbar.Children.Clear();
                TaskbarStackPanel.Children.Clear();
                Content = null;
                Close();
                Disposing = true;
            }
        }
    }
}
