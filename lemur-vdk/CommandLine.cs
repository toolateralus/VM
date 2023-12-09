using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.Types;
using Lemur.Windowing;
using Microsoft.Windows.Themes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lemur.OS
{
    public record Command(string Identifier, CommandAction Action, params string[] Info); // oh well!

    public delegate void CommandAction(SafeList<object> args);

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute(string identifier, params string[] Info) : Attribute
    {
        public readonly string[] Info = Info;
        public readonly string Identifier = identifier;
    }
    public class CommandLine : IDisposable
    {
        public List<Command> Commands = new();
        public Dictionary<string, string> Aliases = new();
        private bool Disposing;
        public CommandLine()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var methods = type.GetMethods(bindingFlags);

                foreach (var m in methods)
                {
                    var attr = m.GetCustomAttribute<CommandAttribute>(false);
                    if (attr != null)
                    {
                        var cmd = new Command(attr.Identifier, (CommandAction)Delegate.CreateDelegate(typeof(CommandAction), this, m), attr.Info);
                        Commands.Add(cmd);
                    }
                }
            }
        }


        [Command("unhost", "if a server is currently running on this machine this halts any active connections and closes the sever.")]
        private void Unhost(SafeList<object> obj)
        {
            Computer.Current.NetworkConfiguration.StopHosting();
        }

        [Command("--kill-all", "kills all the running processes on the computer, specify an app name like terminal to kill only those app instances, if any.")]
        private void KillAll(SafeList<object> obj)
        {
            List<string> toKill = [];

            if (obj != null && obj.Length == 1)
            {
                if (obj[0] is string name && Computer.ProcessClassTable.TryGetValue(name, out var procClass))
                    toKill.AddRange(procClass.Select(i => i.ID));
                else
                {
                    Notifications.Now($"No process with name {obj[0]} found.");
                    return;
                }
            }
            else
            {
                foreach (var procClass in Computer.ProcessClassTable.Values)
                    toKill.AddRange(procClass.Select(i => i.ID));
            }

            foreach (var pid in toKill)
            {
                var proc = Computer.GetProcess(pid);
                proc?.UI.Close();
            }

        }

        [Command("dispose", "disposes of the current running JavaScript environment, and instantiates a new one.")]
        private void DisposeJSEnv(SafeList<object> obj)
        {
            if (Computer.Current.JavaScript.Disposing)
            {
                Notifications.Now("You cannot reset the JS environment while it's in the process of disposing.");
                return;
            }

            var oldEngine = Computer.Current.JavaScript;
            oldEngine.Dispose();

            Engine newEngine = new();


            Computer.Current.JavaScript = newEngine;

            if (Computer.Current.JavaScript == newEngine)
            {
                Notifications.Now("Engine successfully swapped");
                return;
            }
            Notifications.Now("Engine swap failed. Please restart your computer.");
        }

        [Command("host", "hosts a server on the provided <port>, none provided it will default to 8080")]
        private void Host(SafeList<object> obj)
        {
            Task.Run(async () =>
            {
                int? port = obj?[0] as int?;
                if (await Computer.Current.NetworkConfiguration.StartHosting(port ?? NetworkConfiguration.defaultPort))
                {
                    Notifications.Now($"Hosting on {Computer.Current.NetworkConfiguration.GetIPPortString()}");
                    return;
                }
                Notifications.Now($"Failed to begin hosting on {LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{port}");
            });
        }


        [Command("move", "moves a file / changes its name")]
        private void Move(SafeList<object> obj)
        {
            string? a = obj[0] as string;
            string? b = obj[1] as string;

            if (a == null || b == null)
            {
                Notifications.Now("Bad arguments to : move");
                return;
            }

            FileSystem.Move(a, b);
            Notifications.Now($"Moved {a}->{b}");
        }

        [Command("lp", "lists all the running processes")]
        private void LP(SafeList<object> obj)
        {
            foreach (var item in Computer.ProcessClassTable)
            {
                Notifications.Now($"Process: {item.Key} \n\t PIDs: {string.Join(",", item.Value.Select(i => i.ID))}");
            }
        }

        [Command("ip", "fetches the local ip address of your internet connection")]
        private void getIP(SafeList<object> obj)
        {
            var IP = LANIPFetcher.GetLocalIPAddress().MapToIPv4();
            Notifications.Now(IP.ToString());
        }

        [Command("delete", "deletes a file / folder. use with caution!")]
        private void Delete(SafeList<object> obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string target)
            {
                FileSystem.Delete(target);
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
        }

        [Command("edit", "reads / creates a .js file at provided path, and opens it in the text editor")]
        private void Edit(SafeList<object> obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string fileName)
            {

                if (FileSystem.GetResourcePath(fileName) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
                {
                    if (!File.Exists(AbsPath))
                    {
                        var str = File.Create(AbsPath);
                        str.Close();
                    }
                    var wnd = Computer.Current.Window;
                    var tEdit = new Texed(AbsPath);
                    Computer.Current.OpenApp(tEdit, "texed.app", Computer.GetNextProcessID());
                }
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
        }

        [Command("config", "config <all|set|get|rm> <prop_name?> <value?>")]
        private void Config(SafeList<object> obj)
        {
            // I am not sure if this is even possible.
            Computer.Current.Config ??= [];

            if (obj is null)
                return;

            if (obj.Length == 0)
            {
                Notifications.Now("Invalid input parameters.");
                return;
            }

            if (obj[0] is string call)
            {
                switch (call)
                {
                    case "save":
                        Computer.SaveConfig(Computer.Current.Config.ToString());
                        return;
                    case "load":
                        Computer.LoadConfig();
                        return;
                    case "all":
                        var commandPrompt = Computer.TryGetProcessOfType<CommandPrompt>();

                        if (commandPrompt == default)
                        {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        StringBuilder outputSb = new();

                        foreach (var kvp in Computer.Current.Config)
                            outputSb.Append($"\n {{{kvp.Key} : {kvp.Value}}}");

                        commandPrompt.output.AppendText(outputSb.ToString());
                        break;
                }
                
                if (obj.Length < 2) 
                    return;

                var propname = (obj[1] as string);

                if (propname is null)
                {
                    Notifications.Now("Invalid input parameters.");
                    return;
                }

                propname = propname.ToUpper(CultureInfo.CurrentCulture);

                switch (call) 
                {
                    case "rm":

                        if (!Computer.Current.Config.TryGetValue(propname, out var propValue))
                        {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }

                        Computer.Current.Config.Remove(propname);

                        break;


                    case "get":
                        var commandPrompt = Computer.TryGetProcessOfType<CommandPrompt>();

                        if (commandPrompt == default)
                        {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        if (!Computer.Current.Config.TryGetValue(propname, out propValue))
                        {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }

                        commandPrompt.output.AppendText($"\n {{{propname} : {propValue}}}");
                        break;

                    case "set":
                        string arg = "";

                        arg += string.Join(" ", obj[2..]);

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

                            var jObject = JObject.FromObject(objectArgs);

                            Computer.Current.Config[propname] = jObject;
                        }

                        Computer.Current.Config[propname] = arg;
                        break;

                   
                }


            }


          

        }

        [Command("ls", "list's the current directory's contents, or list's the provided target directory's contents.")]
        private void ListDir(SafeList<object> obj)
        {
            var commandPrompt = Computer.TryGetProcessOfType<CommandPrompt>();

            if (commandPrompt == default)
            {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            var list = Computer.Current.FileSystem.DirectoryListing();

            var textList = string.Join("\n\t", list);

            var text = $"\n##Current Directory: {FileSystem.CurrentDirectory}###\n";
            commandPrompt.output.AppendText(text + "\t");
            commandPrompt.output.AppendText(textList);
        }
        [Command("cd", "changes the computer-wide current directory.")]
        private void ChangeDir(SafeList<object> obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                FileSystem.ChangeDirectory(Path);
            }
            else
            {
                Notifications.Now("failed cd: bad arguments. usage : cd /path/to/wherever.. or cd .. (to go up one)");
            }
        }
        [Command("clear", "clears the terminal(s), if open.")]
        private void Clear(SafeList<object> obj)
        {
            var cmd = Computer.TryGetProcessOfType<CommandPrompt>()?.output;
            Computer.Current.Window.Dispatcher.Invoke(() => { cmd?.Clear(); });

            if (cmd is null)
                Notifications.Now("failed to clear - no cmd prompt open");
        }
        [Command("copy", "sets the command prompts font for this session. call this from a startup to set as default")]
        private void Copy(SafeList<object> obj)
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
                    FileSystem.Copy(Path, Destination);
                    Notifications.Now($"Copied from {Path}->{Destination}");
                }
            }
            else
            {
                Notifications.Now("failed cpy: bad arguments. usage : copy source dest.. (one or many destinations)");
            }
        }

        [Command("mkdir", "creates a directory at the given path")]
        private void MkDir(SafeList<object> obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                FileSystem.NewDirectory(Path);
                Notifications.Now($"Created directory {Path}");
            }
            else
            {
                Notifications.Now("failed mkdir: bad arguments. usage : 'mkdir directory.'");
            }
        }

        [Command("help", "prints these help listings")]
        private void Help(SafeList<object> obj)
        {
            var commandPrompt = Computer.TryGetProcessOfType<CommandPrompt>();

            StringBuilder cmdbuilder = new();
            StringBuilder aliasbuilder = new();

            foreach (var item in Commands)
                cmdbuilder?.Append($"\n{{{item.Identifier}}} \t\n\'{string.Join(",", item.Info)}\'");

            foreach (var item in Aliases)
                aliasbuilder.Append($"\n{item.Key} -> {item.Value.Split('\\').Last()}");


            commandPrompt?.DrawTextBox(" ### Native Commands ### ");
            commandPrompt?.output.AppendText(cmdbuilder.ToString());

            commandPrompt?.DrawTextBox(" ### Command Aliases ### ");
            commandPrompt?.output.AppendText(aliasbuilder.ToString());

        }

        [Command("run", "run's a JavaScript file of specified path in the computer's main engine.")]
        private async void RunJs(SafeList<object> obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string path && FileSystem.GetResourcePath(path + ".js") is string AbsPath && File.Exists(AbsPath))
            {
                await Computer.Current.JavaScript.Execute(File.ReadAllText(AbsPath));
                Notifications.Now($"running {AbsPath}...");
            }
            else
            {
                Notifications.Now("failed run: bad args. usage : run 'path.js'");
            }
        }

        public bool TryCommand(string input)
        {
            if (Find(input) is Command _cmd && _cmd.Identifier != null && _cmd.Identifier != "NULL" && _cmd.Action != null)
            {
                _cmd.Action.Invoke(new SafeList<object?>([]));
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

                _ = Task.Run(async delegate { await Computer.Current.JavaScript.Execute(jsCode); });
                return true;
            }

            return TryInvoke(cmdName, str_args);
        }
        public Command Find(string name) => Commands.FirstOrDefault(c => c.Identifier == name);
        public bool TryInvoke(string name, string[] args)
        {
            Command cmd = Find(name);
            cmd?.Action?.Invoke(args);
            return cmd?.Action != null;
        }

        [Command("root", "navigates the open file explorer to the root directory of the computer.")]
        public void RootCmd(SafeList<object> obj)
        {
            FileSystem.ChangeDirectory(FileSystem.Root);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
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

