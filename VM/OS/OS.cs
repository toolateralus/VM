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
        public FileSystem FileSystem = new(ROOT);
        public JavaScriptEngine JavaScriptEngine = new();
        public static string ROOT => root;
        static string root = $"{Directory.GetCurrentDirectory()}\\root";
        
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
