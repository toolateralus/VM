using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VM.Network;
using VM.JS;
using System.Net;

namespace VM.FS
{
    public struct Command
    {
        public string id = "NULL";
        public Action<object[]?> Method;
        public string[] infos = Array.Empty<string>();
        public Command(string id, Action<object[]?> method, params string[]? infos)
        {
            this.id = id;
            this.Method = method;

            if (infos != null)
            {
                this.infos = this.infos.Concat(infos).ToArray();
            }

        }
    }
    public class CommandLine : IDisposable
    {
        public Computer Computer;
        public List<Command> Commands = new();
        public Dictionary<string, string> Aliases = new();
        private bool Disposing;

        public CommandLine(Computer computer)
        {
            Computer = computer;
            RegisterNativeCommands();
        }
        private void RegisterNativeCommands()
        {
            LoadCommandSet(
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("config" ,Config, "config .. {<set or get>} ..  {<property name>} .. {(new value is for set only) <new value>}"),
                new("ip", FetchIP, "fetches the local ip address of wifi/ethernet"),
                new("lp", ListProcesses, "lists all the running proccesses"),
                new("js", RunJavaScriptSourceFile, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM"),
                new("host", HostServer, "hosts a server on the provided <port>, none provided it will default to 8080"),
                new("unhost", (args) => Computer.Network.StopHosting(args), "if a server is currently running on this machine this halts any active connections and closes the sever.")
            );
        }
        #region  DEFAULT COMMANDS
        // These should get moved to their respective locations, it's unneccesary to make all the commands statically in this class when it presents complexity,
        // you should be making commands in their context. it's much simpler anyway.
        private void HostServer(object[]? obj)
        {
            Task.Run(async () =>
            {
                int? port = obj?[0] as int?;
                if (await Computer.Network.StartHosting(port ?? NetworkConfiguration.DEFAULT_PORT))
                {
                    Notifications.Now($"Hosting on {Computer.Network.GetIPPortString()}");
                    return;
                }
                Notifications.Now($"Failed to begin hosting on {LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{port}");
            });
        }
        private void ListProcesses(object[]? obj)
        {
            foreach (var item in Computer.USER_WINDOW_INSTANCES)
            {
                Notifications.Now($"\n{item.Key}");
            }

        }
        private void FetchIP(object[]? obj)
        {
            var IP = LANIPFetcher.GetLocalIPAddress().MapToIPv4();
            Notifications.Now(IP.ToString());
        }
        private void Config(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string getset)
            {
                string toLower = getset.ToLower();

                if (obj.Length > 1 && obj[1] is string propname)
                {
                    propname = propname.ToUpper();

                    if (toLower == "get")
                    {
                        if (!Computer.Config.TryGetValue(propname, out var propValue))
                        {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }
                        System.Console.WriteLine($"propname : {propValue}");
                    }
                    else if (toLower == "set" && obj.Length > 2)
                    {
                        string arg = "";

                        // join last args
                        foreach (var item in obj[2..])
                        {
                            arg += $" {item}";
                        }

                        arg = arg.Trim();

                        if (arg.First() == '[' && arg.Last() == ']')
                        {
                            var clean = arg.Replace('[', ' ');
                            clean = clean.Replace(']', ' ');

                            var contents = clean.Split(',');

                            List<object> objectArgs = new();

                            foreach (var item in contents)
                            {
                                if (float.TryParse(item, out var floatingpt))
                                    objectArgs.Add(floatingpt);
                                else if (int.TryParse(item, out var integer))
                                    objectArgs.Add(integer);
                                else if (bool.TryParse(item, out var boolean))
                                    objectArgs.Add(boolean);
                            }

                            var jObject =JObject.FromObject(objectArgs);

                            Computer.Config[propname] = jObject;
                        }

                        Computer.Config[propname] = arg;
                    }
                }
                else if (toLower == "all")
                {
                    foreach (var kvp in Computer.Config)
                    {
                        Console.WriteLine($"\n {{{kvp.Key} : {kvp.Value}}}");
                    }
                }
                else
                {
                    Notifications.Now("Invalid operation specified.");
                }
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
        }
        private void Help(params object[]? obj)
        {
            if (obj?.Length > 0)
            {
                GetSpecificHelp(obj);
                return;
            }

            Console.WriteLine(" ### Native Commands ### ");
            Console.WriteLine();
            // Determine the maximum width of the tags
            int maxTagWidth = Commands.Max(item => item.id.Length);

            foreach (var item in Commands)
            {
                string paddedTag = item.id.PadRight(maxTagWidth);
                Console.WriteLine($"{paddedTag} '{string.Join(",", item.infos).ToUpper()}'");
            }

            foreach (var item in Aliases)
            {
                // Split the alias to get the last part for padding
                string alias = item.Value.Split('/').Last();
                string paddedAlias = item.Key.PadRight(maxTagWidth);
                Console.WriteLine($"{paddedAlias} -> {alias}");
            }
        }
        private void GetSpecificHelp(params object[] parameters)
        {
            var name = parameters[0] as string;
            List<string> args = new();
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i] is string arg)
                {
                    args.Add(arg);
                }
            }
        }
        private async void RunJavaScriptSourceFile(object[]? obj)
        {
            if (obj.Length > 0 && obj[0] is string path && FileSystem.GetResourcePath(path + ".js") is string AbsPath &&  File.Exists(AbsPath))
            {
                await Computer.JavaScriptEngine.Execute(File.ReadAllText(AbsPath));
                Notifications.Now($"running {AbsPath}...");
            }
        }
        #endregion
        public bool TryCommand(string input)
        {
            if (Find(input) is Command _cmd && _cmd.id != null && _cmd.id != "NULL" && _cmd.Method != null)
            {
                _cmd.Method.Invoke(null);
                return true;
            }

            string[] split = input.Split(' ');

            if (split.Length == 0)
                return false;

            string cmdName = split.First();
            var str_args = split[1..];

            if (Aliases.TryGetValue(cmdName, out var alias) && File.Exists(alias))
            {
                var jsCode = File.ReadAllText(alias);

                const string ArgsArrayReplacement = "[/***/]";
                var index = jsCode.IndexOf(ArgsArrayReplacement);

                if (index != -1)
                {
                    var args = jsCode.Substring(index, ArgsArrayReplacement.Length);

                    var newArgs = $"[{string.Join(",", str_args)}]";

                    jsCode = jsCode.Replace(args, newArgs);
                }

                Task.Run(async delegate { await Computer.JavaScriptEngine.Execute(jsCode); });
                return true;
            }

            return TryInvoke(cmdName, str_args);
        }
        public Command Find(string name)
        {
            if (Commands.Where(c => c.id == name).FirstOrDefault((Command)default) is Command cmd)
            {
                return cmd;
            }
            return default;
        }
        internal bool TryInvoke(string name, string[] args)
        {
            if (Find(name) is Command command && command.id != null && command.id != "NULL" && command.Method != null)
            {
                command.Method.Invoke(args);
                return true;
            }
            return false;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    Computer = null!;
                    Commands.Clear();
                    Aliases.Clear();
                }
                Disposing = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        internal void LoadCommandSet(params Command[] filesystem_commands)
        {
            Commands ??= new();
            Commands.AddRange(filesystem_commands);
        }
    }
}

