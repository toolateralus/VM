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
using VM;
using Microsoft.VisualBasic.Devices;

namespace VM
{
    public class Computer
    {
        public NetworkConfiguration Network = null!;

        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;
        public CommandLine CommandLine;
        public JObject Config;

        public Theme Theme = new();
        public Dictionary<string, Type> NativeCSharpApps = new();

        public readonly uint ID;
        public readonly string FS_ROOT;
        public readonly string WORKING_DIR;

        public Action OnShutdown { get; set; }
        public bool Disposing { get; internal set; }

        public void InstallApplication(string exePath, Type type)
        {
            // do we need this collection? it helps us identify already existing apps but it's almost unneccesary,
            // we may be relying on our UI scripts to do too much behavior.
            if (NativeCSharpApps.TryGetValue(exePath, out _))
            {
                Notifications.Now("Tried to install an app that already exists on the computer, try renaming it if this was intended");
                return;
            }
            NativeCSharpApps[exePath] = type;
            Notifications.Now($"{exePath} installed!");
            ComputerWindow window = Runtime.GetWindow(FS.Computer);
            window.RegisterApp(exePath, type);
        }
        public void InitializeEngine(Computer computer)
        {
            JavaScriptEngine = new(computer);

            if (Runtime.GetResourcePath("startup.js") is string AbsPath)
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
        }
        internal static JObject LoadConfig()
        {
            if (Runtime.GetResourcePath("config.json") is string AbsPath)
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
            string configFilePath = Runtime.GetResourcePath("config.json");

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

            Shutdown();
        }
        private void Shutdown()
        {
            Disposing = true;
            JavaScriptEngine?.Dispose();
            OnShutdown?.Invoke();
        }
        internal void FinishInit(ComputerWindow wnd)
        {
            InitializeEngine(this);

            LoadBackground(this, wnd);

            InstallCoreApps(this);

            wnd.Show();

            wnd.Closed += (o, e) =>
            {
                Runtime.Computers.Remove(this);
                Task.Run(() => SaveConfig(Config.ToString()));
                this.Shutdown();
            };
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
            wnd.desktopBackground.Source = ComputerWindow.LoadImage(Runtime.GetResourcePath(backgroundPath) ?? "background.png");
        }
        internal void Print(object? obj)
        {
            JavaScriptEngine?.InteropModule?.print(obj ?? "null");
        }
    }
}
