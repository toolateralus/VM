using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VM.OS;

namespace VM.OS.FS
{
    // the "command line" for the file explorer, very basic operations with completely unique syntax.
    public class CommandLine
    {
        public Computer Computer;
        public Dictionary<string, Action<object[]?>> Commands = new();
        public Dictionary<string, string> Aliases = new();
        public CommandLine(Computer computer)
        {
            Computer = computer;
            Commands = new()
            {
                { "-root", RootCmd },
                { "-js", RunJs },
            };
        }

        private async void RunJs(object[]? obj)
        {
            if (obj.Length > 0 && obj[0] is string path && File.Exists(path))
            {
                await Computer.OS.JavaScriptEngine.Execute(File.ReadAllText(path));
            }
        }

        internal bool TryCommand(string input)
        {
            if (Commands.TryGetValue(input, out var _cmd))
            {
                _cmd.Invoke(null);
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

                const string ArgsArrayReplacement = " = [..];";
                var index = jsCode.IndexOf(ArgsArrayReplacement);

                if (index != -1)
                {
                    var args = jsCode.Substring(index, ArgsArrayReplacement.Length);

                    var newArgs = $" = [{string.Join(",", str_args)}]";

                    jsCode = jsCode.Replace(args, newArgs);
                }

                Task.Run(async delegate { await Computer.OS.JavaScriptEngine.Execute(jsCode); });
                return true;
            }
            if (Commands.TryGetValue(cmdName, out var cmd))
            {
                cmd.Invoke(str_args);
                return true;
            }
            return false;
        }
        internal void RootCmd(object[]? args)
        {
            Computer.OS.FS.ChangeDirectory(Computer.OS.FS_ROOT);
        }
    }
}
