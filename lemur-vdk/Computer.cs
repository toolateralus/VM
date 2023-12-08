using System;
using System.IO;
using Lemur.GUI;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Lemur.FS;
using Lemur.JS;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Media;
using Microsoft.ClearScript.JavaScript;
using Lemur.OS;
using Newtonsoft.Json;
using System.Threading;
using Lemur.Windowing;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;

namespace Lemur
{
    public class Computer : IDisposable
    {
        internal string WorkingDir { get; private set; }
        internal uint ID { get; private set; }
        private static uint processCount;

        [Obsolete("This is a (probably broken) tcp network implementation. It is not especially secure. Use at your own risk, but probably don't use this. it is unused by default")]
        internal NetworkConfiguration NetworkConfiguration { get; set; }
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
            JavaScript = new();

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
                JavaScript.ExecuteScript(AbsPath);
        }
       
        public Computer(FileSystem fs)
        {
            current = this;

            FileSystem = fs;

            CmdLine = new();
             
            NetworkConfiguration = new();

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
        public void OpenApp(UserControl control, string title = "window", Engine engine = null)
        {
            // update config before we start apps. cheap & easy way to make sure the config is up to date, though it could be more frequent.
            Computer.LoadConfig();


            if (UserWindows.ContainsKey(title))
            {
                if (char.IsDigit(title.Last()))
                {
                    var i = int.Parse(title.Last().ToString()) + 1;
                    title.Replace(title.Last(), i.ToString()[0]);
                }
                else
                {
                    title += "1";
                }
            }

            // the resizable is the container that hosts the user app.
            // this is made seperate to eliminate annoying and complex boiler plate.
            UserWindow window = Window.OpenAppUI(title, out var resizable_window);

            UserWindows[title] = window;

            if (ComputerWindow.IsValidType(control.GetType().GetMembers()))
                ComputerWindow.AssignComputer(control, resizable_window);

            window.InitializeUserContent(resizable_window, control, engine);

            resizable_window.BringToTopOfDesktop();



            // todo : change this
            resizable_window.Width = 900;
            resizable_window.Height = 700;
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

            Window.InstallIcon(AppType.JsXaml, type);
        }
        public void InstallJSHTML(string type)
        {
            if (disposing)
                return;
            jsApps.Add(type);
            Window.InstallIcon(AppType.JsHtml, type);
        }
        public void InstallCSWPF(string exePath, Type type)
        {
            var name = exePath.Split('.')[0];
            Window.InstallIcon(AppType.NativeCs, exePath, type);
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
        public async Task OpenCustom(string type, params object[] cmdLineArgs)
        {
            var data = Runtime.GetAppDefinition(type);

            var control = XamlHelper.ParseUserControl(data.XAML);

            if (control == null)
            {
                Notifications.Now($"Error : either the app was not found or there was an error parsing xaml or js for {type}.");
                return;
            }

            Engine engine = new();
            var (id, code) = await InstantiateWindowClass(type, cmdLineArgs, data, engine);

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

        private int startupTimeoutMs = 20_000;

        public static Computer Current => current;
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

            pc.InstallCSharpApp("CommandPrompt.app", typeof(CommandPrompt));
            pc.InstallCSharpApp("FileExplorer.app", typeof(FileExplorer));
            pc.InstallCSharpApp("TextEditor.app", typeof(TextEditor));

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
        public static T? TryGetProcessOfTypeUnsafe<T>() where T : UserControl
        {
            var matchingWindow = Current.UserWindows.Values.FirstOrDefault(window =>
            {
                return window is UserWindow userWindow &&
                       userWindow.ContentsFrame is Frame frame &&
                       frame.Content is T;
            });

            return matchingWindow?.ContentsFrame.Content as T;
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
        private static async Task<(string id, string code)> InstantiateWindowClass(string type, object[] cmdLineArgs, (string XAML, string JS) data, Engine engine)
        {
            var name = type.Split('.')[0];

            var JS = new string(data.JS);

            _ = await engine.Execute(JS);

            var instance_name = "p" + processCount++.ToString();

            engine.AppModule.__SetId(instance_name);

            string instantiation_code;

            if (cmdLineArgs.Length != 0)
            {
                var args = string.Join(", ", cmdLineArgs);
                instantiation_code = $"const {instance_name} = new {name}('{instance_name}, {args}')";
            }
            else
                instantiation_code = $"const {instance_name} = new {name}('{instance_name}')";



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
                    NetworkConfiguration?.StopHosting();
                    NetworkConfiguration?.StopClient();
                    CmdLine?.Dispose();

                    foreach (var item in UserWindows)
                        item.Value.Close();
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
    }
}
