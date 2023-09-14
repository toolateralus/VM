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
            RegisterNativeCommands(computer);
        }
        private void RegisterNativeCommands(Computer computer)
        {
            Commands = new()
            {
                // we need Delete, Edit (open text editor/create new file)
                new("root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                // new("edit", Edit, "opens the editor for file, or creates a new one if not found."),
                new("delete", Delete, "deletes a file or directory"),
                new("js", RunJs, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM"),
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("ls", ListDir, "lists all dirs in current directory"),
                new("cd", ChangeDir, "navigates to provided path if permitted"),
                new("mkdir", MkDir, "makes directory at provided path if permitted"),
                new("copy", Copy, "copy arg1 to any number of provided paths,\'\n\t\' example: { copy source destination1 destination2 destination3... }"),
                new("clear", Clear, "makes directory at provided path if permitted"),
                new("config" ,Config, "config <set or get> <property name> (set only) <new value>"),
                new("ip", getIP, "fetches the local ip address of wifi/ethernet"),
                new("move", Move, "moves a file/changes its name"),
                new("lp", LP, "lists all the running proccesses"),
                new("host", Host, "hosts a server on the provided <port>, none provided it will default to 8080"),
                new("unhost", (_) => Computer.Network.StopHosting(_), "if a server is currently running on this machine this halts any active connections and closes the sever."),
                new("dispose", DisposeJSEnv, "disposes of the current running javascript environment, and instantiates a new one.")
            };
        }
        private void DisposeJSEnv(object[]? obj)
        {
            if (Computer.JavaScriptEngine.Disposing)
            {
                Notifications.Now("You cannot reset the JS environment while it's in the process of disposing.");
                return;
            }

            var oldEngine = Computer.JavaScriptEngine;
            oldEngine.Dispose();

            JavaScriptEngine newEngine = new(Computer);
            Computer.JavaScriptEngine = newEngine;

            if (Computer.JavaScriptEngine == newEngine)
            {
                Notifications.Now("Engine successfully swapped");
                return;
            }
            Notifications.Now("Engine swap failed. Please restart your computer.");
        }
        private void Host(object[]? obj)
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
        private void Move(object[]? obj)
        {
            string? a = obj[0] as string;
            string? b = obj[1] as string;
            Computer.FS.Move(a, b);
            Notifications.Now($"Moved {a}->{b}");
        }
        private void LP(object[]? obj)
        {
            foreach (var item in Computer.USER_WINDOW_INSTANCES)
            {
                Notifications.Now($"\n{item.Key}");
            }

        }
        private void getIP(object[]? obj)
        {
            var IP = LANIPFetcher.GetLocalIPAddress().MapToIPv4();
            Notifications.Now(IP.ToString());
        }
        private void Delete(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string target)
            {
                Computer.FS.Delete(target);
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
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
                        // TODO: Add output stream call here, or just print.
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
                 
                    StringBuilder sb = new();
                    foreach (var kvp in Computer.Config)
                    {
                        sb.Append($"\n {{{kvp.Key} : {kvp.Value}}}");
                    }
                    // TODO: add output stream / print
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
       
        private void ListDir(object[]? obj)
        {
           
            var list = Computer.FS.DirectoryListing();

            var textList = string.Join("\n\t", list);

            var text = $"\n##Current Directory: {Computer.FS.CurrentDirectory}###\n";
            
            // TODO: add output stream / print / clear
        }
        private void ChangeDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.FS.ChangeDirectory(Path);
            }
        }
        private void Clear(object[]? obj)
        {
           // TODO: add output stream / print / clear
        }
        private void Copy(object[]? obj)
        {
            if (obj != null && obj.Length > 1 && obj[0] is string Path)
            {
                for (int i = 1; i < obj.Length; i++)
                {
                    string Destination = obj[i] as string;
                    
                    if (Destination is null || string.IsNullOrEmpty(Destination))
                    {
                        Notifications.Now($"Invalid path {Destination} in Copy");
                        continue;
                    }
                    Computer.FS.Copy(Path, Destination);
                    Notifications.Now($"Copied from {Path}->{Destination}");
                }
            }
        }
        private void MkDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.FS.NewFile(Path); 
                Notifications.Now($"Created directory {Path}");
            }
        }
        private void Help(params object[]? obj)
        {
            if (obj?.Length > 0)
            {
                GetSpecificHelp(obj);
            }

            Console.WriteLine(" ### Native Commands ### ");

            foreach (var item in Commands)
                Console.WriteLine($"\n{{{item.id}}} \t\n\'{string.Join(",", item.infos)}\'");

            foreach (var item in Aliases)
                Console.WriteLine($"\n{item.Key} -> {item.Value.Split('\\').Last()}");
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
        private async void RunJs(object[]? obj)
        {
            if (obj.Length > 0 && obj[0] is string path && FileSystem.GetResourcePath(path + ".js") is string AbsPath &&  File.Exists(AbsPath))
            {
                await Computer.JavaScriptEngine.Execute(File.ReadAllText(AbsPath));
                Notifications.Now($"running {AbsPath}...");
            }
        }
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
        public void RootCmd(object[]? args)
        {
            Computer.FS.ChangeDirectory(Computer.FS_ROOT);
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
    }
}

