using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Network;
using Lemur.JS;
using Lemur.Types;
using Lemur.Windowing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lemur.OS.Language {
    /// <summary>
    /// command parser &amp; execution engine for Lemur.
    /// </summary>
    public partial class CommandLine {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="termInput"></param>
        /// <returns>true if command existed & was invoked, false if not found.</returns>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCommand(string termInput) {
            ArgumentNullException.ThrowIfNull(termInput);

            if (termInput.Length == 0)
                throw new ArgumentException("invalid command : ");

            termInput = termInput.Trim();

            if (Commands.TryGetValue(termInput, out var _cmd)) {
                _cmd.Action.Invoke([]);
                return true;
            }

            var cmdName = ParseName(ref termInput);

            // retrim since name leaves dead white space between first arg.
            termInput = termInput.TrimStart();

            var arguments = ParseArguments(termInput);

            var result = false;

            var args = arguments.ToArray();

            if (Aliases.TryGetValue(cmdName, out var alias) && FileSystem.FileExists(alias)) {
                var jsCode = FileSystem.Read(alias);

                jsCode = JavaScriptPreProcessor.InjectCommandLineArgs(args, jsCode);

                ThreadPool.QueueUserWorkItem(async _ => {
                    using var engine = new Engine(computer, cmdName);
                    await engine.Execute(jsCode).ConfigureAwait(false);
                });

                return true;
            }

            result = TryInvoke(cmdName, args);

            return result;

            static string ParseName(ref string termInput) {
                // get command name ie 'grep' ..
                string? cmdName = termInput.Split(' ')[0];

                // remove the command name, parse args
                termInput = termInput[cmdName.Length..];
                return cmdName;
            }
        }

        private static List<string> ParseArguments(string termInput) {
            var arguments = new List<string>();
            StringBuilder builder = new();
            var position = 0;

            while (position < termInput.Length) {
                var ch = termInput[position];
                position++; // consume this character BEFORE the switch.

                switch (ch) {
                    case ' ':
                        arguments.Add(builder.ToString());
                        builder.Clear();
                        continue;
                    case '\'':
                        builder.Append(ParseString('\'', termInput, ref position));
                        continue;
                    case '\"':
                        builder.Append(ParseString('\"', termInput, ref position));
                        continue;
                }

                builder.Append(ch);
            }

            if (builder.Length != 0)
                arguments.Add($"{builder}");

            return arguments;
        }

        private static string ParseString(char delimiter, string termInput, ref int position) {
            var builder = new StringBuilder();

            while (position < termInput.Length) {
                var ch = termInput[position];
                position++;

                if (ch == delimiter)
                    break;

                builder.Append(ch);
            }

            return builder.ToString();
        }

        public bool TryInvoke(string name, params string[] args) {
            if (!Commands.TryGetValue(name, out var cmd))
                return false;

            SafeList<string> args1 = new(args);

            cmd?.Action?.Invoke(args1);

            return cmd?.Action != null;
        }
        public void Dispose() {
            Commands.Clear();
            Aliases.Clear();
        }
    }

    /// <summary>
    /// Set of base commands for Lemur. Feel free to expand / refine these.
    /// Check out SafeList to find out more.
    /// </summary>
    public partial class CommandLine : IDisposable {
        public Dictionary<string, Command> Commands = [];
        public Dictionary<string, string> Aliases = [];
        public bool Disposing;
        public Computer computer;

        public CommandLine(Computer computer) {
            this.computer = computer;

            var assembly = Assembly.GetExecutingAssembly();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (var m in GetType().GetMethods(bindingFlags)) {
                var attr = m.GetCustomAttribute<CommandAttribute>();
                if (attr != null) {
                    var del = (CommandAction)Delegate.CreateDelegate(typeof(CommandAction), this, m);

                    var cmd = new Command(attr.Identifier, del, attr.Info);
                    Commands.Add(cmd.Identifier, cmd);
                }
            }
        }

        [Command("move", "moves a file / changes its name")]
        public void MoveFile(SafeList<string> obj) {
            if (obj.Length < 1) {
                Notifications.Now("Bad arguments to : move");
                return;
            }

            FileSystem.Move(obj[0], obj[1]);
            Notifications.Now($"Moved {obj[0]}->{obj[1]}");
        }
       
        [Command("setbg", "sets the desktop background")]
        public void SetBackground(SafeList<string> obj) {
            if (obj[0] is not string fileName) {
                Notifications.Now($"invalid arg : {obj[0]}");
                return;
            }

            if (fileName.Length == 0) {
                Notifications.Now($"invalid arg {fileName} : cannot provide an empty path ");
                return;
            }

            if (FileSystem.GetResourcePath(fileName) is not string fullPath || string.IsNullOrEmpty(fullPath)) {
                Notifications.Now($"invalid arg {fileName} : could not find file specified ");
                return;
            }

            var computer = Computer.Current;

            computer.Config["BACKGROUND"] = fullPath;
            Computer.SaveConfig(computer.Config.ToString());
            computer.LoadBackground();

        }
        [Command("delete", "deletes a file / folder. use with caution!")]
        public void DeleteFile(SafeList<string> obj) {
            if (obj[0] is string target) {
                FileSystem.Delete(target);
            }
            else {
                Notifications.Now("Invalid input parameters.");
            }
        }
        [Command("ls", "list's the current directory's contents, or list's the provided target directory's contents.")]
        public void ListDir(SafeList<string> obj) {
            var terminal = computer.ProcessManager.TryGetProcessOfType<Terminal>();

            if (terminal == default) {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            var list = Computer.Current.FileSystem.DirectoryListing();

            var textList = string.Join("\n\t", list);

            var text = $"\n##Current Directory: {FileSystem.CurrentDirectory}###\n";
            terminal.output.AppendText(text + "\t");
            terminal.output.AppendText(textList);
        }
        [Command("cd", "changes the computer-wide current directory.")]
        public void ChangeDir(SafeList<string> obj) {
            if (obj[0] is string Path) {
                FileSystem.ChangeDirectory(Path);
            }
            else {
                Notifications.Now("failed cd: bad arguments. usage : cd /path/to/wherever.. or cd .. (to go up one)");
            }
        }
        [Command("copy", "sets the command prompts font for this session. call this from a startup to set as default")]
        public void CopyFile(SafeList<string> obj) {
            if (obj[0] is string path) {
                for (int i = 1; i < obj.Length; i++) {
                    if (obj[i] is not string destination) {
                        Notifications.Now("failed copy: bad arguments. usage : copy source dest.. (one or many destinations)");
                        continue;
                    }

                    if (destination is null || string.IsNullOrEmpty(destination)) {
                        Notifications.Now($"Invalid path {destination} in Copy");
                        continue;
                    }
                    FileSystem.Copy(path, destination);
                    Notifications.Now($"Copied from {path}->{destination}");
                }
            }
            else {
                Notifications.Now("failed cpy: bad arguments. usage : copy source dest.. (one or many destinations)");
            }
        }
        [Command("mkdir", "creates a directory at the given path")]
        public void MakeDir(SafeList<string> obj) {
            if (obj[0] is string Path) {
                FileSystem.NewDirectory(Path);
                Notifications.Now($"Created directory {Path}");
            }
            else {
                Notifications.Now("failed mkdir: bad arguments. usage : 'mkdir directory.'");
            }
        }
        [Command("root", "navigates the open file explorer to the root directory of the computer.")]
        public void RootCmd(SafeList<string> obj) {
            FileSystem.ChangeDirectory(FileSystem.Root);
        }
        [Command("unhost", "if a server is currently running on this machine this halts any active connections and closes the sever.")]
        public void StopHosting(SafeList<string> obj) {
            Computer.Current.Network.StopHosting();
        }
        [Command("--kill-all", "kills all the running processes on the computer, specify an app name like terminal to kill only those app instances, if any.")]
        public void KillAllProcesses(SafeList<string> obj) {
            List<string> toKill = [];

            if (obj != null && obj.Length == 1) {
                if (obj[0] is string name && computer.ProcessManager.ProcessClassTable.TryGetValue(name, out var procClass))
                    toKill.AddRange(procClass.Select(i => i.ID));
                else {
                    Notifications.Now($"No process with name {obj[0]} found.");
                    return;
                }
            }
            else {
                foreach (var procClass in computer.ProcessManager.ProcessClassTable.Values)
                    toKill.AddRange(procClass.Select(i => i.ID));
            }

            foreach (var pid in toKill) {
                var proc = computer.ProcessManager.GetProcess(pid);
                proc?.Terminate();
            }

        }
        [Command("host", "hosts a server on the provided <port>, none provided it will default to 8080")]
        public void StartHosting(SafeList<string> obj) {
            Task.Run(async () => {
                // todo: make it use any port;
                if (await Computer.Current.Network.StartHosting(NetworkConfiguration.defaultPort).ConfigureAwait(false)) {
                    Notifications.Now($"Hosting on {Computer.Current.Network.GetIPPortString()}");
                    return;
                }
                Notifications.Now($"Failed to begin hosting on {LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{NetworkConfiguration.defaultPort}");
            });
        }
        [Command("lp", "lists all the running processes")]
        public void ListProcesses(SafeList<string> obj) {
            foreach (var item in computer.ProcessManager.ProcessClassTable) {
                Notifications.Now($"Process: {item.Key} \n\t PIDs: {string.Join(",", item.Value.Select(i => i.ID))}");
            }
        }
        [Command("ip", "fetches the local ip address of your internet connection")]
        public void GetIPAddress(SafeList<string> obj) {
            var IP = LANIPFetcher.GetLocalIPAddress().MapToIPv4();
            Notifications.Now(IP.ToString());
        }

        [Command("edit", "reads / creates a .js file at provided path, and opens it in the text editor")]
        public void EditTextFile(SafeList<string> obj) {
            if (obj[0] is string fileName) {
                if (FileSystem.GetResourcePath(fileName) is string AbsPath && !string.IsNullOrEmpty(AbsPath)) {
                    var tEdit = new Texed(AbsPath);
                    Computer.Current.PresentGUI(tEdit, "texed.app", computer.ProcessManager.GetNextProcessID());
                }
                else {

                    const string TerminalAppArgument = "-t";
                    switch (obj[1]) {
                        case TerminalAppArgument:
                            FileSystem.Write(fileName,
$@"
/* 
    this file was created as '{fileName}'
        by the 'edit' command 
            at {DateTime.Now} 
                by {Computer.Current.Config["USER"]}
*/

const args = [/***/];

var is_running = true;
while (is_running) {{
    let user_input = read();
    switch (user_input) {{
        case 'exit':
            is_running = false;
            break;
    }}
}}
");
                            break;
                        default:
                            FileSystem.Write(fileName, $@"
/* 
    this file was created as '{fileName}'
        by the 'edit' command 
            at {DateTime.Now} 
                by ??????
*/");
                            break;
                    }


                }
            }
            else {
                Notifications.Now($"Invalid input parameters... got {obj[0]}");
            }
        }
        [Command("config", "config <all|set|get|rm> <prop_name?> <value?>")]
        public void ModifyConfig(SafeList<string> obj) {
            Computer.Current.Config ??= [];

            if (obj is null)
                return;

            if (obj.Length == 0) {
                Notifications.Now("Invalid input parameters.");
                return;
            }

            if (obj[0] is string call) {
                switch (call) {
                    case "save":
                        Computer.SaveConfig(Computer.Current.Config.ToString());
                        return;
                    case "load":
                        Computer.LoadConfig();
                        return;
                    case "all":
                        var terminal = computer.ProcessManager.TryGetProcessOfType<Terminal>();

                        if (terminal == default) {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        StringBuilder outputSb = new();

                        foreach (var kvp in Computer.Current.Config)
                            outputSb.Append($"\n {{{kvp.Key} : {kvp.Value}}}");

                        terminal.output.AppendText(outputSb.ToString());
                        break;
                }

                if (obj.Length < 2)
                    return;

                var propname = (obj[1] as string);

                if (propname is null) {
                    Notifications.Now("Invalid input parameters.");
                    return;
                }

                propname = propname.ToUpper(CultureInfo.CurrentCulture);

                switch (call) {
                    case "rm":

                        if (!Computer.Current.Config.TryGetValue(propname, out var propValue)) {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }

                        Computer.Current.Config.Remove(propname);

                        break;


                    case "get":
                        var terminal = computer.ProcessManager.TryGetProcessOfType<Terminal>();

                        if (terminal == default) {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        if (!Computer.Current.Config.TryGetValue(propname, out propValue)) {
                            Notifications.Now($"Property '{propname}' not found in configuration.");
                            return;
                        }

                        terminal.output.AppendText($"\n {{{propname} : {propValue}}}");
                        break;

                    case "set":
                        string arg = "";

                        arg += string.Join(" ", obj[2..]);

                        arg = arg.Trim();

                        if (arg.First() == '[' && arg.Last() == ']') {
                            var clean = arg.Replace('[', ' ');
                            clean = clean.Replace(']', ' ');

                            var contents = clean.Split(',');

                            List<object> objectArgs = new();

                            foreach (var item in contents) {
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
        [Command("clear", "clears the terminal(s), if open.")]
        public void ClearTerminal(SafeList<string> _) {
            var cmd = computer.ProcessManager.TryGetProcessOfType<Terminal>()?.output;

            Computer.Current.Window.Dispatcher.Invoke(() => {
                cmd?.Clear();
            });

            if (cmd is null)
                Notifications.Now("failed to clear - no cmd prompt open");
        }
        [Command("help", "prints these help listings")]
        public void ShowHelp(SafeList<string> _) {
            var terminal = computer.ProcessManager.TryGetProcessOfType<Terminal>();

            if (terminal == null) {
                return;
            }

            StringBuilder cmdbuilder = new();
            StringBuilder aliasbuilder = new();

            // todo: make this easier to read, add a manual.
            foreach (var item in Computer.Current.CLI.Commands)
                cmdbuilder.Append(CultureInfo.CurrentCulture, $"\n{{{item.Value.Identifier}}} \t\n\'{string.Join(",", item.Value.Info)}\'");

            // todo: add better alias info
            foreach (var item in Computer.Current.CLI.Aliases)
                aliasbuilder.Append(CultureInfo.CurrentCulture, $"\n{item.Key} -> {item.Value.Split('\\').Last()}");

            terminal.Dispatcher.Invoke(() => {
                terminal?.output.AppendText(" ### Native Commands ### ");
                terminal?.output.AppendText(cmdbuilder.ToString());

                terminal?.output.AppendText(" ### Command Aliases ### ");
                terminal?.output.AppendText(aliasbuilder.ToString());
            });

        }
        [Command("run", "runs a JavaScript file of specified path in the computers main engine.")]
        public async void RunJsFile(SafeList<string> obj) {
            ArgumentNullException.ThrowIfNull(obj);
            if (obj[0] is string path 
                && FileSystem.GetResourcePath(
                    path.Replace(".js", "") + ".js") 
                    is string AbsPath 
                && File.Exists(AbsPath)) {
                await Computer.Current.JavaScript.Execute(File.ReadAllText(AbsPath)).ConfigureAwait(false);
            }
            else {
                Notifications.Now("failed run: bad args. usage : run 'path.js'");
            }
        }
    }
}

