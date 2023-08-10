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
                new("root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                new("js", RunJs, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM"),
                new("help", Help, "shows a list of all available commands and aliases to the currently open command prompt."),
                new("ls", ListDir, "lists all dirs in current directory"),
                new("cd", ChangeDir, "navigates to provided path if permitted"),
                new("mkdir", MkDir, "makes directory at provided path if permitted"),
                new("copy", Copy, "copy arg1 to any number of provided paths,\'\n\t\' example: { copy source destination1 destination2 destination3... }"),
                new("clear", Clear, "makes directory at provided path if permitted"),
                new("font", SetFont, "set\'s the command prompt\'s font for this session. call this from a startup to set as default"),
                new("config" ,Config, "config <set or get> <property name> (set only) <new value>")
            };
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

                    foreach (var kvp in Computer.OS.Config)
                    {
                        commandPrompt.output.AppendText($"\n {{{kvp.Key} : {kvp.Value}}}");
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


            var text = listitemformat($"-Current Directory: {Computer.OS.FS.CurrentDirectory} --", list, commandPrompt);
            commandPrompt.output.AppendText(text);
            
        }

        private string listitemformat(string header, string[] data, CommandPrompt commandPrompt)
        {
            StringBuilder product = new();
            product.AppendLine(headerformat(header));
            product.AppendLine(@$"{Environment.NewLine}/*");
            foreach (var file in data)
            {
                product.AppendLine(file);
            }
            product.AppendLine(@$"*/{Environment.NewLine}");
            return product.ToString();
        }
        
        string headerformat(string input)
        {
            return $"\n// // // // //\n{input}\n\t// // // // //";
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
                foreach (string Destination in obj[0..].Where(o => o.GetType() == typeof(string) && !string.IsNullOrEmpty(o as string)).Cast<string>())
                    Computer.OS.FS.Copy(Path, Destination);
            }
        }
        private void MkDir(object[]? obj)
        {
            if (obj != null && obj.Length > 0 && obj[0] is string Path)
            {
                Computer.OS.FS.NewFile(Path);
            }
        }
        private void Help(object[]? obj)
        {
            var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(Computer);

            if (commandPrompt == default)
            {
                Notifications.Now("You must have a cmd prompt open to display 'help' command results.");
                return;
            }

            foreach (var item in Commands)
                commandPrompt.output.AppendText($"\n {{{item.id}}} \n\t\'{string.Join(",", item.infos)}\'");

            foreach (var item in Aliases)
            {
                commandPrompt.output.AppendText($"\n{item.Key} -> {item.Value}");
            }
        }
        private async void RunJs(object[]? obj)
        {
            if (obj.Length > 0 && obj[0] is string path && Runtime.GetResourcePath(path, ".js") is string AbsPath &&  File.Exists(AbsPath))
            {
                await Computer.OS.JavaScriptEngine.Execute(File.ReadAllText(AbsPath));
            }
        }
        internal bool TryCommand(string input)
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
