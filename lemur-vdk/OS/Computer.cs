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
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;
using Microsoft.ClearScript.JavaScript;
using Lemur.OS;
using Lemur.Graphics;
using Newtonsoft.Json;

namespace Lemur
{
    public class Computer : IDisposable
    {
        internal string WorkingDir { get; private set; }
        internal uint ID { get; private set; }
        private static uint processCount;

        [Obsolete("This is a (probably broken) tcp network implementation. It is not especially secure. Use at your own risk, but probably don't use this. it is unused by default")]
        internal NetworkConfiguration Network { get; set; }
        internal ComputerWindow Window { get; set; }
        internal FileSystem FileSystem { get; set; }
        internal Engine JavaScript { get; set; }
        internal CommandLine CmdLine { get; set; }
        internal JObject Config { get; set; }

        /// <summary>
        /// app name keys, list of process id's values
        /// </summary>
        internal static Dictionary<string, List<string>> ProcessLookupTable = new();
        internal readonly Dictionary<string, UserWindow> UserWindows = new();
        internal readonly Dictionary<string, Type> csApps = new();

        internal readonly List<string> jsApps = new();

        internal bool disposing;
        public void InitializeEngine(Computer computer)
        {
            JavaScript = new(computer);

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
                JavaScript.ExecuteScript(AbsPath);
        }
       
        public Computer(FileSystem fs)
        {
            this.FileSystem = fs;

            CmdLine = new();
             
            Network = new(this);

            Config = LoadConfig();

            InitializeEngine(this);

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
        public void OpenApp(UserControl control, string title = "window", Brush? background = null, Brush? foreground = null, Engine engine = null)
        {
            // the resizable is the container that hosts the user app.
            // this is made seperate to eliminate annoying and complex boiler plate.
            UserWindow window = Window.OpenAppUI(title, out var resizable_window);
            UserWindows[title] = window;

         
            window.InitializeUserContent(resizable_window, control, engine);

            resizable_window.BringToTopOfDesktop();

            // todo : change this
            resizable_window.Width = 650;
            resizable_window.Height = 650;
            Canvas.SetTop(resizable_window, 200);
            Canvas.SetLeft(resizable_window, 200);
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

            InstallCSWPF(exePath, type);
        }
        public void InstallJSWPF(string type)
        {
            if (disposing)
                return;
            
            jsApps.Add(type);

            Window.InstallIcon(AppType.JAVASCRIPT_XAML_WPF, type);
        }
        public void InstallJSHTML(string type)
        {
            if (disposing)
                return;
            jsApps.Add(type);
            Window.InstallIcon(AppType.JS_HTML_WEB_APPLET, type);
        }
        public void InstallCSWPF(string exePath, Type type)
        {
            var name = exePath.Split('.')[0];
            Window.InstallIcon(AppType.CSHARP_XAML_WPF_NATIVE, exePath, type);
        }
       
        public void Uninstall(string name)
        {
            jsApps.Remove(name);
            Window.Dispatcher.Invoke(() => { Window.RemoveDesktopIcon(name); });
        }

        private static void LoadBackground(Computer pc, ComputerWindow wnd)
        {
            string backgroundPath = pc?.Config?.Value<string>("BACKGROUND") ?? "background.png";
            wnd.desktopBackground.Source = ComputerWindow.LoadImage(FileSystem.GetResourcePath(backgroundPath) ?? "background.png");
        }
        

        public async Task OpenCustom(string type)
        {
            var data = Runtime.GetAppDefinition(type);
            
            var control = XamlHelper.ParseUserControl(data.XAML);

            if (control == null)
            {
                Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {type}.");
                return;
            }

            Engine engine = new(this);
            
            var (id, code) = await InstantiateWindowClass(type, data, engine);

            OpenApp(control, title: id, engine: engine);

            if (!ProcessLookupTable.TryGetValue(type, out var array))
            {
                array = new();
                ProcessLookupTable[type] = array;
            }

            ProcessLookupTable[type].Add(id);

            UserWindows[id].OnClosed += delegate 
            {
                if (!ProcessLookupTable.ContainsKey(type))
                    throw new Exception("The application became detached from the operating system, or is unknown.");

                ProcessLookupTable[type].Remove(id);

                if (ProcessLookupTable[type].Count == 0)
                    ProcessLookupTable.Remove(type);
            };

            await engine.Execute(code);
        }
        public static string GetProcessClass(string identifier)
        {
            var processClass = "Unknown process";

            foreach (var proc in Computer.ProcessLookupTable)
                foreach (var pid in proc.Value)
                    if (pid == identifier)
                        processClass = proc.Key;
            return processClass;
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
            var workingDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + $"\\Lemur\\computer{cpu_id}";
            var FileSystem = new FileSystem(workingDir);

            Computer pc = new(FileSystem);
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

            pc.InstallCSharpApp("CommandPrompt.app", typeof(CommandPrompt));
            pc.InstallCSharpApp("FileExplorer.app", typeof(FileExplorer));
            pc.InstallCSharpApp("TextEditor.app", typeof(TextEditor));
#if DEBUG   
            pc.InstallCSharpApp("GL_TEST.app", typeof(OpenGL2Window));
#endif

            Runtime.LoadCustomSyntaxHighlighting();
        }

        public static IReadOnlyCollection<T> TryGetAllProcessesOfType<T>() where T : UserControl
        {
            List<T> contents = new();
            foreach (var window in Current.UserWindows)
            {
                window.Value.Dispatcher.Invoke(() => {

                    if (window.Value is not UserWindow userWindow)
                        return;

                    if (userWindow.ContentsFrame is not Frame frame)
                        return;

                    if (frame.Content is not T instance)
                        return;

                    contents.Add(instance);
                });
            }
            return contents;
        }

        public static T TryGetProcessOfType<T>() where T : UserControl
        {
            T content = null;
            foreach (var window in Current.UserWindows)
            {
                window.Value.Dispatcher.Invoke(() => { 
                
                    if (window.Value is not UserWindow userWindow)
                        return;

                    if (userWindow.ContentsFrame is not Frame frame)
                        return;

                    if (frame.Content is not T instance)
                        return;

                    content = instance;
                });
                
                if (content != null)
                {
                    return content;
                }
            }
            return content!;
        }
    
        private static async Task<(string id, string code)> InstantiateWindowClass(string type, (string XAML, string JS) data, Engine engine)
        {
            var name = type.Split('.')[0];

            var JS = new string(data.JS);

            _ = await engine.Execute(JS);

            var instance_name = "p" + (processCount++).ToString();

            engine.AppModule.__SetId(instance_name);

            string instantiation_code = $"const {instance_name} = new {name}('{instance_name}')";

            return (instance_name, instantiation_code);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposing)
            {
                if (disposing)
                {
                    JavaScript?.Dispose();
                    Window?.Dispose();
                    Network?.Dispose();
                    CmdLine?.Dispose();

                    foreach (var item in UserWindows)
                        item.Value.Close();
                }

                JavaScript = null!;
                Window = null!;
                Network = null!;
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
    }
}
