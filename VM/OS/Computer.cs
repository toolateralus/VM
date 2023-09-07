using System;
using System.IO;
using VM.GUI;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using VM.Network;
using VM.FS;
using VM.JS;
using VM.UserInterface;
using Microsoft.VisualBasic.Devices;
using System.Threading;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;
using System.ComponentModel;
using static VM.GUI.ComputerWindow;

namespace VM
{
    public class Computer : IDisposable
    {
        public NetworkConfiguration Network = null!;
        public ComputerWindow Window;
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;
        public CommandLine CommandLine;

        public JObject Config;

        public readonly Dictionary<string, UserWindow> USER_WINDOW_INSTANCES = new();
        public Theme Theme { get; private set; } = new();

        public readonly List<string> InstalledJSApps = new();
        public Dictionary<string, Type> Installed_CSharp_Apps { get; private set; } = new();

        public uint ID { get; private set; }
        public string FS_ROOT { get; private set; }
        public string WORKING_DIR { get; private set; }
        public bool Disposing { get; internal set; }

        public void InstallApplication(string exePath, Type type)
        {
            // do we need this collection? it helps us identify already existing apps but it's almost unneccesary,
            // we may be relying on our UI scripts to do too much behavior.
            if (Installed_CSharp_Apps.TryGetValue(exePath, out _))
            {
                Notifications.Now("Tried to install an app that already exists on the computer, try renaming it if this was intended");
                return;
            }
            Installed_CSharp_Apps[exePath] = type;
            Notifications.Now($"{exePath} installed!");
            ComputerWindow window = Window;
            InstallCSWPF(exePath, type);
        }
        public void InitializeEngine(Computer computer)
        {
            JavaScriptEngine = new(computer);

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
            {
                JavaScriptEngine.ExecuteScript(AbsPath);
            }
        }
        public static string SearchForParentRecursive(string targetDirectory)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string currentDirectory = Path.GetDirectoryName(assemblyLocation);

            while (!Directory.Exists(Path.Combine(currentDirectory, targetDirectory)))
            {
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                if (currentDirectory == null)
                {
                    // Reached the root directory without finding the target
                    return null;
                }
            }

            return Path.Combine(currentDirectory, targetDirectory);
        }
        public Computer(uint id)
        {
            CommandLine = new(this);

            var WORKING_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM";

            this.WORKING_DIR = Path.GetFullPath(WORKING_DIR);
            
            // prepare the root dir for the FileSystem, since we add a dir to contain that itself.
            FS_ROOT = $"{this.WORKING_DIR}\\computer{id}";

            FS = new(FS_ROOT, this);

            Config = LoadConfig();

            Network = new(this);

            InitializeEngine(this);

        }
        internal static JObject LoadConfig()
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
                    catch (Exception ex)
                    {
                        Notifications.Now($"Error loading JSON: {ex.Message}");
                    }
                }
                else
                {
                    Notifications.Now("JSON file not found.");
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
        public void OpenApp(UserControl control, string title = "window", Brush? background = null, Brush? foreground = null, JavaScriptEngine engine = null)
        {
            UserWindow window = Window.OpenAppUI(control, title,ref background, ref foreground, engine);
            USER_WINDOW_INSTANCES[title] = window;
        }

        public void InstallJSWPF(string type)
        {
            if (Disposing)
                return;
            InstalledJSApps.Add(type);
            Window.InstallIcon(AppType.JAVASCRIPT_XAML_WPF, type);
        }
        public void InstallJSHTML(string type)
        {
            if (Disposing)
                return;
            InstalledJSApps.Add(type);
            Window.InstallIcon(AppType.JS_HTML_WEB_APPLET, type);
        }
        public void InstallCSWPF(string exePath, Type type)
        {
            var name = exePath.Split('.')[0];
            Window.InstallIcon(AppType.CSHARP_XAML_WPF_NATIVE, exePath, type); ;
        }
       
        public void Uninstall(string name)
        {
            InstalledJSApps.Remove(name);
            Window.Dispatcher.Invoke(() => { Window.RemoveDesktopIcon(name); });
        }
        internal void FinishInit(ComputerWindow wnd)
        {
            LoadBackground(this, wnd);

            wnd.Show();

            wnd.Closed += (o, e) =>
            {
                Computer.Computers.Remove(this);
                Task.Run(() => SaveConfig(Config.ToString()));
                Dispose();
            };

            InstallCoreApps(this);
        }
        private static void InstallCoreApps(Computer pc)
        {
            pc.InstallApplication("CommandPrompt.app", typeof(CommandPrompt));
            pc.InstallApplication("FileExplorer.app", typeof(FileExplorer));
            pc.InstallApplication("TextEditor.app", typeof(TextEditor));
        }
        private static void LoadBackground(Computer pc, ComputerWindow wnd)
        {
            string backgroundPath = pc?.Config?.Value<string>("BACKGROUND") ?? "background.png";
            wnd.desktopBackground.Source = ComputerWindow.LoadImage(FileSystem.GetResourcePath(backgroundPath) ?? "background.png");
        }
        internal void Print(object? obj)
        {
            JavaScriptEngine?.InteropModule?.print(obj ?? "null");
        }

        public async Task OpenCustom(string type)
        {
            var data = Runtime.GetAppDefinition(type);
            var control = XamlJsInterop.ParseUserControl(data.XAML);
            if (control == null)
            {
                Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {type}.");
                return;
            }
            JavaScriptEngine engine = new(this);
            var jsResult = await InstantiateWindowClass(type, data, engine);
            OpenApp(control, title: jsResult.id, engine: engine);
            await engine.Execute(jsResult.code);
        }

        #region Application
        public static Dictionary<Computer, ComputerWindow> Computers = new();
        public static ComputerWindow? GetWindow(Computer pc)
        {
            Computers.TryGetValue(pc, out var val);
            return val!;
        }
        public static void Restart(uint id)
        {
            var pc = Computers.Where(C => C.Key.ID == id).FirstOrDefault();

            if (pc.Key != null && pc.Value != null)
            {
                var computer = pc.Key;
                Computers.Remove(computer);

                pc.Key.Exit(0);
                pc.Value.Dispose();

                Thread.Sleep(2500);

                Boot(id);
            }
        }
        internal protected static void Boot(uint cpu_id)
        {
            Computer pc = new(cpu_id);
            ComputerWindow wnd = new(pc);
            pc.Window = wnd;
            Computers[pc] = wnd;
            pc.FinishInit(wnd);
        }
        public static T SearchForOpenWindowType<T>(Computer Computer)
        {
            var wnd = GetWindow(Computer);
            T content = default!;

            if (wnd is null)
            {
                Notifications.Exception(new NullReferenceException("Window not found."));
                return default!;
            }

            foreach (var window in wnd.Computer.USER_WINDOW_INSTANCES)
            {
                window.Value.Dispatcher.Invoke(() => { 
                
                    if (window.Value is UserWindow userWindow && userWindow.Content is Grid g)
                    {
                        foreach (var item in g.Children)
                        {
                            if (item is Frame frame)
                            {
                                if (frame.Content is T ActualApplication)
                                {
                                    content =  ActualApplication;
                                    break;
                                }
                            }
                        }

                    }
                });
            }
            return content!;

        }
        private static async Task<(string id, string code)> InstantiateWindowClass(string type, (string XAML, string JS) data, JavaScriptEngine engine)
        {
            var name = type.Split('.')[0];

            var JS = new string(data.JS);

            _ = await engine.Execute(JS);

            var instance_name = "uid" + Guid.NewGuid().ToString().Split('-')[0];

            string instantiation_code = $"let {instance_name} = new {name}('{instance_name}')";

            return (instance_name, instantiation_code);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    JavaScriptEngine?.Dispose();
                    Window?.Dispose();
                    Network?.Dispose();
                    FS?.Dispose();
                    CommandLine?.Dispose();

                    foreach (var item in USER_WINDOW_INSTANCES)
                        item.Value.Close();
                }
                
                JavaScriptEngine = null!;
                Window = null!;
                Network = null!;
                FS = null!;
                CommandLine = null!;

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
