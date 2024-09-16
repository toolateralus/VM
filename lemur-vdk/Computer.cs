using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.OS.Language;
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
using System.Xml;
using System.Xml.Linq;

namespace Lemur {
    public class Computer : IDisposable {
        internal uint ID { get; private set; }
        // the 'computer id'. This is useless basically,
        // why would you ever need to run multiple desktops under the same C# process.
        internal static uint __procId; 
        internal NetworkConfiguration Network { get; set; }
        internal DesktopWindow Window { get; set; }
        internal FileSystem FileSystem { get; set; }
        internal Engine JavaScript { get; set; }
        internal CommandLine CLI { get; set; }
        internal JObject Config { get; set; }

        private static Computer? current;
        public static Computer Current => current;

        public required ProcessManager ProcessManager { get; init; }

        // we don't use bootstrappers for these because creating them requires litle procedure.
        internal readonly Dictionary<string, Type> externApps = [];
        
        internal readonly Dictionary<string, Bootstrapper> bootstrappers = [];

        internal bool disposing;
        public Computer(FileSystem fs) {
            current = this;

            Notifications.Current = this;

            FileSystem = fs;

            CLI = new(this);

            Network = new();

            if (LoadConfig() is not JObject config) {
                MessageBox.Show("Failed to load config.json! create one or fix your corrupted environment. You will experience problems as a result of not having one.");
                return;
            }

            Config = config;

            JavaScript = new(this, "Computer");

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
                JavaScript.ExecuteScript(AbsPath);

        }
        internal void Exit(int exitCode) {
            if (exitCode != 0) {
                Notifications.Now($"Computer {ID} has exited, most likely due to an error. code:{exitCode}");
            }
            Dispose();
        }
        internal static JObject? LoadConfig() {
            if (FileSystem.GetResourcePath("config.json") is string AbsPath) {
                if (File.Exists(AbsPath)) {
                    string json = File.ReadAllText(AbsPath);

                    try {
                        return JObject.Parse(json);
                    }
                    catch (JsonException ex) {
                        Notifications.Now($"Error loading JSON: {ex.Message}");
                    }
                }
                else {
                    Notifications.Now("Could not locate a valid 'config.json' File.");
                }
            }

            return null;
        }
        public static void SaveConfig(string config) {
            string configFilePath = FileSystem.GetResourcePath("config.json");

            if (!string.IsNullOrEmpty(configFilePath)) {
                try {
                    File.WriteAllText(configFilePath, config);
                }
                catch (Exception ex) {
                    Notifications.Now($"Error saving JSON config: {ex.Message}");
                }
            }
        }
      
        private bool TryOpenExtern(string type, object[] cmdLineArgs) {
            if (externApps.TryGetValue(type, out var csType)) {
                var pid = ProcessManager.GetNextProcessID();
                var app = (UserControl)Activator.CreateInstance(csType, cmdLineArgs)!;
                PresentGUI(app, type, pid);
                return true;
            }
            return false;
        }
        public void PresentGUI(UserControl control, string pClass, string processID, Engine? engine = null) {
            ArgumentNullException.ThrowIfNull(control);
            ArgumentNullException.ThrowIfNull(pClass);
            ArgumentNullException.ThrowIfNull(processID);

            // refresh config.
            LoadConfig();

            // open up a window.
            UserWindow userWindow = Window.CreateWindow(processID, pClass, out var resizable_window);

            // register process now, because it depends on the other pieces. this should not be in this function.
            var process = new Process(this, userWindow, processID, pClass);
            ProcessManager.RegisterNewProcess(process, out var procList);

            userWindow.InitializeContent(resizable_window, control, engine);

            // todo: add all the generated os controls to array(s) for easier disposal,
            // such as the taskbar button, the desktop icon, etc.
            // also, having this more organized will make it much easier to safely expose it to JavaScript
            // so we can allow the user to add to their window's toolbar, title, close the app programmatically, etc.

            void OnWindowClosed() {

                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                    App.Current.Dispatcher.Invoke(() => closeMethod());
                else
                    closeMethod();

                void closeMethod() {
                    userWindow.OnApplicationClose -= OnWindowClosed;
                    Window.Desktop.Children.Remove(resizable_window);
                    Window.RemoveTaskbarButton(pClass);
                }
            }

            userWindow.OnApplicationClose += OnWindowClosed;

            resizable_window.BringIntoViewAndToTop();

            // run late init on valid extern apps.
            var allMembers = control.GetType().GetMembers().Where(i => i is MethodInfo);
            if (IsValidExternAppType(allMembers))
                TryRunLateInit(control, resizable_window);

            // todo : change this, works for now but it's annoying.
            // we could have a much smarter windowing system that opens apps to the emptiest space or something.
            resizable_window.Width = 900;
            resizable_window.Height = 700;
            Canvas.SetTop(resizable_window, 200);
            Canvas.SetLeft(resizable_window, 200);
        }
       
        public static BitmapImage LoadImage(string path) {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }
        public static BitmapImage? GetExternIcon(Type type) {
            var properties = type.GetProperties();

            foreach (var property in properties) {
                if (property.Name.Contains("DesktopIcon") &&
                    property.PropertyType == typeof(string) &&
                    property.GetValue(null) is string path &&
                    !string.IsNullOrEmpty(path)) {
                    return LoadImage(path);
                }
            }

            return null;
        }
        public static void StyleDesktopIcon(string name, Button btn, object? image) {
            btn.Background = Brushes.Transparent;
            var grid = new Grid() { Height = btn.Height, Width = btn.Width };

            grid.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });

            var textBlock = new TextBlock {
                Text = name,
                TextAlignment = TextAlignment.Left,
                FontSize = 12,
                //FontFamily = new FontFamily("MS Gothic"),
                Foreground = Brushes.Cyan,
                Background = new SolidColorBrush(Color.FromArgb(155, 0, 0, 0)),
            };

            Grid.SetRow(textBlock, 0);
            grid.Children.Add(textBlock);

            FrameworkElement element;
            if (image is BitmapImage img) {
                element = new Image {
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
        internal static bool IsValidExternAppType(IEnumerable<MemberInfo> members) {
            return members.Any(member => member.Name == "LateInit");
        }
        internal static void TryRunLateInit(object instance, ResizableWindow? resizableWindow = null) {
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
        
        public void InstallExtern(string name, Type type) {
            Notifications.Now($"{name} installed!");
            CreateBootstrapper(AppType.Extern, name, type);
        }
        
        public void InstallNative(string type) {
            if (disposing)
                return;

            type = type.Replace(".app", "", StringComparison.CurrentCulture);

            var conf = type + ".appconfig";

            var path = FileSystem.GetResourcePath(conf);

            AppConfig? config = null;

            if (path.Length != 0) {
                try {
                    var data = File.ReadAllText(path);
                    config = JsonConvert.DeserializeObject<AppConfig>(data);
                }
                catch (Exception e) {
                    Notifications.Exception(e);
                }
            }

            if (config is null) {
                CreateBootstrapper(AppType.Native, type);
            }
            else {
                CreateBootstrapper(AppType.Native, type, runtime_type: null, config);
            }

        }
        
        public void Uninstall(string name) {
            Window.Dispatcher.Invoke(() => {
                Window.RemoveDesktopIcon(name);
            });
        }
        
        internal void LoadBackground() {
            string backgroundPath = Config?.Value<string>("BACKGROUND") ?? "background.png";
            var fullPath = FileSystem.GetResourcePath(backgroundPath);

            if (fullPath.Length == 0) {
                Notifications.Now($"Failed to find file for desktop background at path : {backgroundPath}");
                return;
            }

            Window.desktopBackground.Source = LoadImage(fullPath);
        }
        
        public void CreateBootstrapper(AppType type, string appName, Type? runtime_type = null, AppConfig? config = null) {
               
            Window.Dispatcher?.Invoke(() => {
                var btn = Window.MakeDesktopButton(appName);

                switch (type) {
                    case AppType.Native:
                        InstallNative(btn, appName);
                        break;
                    case AppType.Extern:
                    if (runtime_type != null)
                        InstallExtern(btn, appName, runtime_type);
                    break;
                }

                bootstrappers.Add(appName, new(appName));

                // if we don't stop this, it deletes the whole computer xD .. (later) .. idk what this comment meant. uh oh!
                if (runtime_type == null) {
                    MenuItem delete = new() {
                        Header = "delete app (no undo)"
                    };
                    delete.Click += (sender, @event) => {
                        var answer = MessageBox.Show($"are you sure you want to delete {appName}?", "Delete PERMANENTLY??", MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

                        if (answer == MessageBoxResult.Yes) {
                            Computer.Current.Uninstall(appName + ".app");
                            var path = FileSystem.GetResourcePath(appName + ".app");
                            if (!string.IsNullOrEmpty(path))
                                FileSystem.Delete(path);
                        }
                    };
                    btn.ContextMenu.Items.Add(delete);
                }

                Window.DesktopIconPanel.UpdateLayout();
                Window.DesktopIconPanel.Children.Add(btn);

                MenuItem uninstall = new() {
                    Header = "uninstall app"
                };

                uninstall.Click += (sender, @event) => {
                    var answer = MessageBox.Show($"are you sure you want to uninstall {appName}?", "uninstall?", MessageBoxButton.YesNo);

                    if (answer == MessageBoxResult.Yes)
                        Current.Uninstall(appName + ".app");
                };
                
                btn.ContextMenu ??= new();
                btn.ContextMenu.Items.Add(uninstall);
            });

            void InstallNative(Button btn, string type) {
                btn.MouseDoubleClick += OnDesktopIconPressed;

                var contextMenu = Window.GetNativeContextMenu(appName, config);

                btn.ContextMenu = contextMenu;

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e) {
                    bootstrappers[type].Open([]);
                }

                StyleDesktopIcon(type, btn, Runtime.GetAppIcon(appName));
            }
            void InstallExtern(Button btn, string name, Type type) {
                btn.MouseDoubleClick += OnDesktopIconPressed;
                StyleDesktopIcon(appName, btn, GetExternIcon(type));

                void OnDesktopIconPressed(object? sender, RoutedEventArgs e) {
                    if (Activator.CreateInstance(type) is UserControl userControl) {
                        Computer.Current.PresentGUI(userControl, name, ProcessManager.GetNextProcessID());
                    }
                    else {
                        Notifications.Now("Failed to create instance of native application. the app is likely misconfigured");
                    }
                }
            }
        }
        
        protected virtual void Dispose(bool disposing) {
            if (!this.disposing) {
                if (disposing) {
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
        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class Bootstrapper(string appName) {
        private readonly string appName = appName;
        private static void CreateJavaScriptRuntime(out string processID, out Engine engine) {
            processID = Computer.Current.ProcessManager.GetNextProcessID();
            engine = new(Computer.Current, $"App__{processID}");
            engine.NetworkModule.processID = processID;
            engine.AppModule.processID = processID;
        }
        public async void Open(params object[] args) {
            // we don't want to dispose the idisposable here. just creating it.
            CreateJavaScriptRuntime(out var processID, out var engine);

            string name = appName.Replace(".app", "");
            var absPath = FileSystem.GetResourcePath(name + ".app");

            if (!Directory.Exists(absPath)) {
                Notifications.Now($"directory for app {appName} not found");
                return;
            }
            if (string.IsNullOrEmpty(name)) {
                Notifications.Now($"invalid name for app {appName}");
                return;
            }

            AppConfig? appConfig = new() {
                requires = [],
                @class = name,
                isWpf = true,
                terminal = false,
                title = name,
            };

            // we support not having a .appconfig to prevent silly issues with accidental deletions 
            // and older versions that didn't use appconfig
            string conf = FileSystem.GetResourcePath(name + ".appconfig");
            var exists = FileSystem.FileExists(conf);
            if (exists) {
                try {
                    var file = File.ReadAllText(conf);
                    var config = JsonConvert.DeserializeObject<AppConfig>(file);
                    appConfig = config ?? appConfig;
                }
                catch (Exception e) {
                    Notifications.Exception(e);
                    return;
                }

            }
            else {
                Notifications.Now($"Warning : No '.appconfig' file found for app {name}. using a default.");
            }

           await ExecuteRequires(engine, appConfig).ConfigureAwait(true);

            // locate the entry point of the application, the main file.
            string jsFile = System.IO.Path.Combine(absPath, appConfig.entryPoint ?? (name + ".xaml.js"));

            if (!File.Exists(jsFile)) {
                Notifications.Now("Invalid application structure : your xaml & .xaml.js & .app files/folder must be the same name.");
                return;
            }

            var js = await File.ReadAllTextAsync(jsFile).ConfigureAwait(true);

            if (appConfig.isWpf) {
                // setup some names.
                string xamlFile = System.IO.Path.Combine(absPath, appConfig.frontEnd ?? (name + ".xaml"));

                if (!File.Exists(xamlFile)) {
                    Notifications.Now("Invalid application structure : your xaml & .xaml.js & .app files/folder must be the same name.");
                    return;
                }

                var xaml = File.ReadAllText(xamlFile);
                var control = XamlHelper.ParseUserControl(xaml);

                if (control == null) {
                    Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {appName}.");
                    return;
                }

                // TODO: make a way to have a void main() style app.
                Computer.Current.PresentGUI(control, appConfig.title, processID, engine);

                // wait for the class declaration to finalize.
                // Hope that the user didn't put some long running global code in there D:
                _ = await engine.Execute(js).ConfigureAwait(true);

                string instantiation_code = $"const {processID} = new {name}('{processID}')";

                _ = engine.Execute(instantiation_code);
            }
            else if (appConfig.terminal) {
                Terminal term = new() {
                    Engine = engine
                };

                // if we're using both wpf and a terminal we need to spawn a nw process for this.
                // todo: verify that we don't need an entire new process object, or that this creates one.
                // wtf is this doing
                var pid = appConfig.isWpf ? Computer.Current.ProcessManager.GetNextProcessID() : processID;

                Computer.Current.PresentGUI(term, appConfig.title, pid, engine);

                _ = engine.Execute($"""const pid = {pid}'""").ConfigureAwait(false);
                if (appConfig.isWpf == false)
                    _ = engine.Execute(js).ConfigureAwait(false);
            }

        }
        
        private static async Task ExecuteRequires(Engine engine, AppConfig? appConfig) {
            string joinedRequires = "";

            if (appConfig is not null)
                foreach (var item in appConfig.requires)
                    joinedRequires += $"const {{{string.Join(", ", item.Value)}}} = require('{item.Key}')\n";

            if (joinedRequires.Length > 0)
                await engine.Execute(joinedRequires).ConfigureAwait(true);
        }
    }

}


