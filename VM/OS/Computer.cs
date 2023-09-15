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
using VM.Lang;

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

        private void HostServer(object[]? obj)
        {
            Task.Run(async () =>
            {
                int? port = obj?[0] as int?;
                if (await Network.StartHosting(port ?? NetworkConfiguration.DEFAULT_PORT))
                {
                    IO.WriteLine($"Hosting on {Network.GetIPPortString()}");
                    return;
                }
                IO.WriteLine($"Failed to begin hosting on {LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{port}");
            });
        }
        private void ListProcesses(object[]? obj)
        {
            foreach (var item in USER_WINDOW_INSTANCES)
            {
                IO.WriteLine($"\n{item.Key}");
            }

        }
        private void FetchIP(object[]? obj)
        {
            var IP = LANIPFetcher.GetLocalIPAddress().MapToIPv4();
            IO.WriteLine(IP.ToString());
        }

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
                        IO.WriteLine($"Error loading JSON: {ex.Message}");
                    }
                }
                else
                {
                    IO.WriteLine("JSON file not found.");
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
                    IO.WriteLine($"Error saving JSON config: {ex.Message}");
                }
            }
        }
        public void Exit(int exitCode)
        {
            if (exitCode != 0)
            {
                IO.WriteLine($"Computer {ID} has exited, most likely due to an error. code:{exitCode}");
            }
            Dispose();
        }
        public void Boot(uint id)
        {
            if (id > Computers.Length)
            {
                IO.WriteLine($"Invalid index. {id}");
                IO.WriteLine("Boot aborted");
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

            CommandLine.LoadCommandSet( 
                "network commands",
                new("ip", FetchIP, "fetches the local ip address of wifi/ethernet"),
                new("lp", ListProcesses, "lists all the running proccesses"),
                new("host", HostServer, "hosts a server on the provided <port>, none provided it will default to 8080"),
                new("unhost", (args) => Network.StopHosting(args), "if a server is currently running on this machine this halts any active connections and closes the sever.")
            );

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

        public static void WriteLine(object? o) => OSTREAM?.Invoke(o);
        public static string? ReadLine() => ISTREAM?.Invoke();
        public static Action? CSTREAM { get; private set; }
        public static Func<string?>? ISTREAM { get; private set; }= Console.ReadLine;
        public static Action<object?>? OSTREAM { get; private set; } = Console.WriteLine;
        static Dictionary<long, Action> InputHandlerDisconnects = new();
        static Dictionary<long, Action> ClearHandlerDisconnects = new();
        static Dictionary<long, Action> OutputStreamDisconnects = new();
        public static long AddOutput(Action<object> value)
        {
            var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            OutputStreamDisconnects[id] = 
            () =>
            {
                IO.OSTREAM -= value;
            };
                

            IO.OSTREAM += value;

            return id;
        }
        public static void RemoveOutput(long io_handle)
        {
            if (OutputStreamDisconnects.TryGetValue(io_handle, out var disconnect)){
                disconnect?.Invoke();
                OutputStreamDisconnects.Remove(io_handle);
            }
        }
        
        internal static void RequestClear()
        {
            CSTREAM?.Invoke();
            Console.Clear();
        }
        public static long AddClearHandler(Action value)
        {

            var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            ClearHandlerDisconnects[id] = 
            () =>
            {
                IO.CSTREAM -= value;
            };
                
            CSTREAM += value;


            return id;
        }
        public static void RemoveClearHandler(long io_handle_clear)
        {
           if (ClearHandlerDisconnects.TryGetValue(io_handle_clear, out var disconnect)){
                disconnect?.Invoke();
                ClearHandlerDisconnects.Remove(io_handle_clear);
            }
        }
        
        public static long AddInput(Func<string?> value)
        {
           var id = Random.Shared.NextInt64();
            
            // setup disconnecter to prevent memory leaks
            // from closing / opening sources
            InputHandlerDisconnects[id] = 
            () =>
            {
                IO.ISTREAM -= value;
            };
                

            IO.ISTREAM += value;

            return id;

        }
        public static void UnhookInput(long handle)
        {
            if (InputHandlerDisconnects.TryGetValue(handle, out var disconnect)){
                disconnect?.Invoke();
                InputHandlerDisconnects.Remove(handle);
            }
        }

    }

    
}



