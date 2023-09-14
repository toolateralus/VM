using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

using VM.Network;
using VM.FS;
using VM.JS;
using System.Threading;
using System.Linq;

namespace VM
{
    public class Computer : IDisposable
    {
        /// <summary>
        /// I can't imagine why you'd need more than two computers, so I multiply that by six. xD
        /// </summary>
        public static Computer[] Computers = new Computer[12];
        public NetworkConfiguration Network = null!;
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;
        public CommandLine CommandLine;
        public JObject Config;
        public readonly Dictionary<string, object> USER_WINDOW_INSTANCES = new();
        public readonly List<string> InstalledJSApps = new();
        public Dictionary<string, Type> Installed_CSharp_Apps { get; private set; } = new();

        public uint ID { get; private set; }
        public string FS_ROOT { get; private set; }
        public string WORKING_DIR { get; private set; }
        public bool Disposing { get; set; }

        public void InstallApplication(string exePath, Type type)
        {
            // do we need this collection? it helps us identify already existing apps but it's almost unneccesary,
            // we may be relying on our UI scripts to do too much behavior.
            if (Installed_CSharp_Apps.TryGetValue(exePath, out _))
            {
                Notifications.Now("Tried to install an app that already exists on the computer, try renaming it if this was intended");
                return;
            }
            Notifications.Now($"{exePath} installed!");
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
       
        public static JObject LoadConfig()
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
        public void Exit(int exitCode)
        {
            if (exitCode != 0)
            {
                Notifications.Now($"Computer {ID} has exited, most likely due to an error. code:{exitCode}");
            }
            Dispose();
        }
        public void Print(object? obj)
        {
            JavaScriptEngine?.InteropModule?.print(obj ?? "null");
        }

        #region Application
        public void Boot(uint id)
        {
            if (id > Computers.Length){
                System.Console.WriteLine($"Invalid index. {id}");
                System.Console.WriteLine("Boot aborted");
                return;
            }

            CommandLine = new(this);

            var WORKING_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/VM";

            this.WORKING_DIR = Path.GetFullPath(WORKING_DIR);
            
            // prepare the root dir for the FileSystem, since we add a dir to contain that itself.
            FS_ROOT = $"{this.WORKING_DIR}/computer{id}";

            FS = new(FS_ROOT, this);

            Config = LoadConfig();

            Network = new(this);

            InitializeEngine(this);

            Computers[id] = this;
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
                    Network?.Dispose();
                    FS?.Dispose();
                    CommandLine?.Dispose();
                }
                
                JavaScriptEngine = null!;
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
