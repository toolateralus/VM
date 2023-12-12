using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.OS;
using Lemur.Types;
using Lemur.Windowing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lemur
{
    public record Process(UserWindow UI, string ID, string Type)
    {
        public Action? OnProcessTermination { get; internal set; }
    }
    public class Computer : IDisposable
    {
        internal string WorkingDir { get; private set; }
        internal uint ID { get; private set; }
        internal static uint __procId;
        internal NetworkConfiguration NetworkConfiguration { get; set; }
        internal ComputerWindow Window { get; set; }
        internal FileSystem FileSystem { get; set; }
        internal Engine JavaScript { get; set; }
        internal CommandLine CmdLine { get; set; }
        internal JObject Config { get; set; }

        private static Computer? current;
        private int startupTimeoutMs = 20_000;
        public static Computer Current => current;


        public static IEnumerable<Process> AllProcesses()
        {
            return ProcessClassTable.Values.SelectMany(i => i);
        }

        // type : process(es)
        internal static Dictionary<string, List<Process>> ProcessClassTable = [];
        internal readonly Dictionary<string, Type> csApps = [];

        internal readonly List<string> jsApps = new();

        internal bool disposing;
  
        public Computer(FileSystem fs)
        {
            current = this;

            FileSystem = fs;

            CmdLine = new();

            NetworkConfiguration = new();

            Config = LoadConfig();

            JavaScript = new();

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
                JavaScript.ExecuteScript(AbsPath);

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
                    Notifications.Now("Could not locate a valid 'config.json' file.");
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
        internal void Exit(int exitCode)
        {
            if (exitCode != 0)
            {
                Notifications.Now($"Computer {ID} has exited, most likely due to an error. code:{exitCode}");
            }
            Dispose();

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
        internal static bool IsValidExternAppType(IEnumerable<MemberInfo> members)
        {
            return members.Any(member => member.Name == "LateInit");
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

            var process = new Process(userWindow, processID, pClass);

            RegisterNewProcess(process, out var procList);

            void OnWindowClosed()
            {

                // for pesky calls that arent from the UI thread, from javascript etc.
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                    App.Current.Dispatcher.Invoke(() => closeMethod());
                else
                    closeMethod();

                void closeMethod()
                {
                    userWindow.OnAppClosed -= OnWindowClosed;

                    if (!ProcessClassTable.ContainsKey(pClass))
                    {
                        Notifications.Now($"Tried to close non existent app : {pClass}");
                        return;
                    }

                    List<Process> array = ProcessClassTable[pClass];

                    process.OnProcessTermination?.Invoke();

                    Window.Desktop.Children.Remove(resizable_window);

                    resizable_window.Content = null;

                    Window.RemoveTaskbarButton(pClass);

                    array.Remove(process);

                    if (array.Count == 0)
                        ProcessClassTable.Remove(pClass);
                    else
                        ProcessClassTable[pClass] = array;

                }
            }


            // todo: remove a lot of the hard to reach behavior from the userWindow class and move it here.
            // this will allow us to much easier control when & how things get disposed of, and more as described above.
            userWindow.OnAppClosed += OnWindowClosed;

            // todo: make a unified interface for windowing, we have a window manager and window classes but
            // the behavior feels scattered and disorganized. fetching weird references for controls should not be a thing : 
            // we should have a query system or just methods exposing behavior directly on easy to get to objects.
            resizable_window.BringToTopOfDesktop();


            // todo : change this, works for now but it's annoying.
            // we could have a much smarter windowing system that opens apps to the emptiest space or something.
            resizable_window.Width = 900;
            resizable_window.Height = 700;
            Canvas.SetTop(resizable_window, 200);
            Canvas.SetLeft(resizable_window, 200);
        }
        private static void RegisterNewProcess(Process process, out List<Process> procList)
        {
            GetProcessesOfType(process.Type, out procList);

            procList.Add(process);

            ProcessClassTable[process.Type] = procList;

            process.OnProcessTermination += () =>
            {
                var procList = ProcessClassTable[process.Type];

                procList.Remove(process);

                ProcessClassTable[process.Type] = procList;
                
                if (procList.Count == 0)
                    ProcessClassTable.Remove(process.Type);

            };

        }
        private static void GetProcessesOfType(string name, out List<Process> processes)
        {
            if (!ProcessClassTable.TryGetValue(name, out processes!))
                processes= [];
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
        public static void SetupIcon(string name, Button btn, object? image)
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
                Foreground = Brushes.Cyan,
                Background = new SolidColorBrush(Color.FromArgb(65, 10, 10, 10)),
            };

            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            FrameworkElement element;

            if (image is BitmapImage img)
                element = new Image
                {
                    Source = img,
                    Stretch = Stretch.Fill,
                };
            else
                element = new Rectangle() { Fill = Brushes.Black };

            grid.Children.Add(element);

            Grid.SetRow(element, 1);
            grid.ToolTip = name;

            btn.Content = grid;
            btn.ToolTip = name;
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
        public void InstallIcon(AppType type, string appName, Type? runtime_type = null)
        {
            void InstallNative(Button btn, string type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;

                var contextMenu = Window.GetNativeContextMenu(appName);

                btn.ContextMenu = contextMenu;

                async void OnDesktopIconPressed(object? sender, RoutedEventArgs e) => await OpenCustom(type);

                SetupIcon(type, btn, Runtime.GetAppIcon(appName));
            }
            void InstallExtern(Button btn, string name, Type type)
            {
                btn.MouseDoubleClick += OnDesktopIconPressed;
                SetupIcon(appName, btn, GetExternIcon(type));

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e)
                {
                    if (Activator.CreateInstance(type) is object instance && instance is UserControl userControl)
                    {
                        Computer.Current.OpenApp(userControl, name, Computer.GetNextProcessID());
                    }
                    else
                    {
                        Notifications.Now("Failed to create instance of native application. the app is likely misconfigured");
                    }
                }
            }

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

                uninstall.Click += (sender, @event) =>
                {
                    var answer = MessageBox.Show($"are you sure you want to uninstall {appName}?", "uninstall?", MessageBoxButton.YesNo);

                    if (answer == MessageBoxResult.Yes)
                        Computer.Current.Uninstall(appName);
                };

                btn.ContextMenu ??= new();
                btn.ContextMenu.Items.Add(uninstall);

                Window.DesktopIconPanel.UpdateLayout();
                Window.DesktopIconPanel.Children.Add(btn);
            });

        }
        public void InstallFromType(string name, Type type)
        {
            name = name.Replace(".app", string.Empty);
            InstallIcon(AppType.Extern, name, type);
        }
        public void Uninstall(string name)
        {
            jsApps.Remove(name);
            Window.Dispatcher.Invoke(() =>
            {
                Window.RemoveDesktopIcon(name);
            });
        }
        private static void LoadBackground(Computer pc, ComputerWindow wnd)
        {
            string backgroundPath = pc?.Config?.Value<string>("BACKGROUND") ?? "background.png";
            wnd.desktopBackground.Source = Computer.LoadImage(FileSystem.GetResourcePath(backgroundPath) ?? "background.png");
        }
        public async Task OpenCustom(string type, params object[] cmdLineArgs)
        {
            if (!type.Contains(".app"))
                type += ".app";

            var data = Runtime.GetAppDefinition(type);

            var control = XamlHelper.ParseUserControl(data.XAML);

            if (control == null)
            {
                if (csApps.TryGetValue(type, out var csType))
                {
                    OpenApp((UserControl)Activator.CreateInstance(csType, cmdLineArgs)!, type, GetNextProcessID());
                    return;
                }

                Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {type}.");
                return;
            }

            string processID = GetNextProcessID();

            Engine engine = new();

            engine.NetworkModule.processID = processID;
            engine.AppModule.processID = processID;

            var code = await InstantiateWindowClass(type, processID, cmdLineArgs, data, engine).ConfigureAwait(true);

            OpenApp(control, type, processID, engine);

            await engine.Execute(code).ConfigureAwait(true);
        }
        internal static string GetNextProcessID()
        {
            return $"p{__procId++}";
        }
        public static string GetProcessClass(string identifier)
        {
            var processClass = "Unknown process";

            foreach (var procList in ProcessClassTable)
                foreach (var proc in from proc in procList.Value
                                     where proc.ID == identifier
                                     select proc)
                {
                    processClass = proc.Type;
                }

            return processClass;
        }

        [Command("restart", "restarts the computer")]
        public void Restart(SafeList<object> _)
        {
            Current.Exit(0);
            Current.Dispose();
            Boot(Current.ID);
        }
        internal protected static void Boot(uint cpu_id)
        {
            var workingDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\Lemur\\computer{cpu_id}";
            var FileSystem = new FileSystem(workingDir);

            Computer pc = new Computer(FileSystem);

            ComputerWindow wnd = new();

            Current.Window = wnd;

            LoadBackground(pc, wnd);

            wnd.Show();

            wnd.Closed += (o, e) =>
            {
                Task.Run(() => SaveConfig(pc.Config?.ToString() ?? ""));
                pc.Dispose();
            };

            pc.InstallCSharpApp("terminal.app", typeof(Terminal));
            pc.InstallCSharpApp("FileExplorer.app", typeof(FileExplorer));
            pc.InstallCSharpApp("texed.app", typeof(Texed));

            Runtime.LoadCustomSyntaxHighlighting();
        }
        public static IReadOnlyCollection<T> TryGetAllProcessesOfType<T>() where T : UserControl
        {
            List<T> contents = [];
            foreach (var process in ProcessClassTable.Values.SelectMany(i => i.Select(i => i))) // flatten array
            {
                process.UI.Dispatcher.Invoke(() =>
                {
                    if (process.UI.ContentsFrame is not Frame frame)
                        return;

                    if (frame.Content is not T instance)
                        return;

                    contents.Add(instance);
                });
            }
            return contents;
        }
        public static T? TryGetProcessOfTypeUnsafe<T>() where T : UserControl
        {
            T? matchingWindow = default(T);

            foreach (var pclass in ProcessClassTable)
                foreach (var proc in pclass.Value)
                    if (proc.UI.ContentsFrame is Frame frame && frame.Content is T instance)
                        matchingWindow = instance;

            return matchingWindow;
        }
        public static T? TryGetProcessOfType<T>() where T : UserControl
        {
            T? content = default(T);

            Current.Window.Dispatcher.Invoke(() =>
            {
                content = TryGetProcessOfTypeUnsafe<T>();
            });

            return content;
        }
        private static async Task<string> InstantiateWindowClass(string type, string processID, object[] cmdLineArgs, (string XAML, string JS) data, Engine engine)
        {
            var name = type.Split('.')[0];

            var JS = new string(data.JS);

            _ = await engine.Execute(JS);

            string instantiation_code;


            if (cmdLineArgs.Length != 0)
            {
                var args = string.Join(", ", cmdLineArgs);
                instantiation_code = $"const {processID} = new {name}('{processID}, {args}')";
            }
            else
                instantiation_code = $"const {processID} = new {name}('{processID}')";



            return instantiation_code;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposing)
            {
                if (disposing)
                {
                    JavaScript?.Dispose();
                    Window?.Dispose();
                    NetworkConfiguration?.StopHosting();
                    NetworkConfiguration?.StopClient();
                    CmdLine?.Dispose();

                    List<Process> procs = [];
                    foreach (var item in ProcessClassTable.Values.SelectMany(i => i))
                        procs.Add(item);

                    foreach (var item in procs)
                        item.UI.Close();
                }


                JavaScript = null!;
                Window = null!;
                NetworkConfiguration = null!;
                FileSystem = null!;
                CmdLine = null!;

                this.disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        internal void CloseApp(string pID)
        {
            if (GetProcess(pID) is Process p)
                p.UI.Close();
            else Notifications.Now($"Could not find process {pID}");
        }
        internal static Process? GetProcess(string pid)
        {
            foreach (var pclass in ProcessClassTable)
                if (pclass.Value.FirstOrDefault(p => p.ID == pid) is Process proc)
                    return proc;
            return null;
        }
    }
}
