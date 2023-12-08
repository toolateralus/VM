﻿using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.OS;
using Lemur.Windowing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Graphics.ES11;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Lemur
{
    public record Process(UserWindow UI, string ID, string Type)
    {
        public Action? OnProcessTermination;
    }
    public class Computer : IDisposable
    {
        internal string WorkingDir { get; private set; }
        internal uint ID { get; private set; }
        internal static uint processCount;

        [Obsolete("This is a (probably broken) tcp network implementation. It is not especially secure. Use at your own risk, but probably don't use this. it is unused by default")]
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

      
        public void OpenApp(UserControl control, string type, string processID, Engine? engine = null)
        {


            LoadConfig();



            if (ProcessClassTable.ContainsKey(type))
            {
                if (char.IsDigit(type.Last()))
                {
                    int i = int.Parse(type.Last().ToString()) + 1;
                    type.Replace(type.Last(), i.ToString()[0]);
                }
                else
                {
                    type += "1";
                }
            }

            // the resizable is the container that hosts the user app.
            // this is made separate to eliminate annoying and complex boiler plate.
            UserWindow userWindow = Window.OpenAppUI(processID, type, out var resizable_window);

            // 
            if (ComputerWindow.IsValidType(control.GetType().GetMembers()))
                ComputerWindow.AssignComputer(control, resizable_window);

            userWindow.InitializeContent(resizable_window, control, engine);

            Process? process = null;

            process = new Process(userWindow, processID, type);

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
                    List<Process> array = ProcessClassTable[type];

                    process.OnProcessTermination?.Invoke();

                    Window.Desktop.Children.Remove(resizable_window);

                    resizable_window.Content = null;

                    Window.RemoveTaskbarButton(type);

                    array.Remove(process);

                    if (array.Count == 0)
                        ProcessClassTable.Remove(type);
                    else
                        ProcessClassTable[type] = array;
                    
                }
            }

            userWindow.OnAppClosed += OnWindowClosed;

            resizable_window.BringToTopOfDesktop();


            // todo : change this
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
            Window.InstallIcon(AppType.NativeCs, exePath, type);
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
            wnd.desktopBackground.Source = ComputerWindow.LoadImage(FileSystem.GetResourcePath(backgroundPath) ?? "background.png");
        }
        #region Application
        public async Task OpenCustom(string type, params object[] cmdLineArgs)
        {
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

            engine.AppModule.__Attach__Process__ID(processID);

            var code = await InstantiateWindowClass(type, processID, cmdLineArgs, data, engine).ConfigureAwait(true);

            OpenApp(control, type, processID, engine);

            await engine.Execute(code).ConfigureAwait(true);
        }
        internal static string GetNextProcessID()
        {
            return $"p{processCount++}";
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
