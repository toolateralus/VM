using Microsoft.ClearScript;
using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using VM.GUI;
using VM.OS.UserInterface;


namespace VM.OS.JS
{
    public class JSInterop
    {

        public Computer computer;
        public Action<string, object?>? OnModuleExported;
        public Func<string, object?>? OnModuleImported;
        public Action<int>? OnComputerExit;

        public JSInterop(Computer computer)
        {
            this.computer = computer;
        }

        #region System
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void export(string id, object? obj)
        {
            OnModuleExported?.Invoke(id, obj);
        }
        public void exit(int code)
        {
            OnComputerExit?.Invoke(code);
        }
        #endregion
        #region XAML/JS interop
        public async void install(string dir)
        {
            ComputerWindow window = Runtime.GetWindow(computer);

            // js/html app
            if (dir.Contains(".web"))
            {
                window.RegisterCustomWebApp(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.RegisterCustomApp(dir);
            }
        }
        public void alias(string alias, string path)
        {
            computer.OS.CommandLine.Aliases.Add(alias, Runtime.GetResourcePath(path, ".js") ?? "not found");
        }
        public void addEventHandler(object? method, int type)
        {
            var wnd = Runtime.GetWindow(computer);

            if (method is IScriptObject v8Function)
            {
                void execute(params object[]? parameters)
                {
                    v8Function.Invoke(false, parameters ?? new object[] { });
                }

                switch ((XAML_EVENTS)type)
                {
                    case XAML_EVENTS.MOUSE_DOWN:
                        break;
                    case XAML_EVENTS.MOUSE_UP:
                        break;
                    case XAML_EVENTS.MOUSE_MOVE:
                        break;
                    case XAML_EVENTS.KEY_DOWN:
                        break;
                    case XAML_EVENTS.KEY_UP:
                        break;
                    case XAML_EVENTS.LOADED:
                        break;
                    case XAML_EVENTS.WINDOW_CLOSE:
                        break;
                    case XAML_EVENTS.RENDER:
                        CompositionTarget.Rendering += (sender, e) => execute(null);
                        break;
                }
            }

          


        }
        #endregion
        #region IO
        public object? require(string path)
        {
            return OnModuleImported?.Invoke(path);
        }
        public object? read_file(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            return File.ReadAllText(path);
        }
        public void write_file(string path, string data)
        {
           File.WriteAllText(path, data);
        }
        public bool file_exists(string path)
        {
            return File.Exists(path);
        }
        #endregion
    }
}
