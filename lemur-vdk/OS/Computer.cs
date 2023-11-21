using System;
using System.IO;
using Lemur.GUI;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using Lemur.Network;
using Lemur.FS;
using Lemur.JS;
using Lemur.UserInterface;
using Microsoft.VisualBasic.Devices;
using System.Threading;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;
using System.ComponentModel;
using static Lemur.GUI.ComputerWindow;
using Microsoft.ClearScript.JavaScript;

namespace Lemur
{
    public class Computer : IDisposable
    {
        public NetworkConfiguration Network = null!;
        public ComputerWindow Window;
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;
        public CommandLine CommandLine;

        public JObject Config;

        public readonly Dictionary<string, UserWindow> Windows = new();
        public Theme Theme { get; private set; } = new Theme();

        public readonly List<string> InstalledJSApps = new();
        public Dictionary<string, Type> Installed_CSharp_Apps { get; private set; } = new();

        public uint ID { get; private set; }
        public string FS_ROOT { get; private set; }
        public string WORKING_DIR { get; private set; }
        public bool Disposing { get; internal set; }

        public void InstallApplication(string exePath, Type type)
        {
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
                JavaScriptEngine.ExecuteScript(AbsPath);
        }
        public static string? SearchForParentRecursive(string targetDirectory)
        {
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string currentDirectory = Path.GetDirectoryName(assemblyLocation);

            while (!Directory.Exists(Path.Combine(currentDirectory, targetDirectory)))
            {
                currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
                if (currentDirectory == null)
                    return null;
            }

            return Path.Combine(currentDirectory, targetDirectory);
        }
        public Computer(uint id)
        {
            CommandLine = new(this);

            var WORKING_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Lemur";

            this.WORKING_DIR = Path.GetFullPath(WORKING_DIR);

            // prepare the root dir for the file system
            FS_ROOT = $"{this.WORKING_DIR}\\computer{id}";

            FS = new(FS_ROOT);

            Network = new(this);

            InitializeEngine(this);

            Config = LoadConfig();
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
                    // TODO: Make the default installer include a config? it should already.
                    // I guess on first install this probably just gets called pre-install. Fix that!
                    //Notifications.Now("JSON file not found.");
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
            // the resizable is the container that hosts the user app.
            // this is made seperate to eliminate annoying and complex boiler plate.
            UserWindow window = Window.OpenAppUI(title, ref background, ref foreground, out var resizable_window);
            Windows[title] = window;

            // this is the process being opened and the UI being established for it.
            // they are heavily woven, unfortunately.
            window.InitializeUserContent(resizable_window, control, engine);
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
            
            var (id, code) = await InstantiateWindowClass(type, data, engine).ConfigureAwait(true);

            OpenApp(control, title: id, engine: engine);

            if (!ProcessLookupTable.TryGetValue(type, out var array))
            {
                array = new();
                ProcessLookupTable[type] = array;
            }

            ProcessLookupTable[type].Add(id);

            Windows[id].OnClosed += delegate 
            {
                if (!ProcessLookupTable.ContainsKey(type))
                    throw new Exception("The application became detached from the operating system, or is unknown.");

                ProcessLookupTable[type].Remove(id);

                if (ProcessLookupTable[type].Count == 0)
                    ProcessLookupTable.Remove(type);
            };

            await engine.Execute(code).ConfigureAwait(false);
        }

        #region Application
        private static Computer? current;
        public static Computer Current => current ?? throw new InvalidOperationException("No computer was active when accessed."); 
        /// <summary>
        /// This just causes crashes.
        /// </summary>
        /// <param name="id"></param>
        public static void Restart(uint id)
        {
            Current.Exit(0);
            Current.Dispose();
            Boot(id);
        }
        internal protected static void Boot(uint cpu_id)
        {
            Computer pc = new(cpu_id);
            ComputerWindow wnd = new(pc);
            pc.Window = wnd;

            if (current != null)
                throw new InvalidOperationException("you can't open several instances of the computer window anymore. just start a few instances of the app, and use TCP IPC. this can be modded in rather easily, if you need a lower latency or better performance solution.");

            current = pc;

            LoadBackground(pc, wnd);

            wnd.Show();

            wnd.Closed += (o, e) =>
            {
                Task.Run(() => SaveConfig(pc.Config?.ToString() ?? ""));
                pc.Dispose();
            };

            pc.InstallApplication("CommandPrompt.app", typeof(CommandPrompt));
            pc.InstallApplication("FileExplorer.app", typeof(FileExplorer));
            pc.InstallApplication("TextEditor.app", typeof(TextEditor));

            Runtime.LoadCustomSyntaxHighlighting();
        }
        public static T TryGetProcess<T>()
        {
            T content = default!;

            foreach (var window in Current.Windows)
            {
                // we should really put a centralized job queue in each window, and have internal multi-threading threr with several js engines.
                // at least optionally, since it's pretty heavy memory overhead, but that could be very powerful.

                window.Value.Dispatcher.Invoke(() => { 
                
                    if (window.Value is not UserWindow userWindow || userWindow.Content is not Grid g)
                        return;

                    var result = g.Children.ToEnumerable().FirstOrDefault(i => i is Frame frame && frame.Content is T actualApp);

                    if (result == default)
                        return;

                    var frame = result as Frame ?? throw new NullReferenceException("failed to cast result of window search to frame, perhaps you're using the wrong XAML elements? probably an engine bug.");
                    var app = (T)frame.Content;
                    content = app;

                });
            }
            return content!;

        }
        public static Dictionary<string, List<string>> ProcessLookupTable = new();
        private static uint processCount;
        private static async Task<(string id, string code)> InstantiateWindowClass(string type, (string XAML, string JS) data, JavaScriptEngine engine)
        {
            var name = type.Split('.')[0];

            var JS = new string(data.JS);

            _ = await engine.Execute(JS);

            var instance_name = "p" + (processCount++).ToString();

            string instantiation_code = $"const {instance_name} = new {name}('{instance_name}')";

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
                    CommandLine?.Dispose();

                    foreach (var item in Windows)
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
