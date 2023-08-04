using System;
using System.Collections.Generic;

namespace VM.OPSYS
{
    // the "command line" for the file explorer, very basic operations with completely unique syntax.
    public class Command
    {
        public Computer Computer;
        public Dictionary<string, Action<object[]?>> Commands = new();
                
        public Command(Computer computer)
        {
            Computer = computer;
            Commands = new()
            {
                { "-root", RootCmd },
            };
        }
        internal bool TryCommand(string path, params object[]? args)
        {
            if (Commands.TryGetValue(path, out var cmd))
            {
                cmd.Invoke(args);
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
