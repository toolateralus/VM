using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lemur.GUI;
using Lemur.Network;
using Lemur.JS;
using Lemur;
using Lemur.FS;

namespace lemur.OS
{
    public class CommandLine : IDisposable
    {
        public List<Command> Commands = new();
        public Dictionary<string, string> Aliases = new();
        private bool Disposing;
        public CommandLine()
        {
            RegisterNativeCommands();
        }
        private void RegisterNativeCommands()
        {
            Commands = new()
            {
                // we need  Edit (open text editor/create new file) and 
                // a LOT of the file system commands are really bad and cause crashes / bad behaviour.

                new("--kill-all", KillAll, "kills all the running processes on the computer, specify an app name like terminal to kill only those app instances, if any."),
                new("root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                new("edit", Edit, "opens the editor for file, or creates a new one if not found."),
                new("delete", Delete, "deletes a file or directory"),
                new("js", RunJs, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/Lemur"),
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("ls", ListDir, "lists all dirs in current directory"),
                new("cd", ChangeDir, "navigates to provided path if permitted"),
                new("mkdir", MkDir, "makes directory at provided path if permitted"),
                new("copy", Copy, "copy arg1 to any number of provided paths,\'\n\t\' example: { copy source destination1 destination2 destination3... }"),
                new("clear", Clear, "makes directory at provided path if permitted"),
                new("font", SetFont, "sets the command prompts font for this session. call this from a startup to set as default"),
                new("config" ,Config, "config <set or get> <property name> (set only) <new value>"),
                new("move", Move, "moves a file/changes its name"),
                new("lp", LP, "lists all the running proccesses"),
                new("dispose", DisposeJSEnv, "disposes of the current running javascript environment, and instantiates a new one."),
                //new("ip", getIP, "fetches the local ip address of wifi/ethernet"),
                //new("host", Host, "hosts a server on the provided <port>, none provided it will default to 8080"),
                //new("unhost", (_) => Computer.Current.Network.StopHosting(_), "if a server is currently running on this machine this halts any active connections and closes the sever."),
            };
        }
        private void KillAll(object[]? obj)
        {
            if (obj != null && obj.Length == 1 && obj[0] is string name && Computer.ProcessLookupTable.TryGetValue(name, out var pids_arg))
            {
                foreach (var pid in pids_arg)
                    Computer.Current.Windows[pid].Close();
                return;
            }

            foreach (var pids in Computer.ProcessLookupTable.Values)
                foreach (var pid in pids)
                    Computer.Current.Windows[pid].Close();
        }
        private void DisposeJSEnv(object[]? obj)
        {
            if (Computer.Current.javaScript.Disposing)
            {
                Notifications.Now("You cannot reset the JS environment while it's in the process of disposing.");
                return;
            }

            var oldEngine = Computer.Current.javaScript;
            oldEngine.Dispose();

            Engine newEngine = new(Computer.Current);
            Computer.Current.javaScript = newEngine;

            if (Computer.Current.javaScript == newEngine)
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
                if (await Computer.Current.Network.StartHosting(port ?? NetworkConfiguration.DEFAULT_PORT))
                {
                    Notifications.Now($"Hosting on {Computer.Current.Network.GetIPPortString()}");
                    return;
                }
                Notifications.Now($"Failed to begin hosting on {LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{port}");
            });
        }
        private void Move(object[]? obj)
        {
            string? a = obj[0] as string;
            string? b = obj[1] as string;

            if (a == null || b == null)
            {
                Notifications.Now("Bad arguments to : move");
                return;
            }

            Computer.Current.fileSystem.Move(a, b);
            Notifications.Now($"Moved {a}->{b}");
        }
        private void LP(object[]? obj)
        {
            foreach (var item in Computer.Current.Windows)
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
                Computer.Current.fileSystem.Delete(target);
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
        }
        private void Edit(object[]? obj)
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
                    var tEdit = new TextEditor(AbsPath);
                    Computer.Current.OpenApp(tEdit);
                }
            }
            else
            {
                Notifications.Now("Invalid input parameters.");
            }
        }
        private void Config(object[]? obj)
        {

            Computer.Current.config ??= new();

            if (obj != null && obj.Length > 0 && obj[0] is string getset)
            {
                string toLower = getset.ToLower();

                if (obj.Length > 1 && obj[1] is string propname)
                {
                    propname = propname.ToUpper();

                    if (toLower == "get")
                    {
                        var commandPrompt = Computer.TryGetProcess<CommandPrompt>();

                        if (commandPrompt == default)
                        {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        if (!Computer.Current.config.TryGetValue(propname, out var propValue))
                        {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }

                        commandPrompt.output.AppendText($"\n {{{propname} : {propValue}}}");
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

                            var jObject = JObject.FromObject(objectArgs);

                            Computer.Current.config[propname] = jObject;
                        }

                        Computer.Current.config[propname] = arg;
                    }
                }
                else if (toLower == "all")
                {
                    var commandPrompt = Computer.TryGetProcess<CommandPrompt>();

                    if (commandPrompt == default)
                    {
                        Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                        return;
                    }
                    StringBuilder sb = new();
                    foreach (var kvp in Computer.Current.config)
                    {
                        sb.Append($"\n {{{kvp.Key} : {kvp.Value}}}");
                    }
                    commandPrompt.output.AppendText(sb.ToString());
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
        private void SetFont(object[]? obj)
        {
            var commandPrompt = Computer.TryGetProcess<CommandPrompt>();

            if (commandPrompt == default)
            {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            if (obj != null)
            {
                // args are seperated by white space so we reconstruct input.
                string FontName = "";
                foreach (var fontNameWord in obj)
                {
                    if (fontNameWord != null && fontNameWord is string fontName)
                        FontName += $" {fontName}";
                }


                System.Windows.Media.FontFamily font = null;
                try
                {
                    font = new System.Windows.Media.FontFamily(FontName);
                }
                catch (Exception ex)
                {
                    Notifications.Now($"Font '{FontName}' not found: {ex.Message}");
                    return;
                }

                commandPrompt.output.FontFamily = font;
                commandPrompt.input.FontFamily = font;
                Notifications.Now($"Font '{FontName}' set successfully.");
            }
            else
            {
                Notifications.Now("Font name not provided.");
            }
        }
        private void ListDir(object[]? obj)
        {
            var commandPrompt = Computer.TryGetProcess<CommandPrompt>();

            if (commandPrompt == default)
            {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            var list = Computer.Current.fileSystem.DirectoryListing();

            var textList = string.Join("\n\t", list);

            var text = $"\n##Current Directory: {Computer.Current.fileSystem.CurrentDirectory}###\n";
            commandPrompt.output.AppendText(text + "\t");
            commandPrompt.output.AppendText(textList);
        }
        private void ChangeDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.Current.fileSystem.ChangeDirectory(Path);
            }
            else
            {
                Notifications.Now("failed cd: bad arguments. usage : cd /path/to/wherever.. or cd .. (to go up one)");
            }
        }
        private void Clear(object[]? obj)
        {
            var cmd = Computer.TryGetProcess<CommandPrompt>()?.output;
            Computer.Current.Window.Dispatcher.Invoke(() => { cmd?.Clear(); });

            if (cmd is null)
                Notifications.Now("failed to clear - no cmd prompt open");
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
                    Computer.Current.fileSystem.Copy(Path, Destination);
                    Notifications.Now($"Copied from {Path}->{Destination}");
                }
            }
            else
            {
                Notifications.Now("failed cpy: bad arguments. usage : copy source dest.. (one or many destinations)");
            }
        }
        private void MkDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.Current.fileSystem.NewFile(Path);
                Notifications.Now($"Created directory {Path}");
            }
            else
            {
                Notifications.Now("failed mkdir: bad arguments. usage : 'mkdir directory.'");
            }
        }
        private void Help(params object[]? obj)
        {
            if (obj?.Length > 0)
            {
                GetSpecificHelp(obj);
            }

            var commandPrompt = Computer.TryGetProcess<CommandPrompt>();

            StringBuilder cmdbuilder = new();
            StringBuilder aliasbuilder = new();

            foreach (var item in Commands)
                cmdbuilder?.Append($"\n{{{item.id}}} \t\n\'{string.Join(",", item.infos)}\'");

            foreach (var item in Aliases)
                aliasbuilder.Append($"\n{item.Key} -> {item.Value.Split('\\').Last()}");


            commandPrompt?.DrawTextBox(" ### Native Commands ### ");
            commandPrompt?.output.AppendText(cmdbuilder.ToString());

            commandPrompt?.DrawTextBox(" ### Command Aliases ### ");
            commandPrompt?.output.AppendText(aliasbuilder.ToString());

        }
        private void GetSpecificHelp(params object[] parameters)
        {
            var name = parameters[0] as string;
            List<string> args = new();

            for (int i = 0; i < parameters.Length; ++i)
                if (parameters[i] is string arg)
                    args.Add(arg);
        }
        private async void RunJs(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string path && FileSystem.GetResourcePath(path + ".js") is string AbsPath && File.Exists(AbsPath))
            {
                await Computer.Current.javaScript.Execute(File.ReadAllText(AbsPath));
                Notifications.Now($"running {AbsPath}...");
            }
            else
            {
                Notifications.Now("failed run: bad args. usage : run 'path.js'");
            }
        }

        // todo: clean up this nightmare.
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

                _ = Task.Run(async delegate { await Computer.Current.javaScript.Execute(jsCode); });
                return true;
            }

            return TryInvoke(cmdName, str_args);
        }
        public Command Find(string name) => Commands.FirstOrDefault(c => c.id == name);
        public bool TryInvoke(string name, string[] args)
        {
            Command cmd = Find(name);
            cmd.Method?.Invoke(args);
            // if it wasn't null then as far as we're concerned we've invoked it 
            // the fullest extent.
            return cmd.Method != null;
        }
        public void RootCmd(object[]? args)
        {
            Computer.Current.fileSystem.ChangeDirectory(Computer.Current.FileSystemRoot);
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

