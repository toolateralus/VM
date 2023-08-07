using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VM.OS;

namespace VM.OS.FS
{
    // the "command line" for the file explorer, very basic operations with completely unique syntax.
    public class CommandLine
    {
        public Computer Computer;
        public Dictionary<string, Action<object[]?>> Commands = new();

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
