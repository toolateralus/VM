using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                new("-root", RootCmd, "navigates the open file explorer to the root directory of the computer."),
                new("-js", RunJs, "runs a js file of name provided, such as myCodeFile to run myCodeFile.js in any directory under ../Appdata/VM"),
                new("-help", Help, "shows a list of all available commands and aliases to the currently open command prompt.")
            };
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
                commandPrompt.output.AppendText($"\n{string.Join(",", item.infos)}");

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
