using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using VM.GUI;
using VM.OS;
using VM.OS.Network;
using VM.OS.Network.Server;

namespace VM.OS.FS
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

    // the "command line" for the file explorer, very basic operations with completely unique syntax.
    public class CommandLine
    {
        public Computer Computer;
        public List<Command> Commands = new();
        public Dictionary<string, string> Aliases = new();

        public CommandLine(Computer computer)
        {
            Computer = computer;
            Commands = new()
            {
                // we need Delete, Edit (open text editor/create new file)
                new("root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                new("edit", Edit, "opens the editor for file, or creates a new one if not found."),
                new("delete", Delete, "deletes a file or directory"),
                new("js", RunJs, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM"),
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("ls", ListDir, "lists all dirs in current directory"),
                new("cd", ChangeDir, "navigates to provided path if permitted"),
                new("mkdir", MkDir, "makes directory at provided path if permitted"),
                new("copy", Copy, "copy arg1 to any number of provided paths,\'\n\t\' example: { copy source destination1 destination2 destination3... }"),
                new("clear", Clear, "makes directory at provided path if permitted"),
                new("font", SetFont, "sets the command prompts font for this session. call this from a startup to set as default"),
                new("config" ,Config, "config <set or get> <property name> (set only) <new value>"),
                new("ip", getIP, "fetches the local ip address of wifi/ethernet"),
                new("move", Move, "moves a file/changes its name"),
                new("restart", (_) => Runtime.Restart(computer.ID()), "restarts this computer"),
                new("lp", LP, "lists all the running proccesses"),
                new("host", Host, "hosts a server on the provided <port>, none provided it will default to 8080"),
                new("unhost", (_) => Computer.Network.StopHosting(_), "if a server is currently running on this machine this halts any active connections and closes the sever.")
            };
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
            Computer.OS.FS.Move(a, b);
            Notifications.Now($"Moved {a}->{b}");
        }
        private void LP(object[]? obj)
        {
            foreach (var item in Runtime.GetWindow(Computer).USER_WINDOW_INSTANCES)
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
                Computer.OS.FS.Delete(target);
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

                if (Runtime.GetResourcePath(fileName) is string AbsPath && AbsPath != null)
                {
                    if (!File.Exists(AbsPath))
                    {
                        var str = File.Create(AbsPath);
                        str.Close();
                    }
                    var wnd = Runtime.GetWindow(Computer);
                    var tEdit = new TextEditor(Computer, AbsPath);
                    wnd.OpenApp(tEdit);
                } 
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
                        var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

                        if (commandPrompt == default)
                        {
                            Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                            return;
                        }

                        if (!Computer.OS.Config.TryGetValue(propname, out var propValue))
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

                            var jObject =JObject.FromObject(objectArgs);

                            Computer.OS.Config[propname] = jObject;
                        }

                        Computer.OS.Config[propname] = arg;
                    }
                }
                else if (toLower == "all")
                {
                    var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

                    if (commandPrompt == default)
                    {
                        Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                        return;
                    }
                    StringBuilder sb = new();
                    foreach (var kvp in Computer.OS.Config)
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
            var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

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
            var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

            if (commandPrompt == default)
            {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            var list = Computer.OS.FS.DirectoryListing();

            var textList = string.Join("\n\t", list);

            var text = $"\n##Current Directory: {Computer.OS.FS.CurrentDirectory}###\n";
            commandPrompt.output.AppendText(text + "\t");
            commandPrompt.output.AppendText(textList);
        }
        private void ChangeDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.OS.FS.ChangeDirectory(Path);
            }
        }
        private void Clear(object[]? obj)
        {
            Runtime.SearchForOpenWindowType<CommandPrompt>(Computer)?.output?.Clear();
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
                    Computer.OS.FS.Copy(Path, Destination);
                    Notifications.Now($"Copied from {Path}->{Destination}");
                }
            }
        }
        private void MkDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.OS.FS.NewFile(Path); 
                Notifications.Now($"Created directory {Path}");
            }
        }
        private void Help(params object[]? obj)
        {
            if (obj?.Length > 0)
            {
                GetSpecificHelp(obj);
            }

            var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

            if (commandPrompt == default)
            {
                //Runtime.GetWindow(Computer).Open();
            }

            StringBuilder cmdbuilder = new();
            StringBuilder aliasbuilder = new();

            foreach (var item in Commands)
                cmdbuilder.Append($"\n{{{item.id}}} \t\n\'{string.Join(",", item.infos)}\'");

            foreach (var item in Aliases)
                aliasbuilder.Append($"\n{item.Key} -> {item.Value.Split('\\').Last()}");

            commandPrompt.output.AppendText(cmdbuilder.ToString());
            commandPrompt.output.AppendText(aliasbuilder.ToString());

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
            if (obj.Length > 0 && obj[0] is string path && Runtime.GetResourcePath(path + ".js") is string AbsPath &&  File.Exists(AbsPath))
            {
                await Computer.OS.JavaScriptEngine.Execute(File.ReadAllText(AbsPath));
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

                Task.Run(async delegate { await Computer.OS.JavaScriptEngine.Execute(jsCode); });
                return true;
            }

            TryInvoke(cmdName, str_args);
           
            return false;
        }
        public Command Find(string name)
        {
            if (Commands.Where(c => c.id == name).FirstOrDefault((Command)default) is Command cmd)
            {
                return cmd;
            }
            return default;
        }
        public void TryInvoke(string name, string[] args)
        {
            if (Find(name) is Command command && command.id != null && command.id != "NULL" && command.Method != null)
            {
                command.Method.Invoke(args);
            }
        }
        internal void RootCmd(object[]? args)
        {
            Computer.OS.FS.ChangeDirectory(Computer.OS.FS_ROOT);
        }
    }
}
