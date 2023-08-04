using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using VM.OPSYS.JS;

namespace VM.OPSYS
{
    public class OS
    {
        public FileSystem FileSystem = new(FS_ROOT);
        public JavaScriptEngine JavaScriptEngine = new();

        private static string EXE_DIR = Directory.GetCurrentDirectory();
        public static string PROJECT_ROOT = Path.GetFullPath(Path.Combine(EXE_DIR, @"..\..\.."));
        public static string FS_ROOT => root;

        static string root = $"{PROJECT_ROOT}\\root";
        private static OS current = null!;
        public static OS Current => current;

        public FontFamily SystemFont { get; internal set; } = new FontFamily("Consolas");

        public OS()
        {

            if (current != null)
            {
                throw new InvalidOperationException("Cannot instantiate several instances of the operating system");
            }
            else current = this;
        }
    }
}
