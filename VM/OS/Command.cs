using System;
using System.Collections.Generic;

namespace VM.OPSYS
{
    // the "command line" for the file explorer, very basic operations with completely unique syntax.
    public static class Command
    {
        public static Dictionary<string, Action<object[]?>> Commands = new(){
            { "-root", RootCmd },
        };
        static internal void RootCmd(object[]? args)
        {
            OS.Current.FileSystem.ChangeDirectory(OS.ROOT);
        }

        internal static bool TryCommand(string path, params object[]? args)
        {
            if (Commands.TryGetValue(path, out var cmd))
            {
                cmd.Invoke(args);
                return true;
            }

            return false;

        }
    }
}
