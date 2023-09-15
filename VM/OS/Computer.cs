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
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

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

        public uint ID { get; private set; }
        public string FS_ROOT { get; set; } = "";
        public string WORKING_DIR { get; set; } = "";
        public bool Disposing { get; set; }

        public void InitializeEngine(Computer computer)
        {
            JavaScriptEngine = new(computer);

            if (FileSystem.GetResourcePath("startup.js") is string AbsPath)
            {
                JavaScriptEngine.ExecuteScript(AbsPath);
            }
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
        public void Boot(uint id)
        {
            if (id > Computers.Length)
            {
                IO.OSTREAM($"Invalid index. {id}");
                IO.OSTREAM("Boot aborted");
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

    public class IO 
    {
        public static Func<string?> ISTREAM { get; private set; }= Console.ReadLine;
        public static Action<object?> OSTREAM { get; private set; } = IO.OSTREAM;
        public static void RedirectOutput(Action<object?> out_stream)
        {
            OSTREAM = out_stream;
        }
        public static void RedirectInput(Func<string?> in_stream)
        {
            ISTREAM = in_stream;
        }
    }

    
}



