using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.OS.Language;
using Lemur.Types;
using Lemur.Windowing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lemur
{
    public record Process(Computer computer, UserWindow UI, string ID, string Type)
    {
        public Action? OnProcessTermination { get; internal set; }
        public readonly Computer computer = computer;

        /// <summary>
        /// Anything that wants to close a process MUST call this method to do so.
        /// </summary>
        internal void Terminate()
        {
            // destroy event handlers mostly.
            OnProcessTermination?.Invoke();

            // close visual UI.
            UI.Close();

            // dispose of the js execution context
            UI.Engine?.Dispose();

            // TODO: put in process manager.
            // remove the process and or type from process table.
            var procList = computer.ProcessManager.ProcessClassTable[Type];
            procList.Remove(this);

            if (procList.Count == 0)
                computer.ProcessManager.ProcessClassTable.Remove(Type);
            else computer.ProcessManager.ProcessClassTable[Type] = procList; // unnecessary? probably.

        }
    }
    public class Computer : IDisposable
    {
        internal string WorkingDir { get; private set; }
        internal uint ID { get; private set; }
        internal static uint __procId;
        internal NetworkConfiguration Network { get; set; }
        internal DesktopWindow Window { get; set; }
        internal FileSystem FileSystem { get; set; }
        internal Engine JavaScript { get; set; }
        internal CommandLine CLI { get; set; }
        internal JObject Config { get; set; }

        private static Computer? current;
        private int startupTimeoutMs = 20_000;
        public static Computer Current => current;

        public required ProcessManager ProcessManager { get; init; }

        // type : process(es)
        internal readonly Dictionary<string, Type> csApps = [];
        internal readonly List<string> jsApps = new();
        internal bool disposing;
  
        public Computer(FileSystem fs)
        {
            current = this;

            Notifications.Current = this;

            FileSystem = fs;

            CLI = new(this);

            Network = new();

            Config = LoadConfig();

            JavaScript = new(this, "Computer");

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
                JavaScript.ExecuteScript(AbsPath);

        }
        internal void Exit(int exitCode)
        {
            if (exitCode != 0)
            {
                Notifications.Now($"Computer {ID} has exited, most likely due to an error. code:{exitCode}");
            }
            Dispose();

        }
        
        internal static JObject? LoadConfig()
        {
            if (FileSystem.GetResourcePath("config.json") is string AbsPath)
            {
                if (File.Exists(AbsPath))
                {
                    string json = File.ReadAllText(AbsPath);

                    try
                    {
                        return JObject.Parse(json);
                    }
                    catch (JsonException ex)
                    {
                        Notifications.Now($"Error loading JSON: {ex.Message}");
                    }
                }
                else
                {
                    Notifications.Now("Could not locate a valid 'config.json' File.");
                }
            }

            return null;
        }
        public static void SaveConfig(string config)
        {
            string configFilePath = FileSystem.GetResourcePath("config.json");

            if (!string.IsNullOrEmpty(configFilePath))
            {
                try
                {
                    File.WriteAllText(configFilePath, config);
                }
                catch (Exception ex)
                {
                    Notifications.Now($"Error saving JSON config: {ex.Message}");
                }
            }
        }
        
        public async void OpenCustom(string type, params object[] cmdLineArgs)
        {
            // todo: move this to the process manager.
            if (!type.Contains(".app"))
                type += ".app";

            var data = Runtime.GetAppDefinition(type);

            var control = XamlHelper.ParseUserControl(data.XAML);

            if (control == null)
            {
                if (csApps.TryGetValue(type, out var csType))
                {
                    OpenApp((UserControl)Activator.CreateInstance(csType, cmdLineArgs)!, type, ProcessManager.GetNextProcessID());
                    return;
                }

                Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {type}.");
                return;
            }

            string processID = ProcessManager.GetNextProcessID();

            Engine engine = new(this, $"App__{processID}");

            engine.NetworkModule.processID = processID;
            engine.AppModule.processID = processID;

            var code = await ProcessManager.CreateJavaScriptBackend(type, processID, cmdLineArgs, data, engine).ConfigureAwait(true);

            OpenApp(control, type, processID, engine);

            await engine.Execute(code).ConfigureAwait(true);
        }
        public void OpenApp(UserControl control, string pClass, string processID, Engine? engine = null)
        {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(pClass);
            ArgumentNullException.ThrowIfNull(processID);

            // refresh config.
            LoadConfig();

            // open up a window.
            UserWindow userWindow = Window.CreateWindow(processID, pClass, out var resizable_window);

            // run late init on valid extern apps.
            var allMembers = control.GetType().GetMembers().Where(i => i is MethodInfo);
            if (IsValidExternAppType(allMembers))
                TryRunLateInit(control, resizable_window);

            userWindow.InitializeContent(resizable_window, control, engine);

            // todo: add all the generated os controls to array(s) for easier disposal,
            // such as the taskbar button, the desktop icon, etc.
            // also, having this more organized will make it much easier to safely expose it to JavaScript
            // so we can allow the user to add to their window's toolbar, title, close the app programmatically, etc.

            var process = new Process(this, userWindow, processID, pClass);
            ProcessManager.RegisterNewProcess(process, out var procList);

            void OnWindowClosed()
            {

                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                    App.Current.Dispatcher.Invoke(() => closeMethod());
                else
                    closeMethod();

                void closeMethod()
                {
                    userWindow.OnApplicationClose -= OnWindowClosed;
                    Window.Desktop.Children.Remove(resizable_window);
                    Window.RemoveTaskbarButton(pClass);
                }
            }


            // todo: remove a lot of the hard to reach behavior from the userWindow class and move it here.
            // this will allow us to much easier control when & how things get disposed of, and more as described above.
            userWindow.OnApplicationClose += OnWindowClosed;

            // todo: make a unified interface for windowing, we have a window manager and window classes but
            // the behavior feels scattered and disorganized. fetching weird references for controls should not be a thing : 
            // we should have a query system or just methods exposing behavior directly on easy to get to objects.
            resizable_window.BringIntoViewAndToTop();


            // todo : change this, works for now but it's annoying.
            // we could have a much smarter windowing system that opens apps to the emptiest space or something.
            resizable_window.Width = 900;
            resizable_window.Height = 700;
            Canvas.SetTop(resizable_window, 200);
            Canvas.SetLeft(resizable_window, 200);
        }

        public static BitmapImage LoadImage(string path)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            return bitmapImage;
        }
        public static BitmapImage? GetExternIcon(Type type)
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

        public static void StyleDesktopIcon(string name, Button btn, object? image)
        {
            btn.Background = Brushes.Transparent;
            var grid = new Grid() { Height = btn.Height, Width = btn.Width };

            grid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock
            {
                Text = name,
                TextAlignment = TextAlignment.Left,
                FontSize = 12,
                FontFamily = new FontFamily("MS Gothic"),
                Foreground = Brushes.Cyan,
                Background = new SolidColorBrush(Color.FromArgb(155, 0, 0, 0)),
            };

            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            FrameworkElement element;
            if (image is BitmapImage img)
            {
                element = new Image
                {
                    Opacity = 0.95,
                    Source = img,
                    Stretch = Stretch.Fill,
                    Margin = new(5)
                };
            }
            else
                element = new Rectangle() { Fill = Brushes.Black, Opacity = 0.15, Margin = new(5) };

            grid.Children.Add(element);

            Grid.SetRow(element, 1);
            grid.ToolTip = name;

            btn.Content = grid;
            btn.ToolTip = name;
        }
        public void InstallIcon(AppType type, string appName, Type? runtime_type = null)
        {


            Window.Dispatcher?.Invoke(() =>
            {
                var btn = Window.MakeDesktopButton(appName);

                switch (type)
                {
                    case AppType.Native:
                        InstallNative(btn, appName);
                        break;
                    case AppType.Extern:
                        if (runtime_type != null)
                            InstallExtern(btn, appName, runtime_type);
                        break;
                }

                MenuItem uninstall = new()
                {
                    Header = "uninstall app"
                };

                MenuItem delete = new()
                {
                    Header = "delete app (no undo)"
                };

                uninstall.Click += (sender, @event) =>
                {
                    var answer = MessageBox.Show($"are you sure you want to uninstall {appName}?", "uninstall?", MessageBoxButton.YesNo);

                    if (answer == MessageBoxResult.Yes)
                        Computer.Current.Uninstall(appName + ".app");
                };

                delete.Click += (sender, @event) =>
                {
                    var answer = System.Windows.MessageBox.Show($"are you sure you want to delete {appName}?", "Delete PERMANENTLY??", MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

                    if (answer == MessageBoxResult.Yes)
                        FileSystem.Delete(appName);
                };

                btn.ContextMenu ??= new();
                btn.ContextMenu.Items.Add(uninstall);
                btn.ContextMenu.Items.Add(delete);

                Window.DesktopIconPanel.UpdateLayout();
                Window.DesktopIconPanel.Children.Add(btn);
            });

            void InstallNative(Button btn, string type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;

                var contextMenu = Window.GetNativeContextMenu(appName);

                btn.ContextMenu = contextMenu;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    OpenCustom(type);
                }

                StyleDesktopIcon(type, btn, Runtime.GetAppIcon(appName));
            }
            void InstallExtern(Button btn, string name, Type type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;
                StyleDesktopIcon(appName, btn, GetExternIcon(type));

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    if (Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                    {
                        Computer.Current.OpenApp(userControl, name, ProcessManager.GetNextProcessID());
                    }
                    else
                    {
                        Notifications.Now("Failed to create instance of native application. the app is likely misconfigured");
                    }
                }
            }
        }
        public void InstallFromType(string name, Type type)
        {
            name = name.Replace(".app", string.Empty);
            InstallIcon(AppType.Extern, name, type);
        }

        internal static bool IsValidExternAppType(IEnumerable<MemberInfo> members)
        {
            return members.Any(member => member.Name == "LateInit");
        }
        internal static void TryRunLateInit(object instance, ResizableWindow? resizableWindow = null)
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


        public void InstallCSharpApp(string exePath, Type type)
        {
            if (csApps.TryGetValue(exePath, out _))
            {
                Notifications.Now("Tried to install an app that already exists on the computer, try renaming it if this was intended");
                return;
            }

            csApps[exePath] = type;

            Notifications.Now($"{exePath} installed!");

            InstallFromType(exePath, type);
        }
        public void InstallNative(string type)
        {
            if (disposing)
                return;

            type = type.Replace(".app", "");
            jsApps.Add(type);

            InstallIcon(AppType.Native, type);
        }

        public void Uninstall(string name)
        {
            jsApps.Remove(name);
            Window.Dispatcher.Invoke(() =>
            {
                Window.RemoveDesktopIcon(name);
            });
        }
        internal void LoadBackground()
        {
            string backgroundPath = Config?.Value<string>("BACKGROUND") ?? "background.png";
            var fullPath = FileSystem.GetResourcePath(backgroundPath);

            if (fullPath.Length == 0)
            {
                Notifications.Now($"Failed to find file for desktop background at path : {backgroundPath}");
                return;
            }

            Window.desktopBackground.Source = LoadImage(fullPath);
        }
        
    

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposing)
            {
                if (disposing)
                {
                    JavaScript?.Dispose();
                    Window?.Dispose();
                    Network?.StopHosting();
                    Network?.StopClient();
                    CLI?.Dispose();

                    List<Process> procs = [];
                    foreach (var item in ProcessManager.ProcessClassTable.Values.SelectMany(i => i))
                        procs.Add(item);

                    foreach (var item in procs)
                        item.Terminate();
                }


                JavaScript = null!;
                Window = null!;
                Network = null!;
                FileSystem = null!;
                CLI = null!;

                this.disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
