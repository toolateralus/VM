using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using VM.GUI;
using VM.OPSYS.JS;

namespace VM.OPSYS
{

    public class Computer
    {
        public Computer(uint id)
        {
            OS = new(id, this);
        }
        public uint ID() => OS.ID;

        public OS OS;

        internal void Exit(int exitCode)
        {
            MainWindow.GetPCWindow(this).Close();
            if (MainWindow.Computers.Count > 0 && exitCode != 0)
            {
                Notifications.Now($"Computer {ID()} has exited, most likely due to an error. code:{exitCode}");
            }
        }
    }

    public class OS
    {
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;

        public readonly uint ID;
        
        public readonly string FS_ROOT;
        public readonly string PROJECT_ROOT;

        public FontFamily SystemFont { get; internal set; } = new FontFamily("Consolas");

        public OS(uint id, Computer pc)
        {
            var EXE_DIR = Directory.GetCurrentDirectory();
            PROJECT_ROOT = Path.GetFullPath(Path.Combine(EXE_DIR, @"..\..\.."));
            FS_ROOT = $"{PROJECT_ROOT}\\computer{id}";
            FS = new(FS_ROOT, pc);
            JavaScriptEngine = new(PROJECT_ROOT);
            JavaScriptEngine.Execute($"OS.id = {id}");
            JavaScriptEngine.InteropModule.OnComputerExit += pc.Exit;
        }
    }
}
