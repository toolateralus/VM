using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
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
            RegisteredEventHandlers.Add("draw_pixels", DrawPixelsEvent);
        }

        public object? DrawPixelsEvent(string id, object? data)
        {
            var window = Runtime.GetWindow(computer);
            var resizableWins = window.Windows.Where(W => W.Key == id);
            if (resizableWins.Any())
            {
                window.Dispatcher.Invoke(() =>
                {
                    // win == (user window), hierarchy == win->grid->frame->ACTUALUSERCONTENT
                    var win = resizableWins.First().Value.Content;

                });
            }
            return null;
        }

        #region System
        public void print(object message)
        {
            Runtime.GetWindow(computer).Dispatcher.Invoke(() =>
            {
                Debug.WriteLine(message);

                var commandPrompt = Runtime.SearchForOpenWindowType<CommandPrompt>(computer);

                if (commandPrompt == default)
                {
                    Notifications.Now(message?.ToString() ?? "Invalid Print.");
                    return;
                }

                commandPrompt.DrawTextBox($"\n {message}");
            });

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

        public void uninstall(string dir)
        {
            ComputerWindow window = Runtime.GetWindow(computer);

            // js/html app
            if (dir.Contains(".web"))
            {
                window.Uninstall(dir); 
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.Uninstall(dir);
                return;
            }

            Notifications.Now("Incorrect path for uninstall");

        }

        public async void install(string dir)
        {
            ComputerWindow window = Runtime.GetWindow(computer);

            // js/html app
            if (dir.Contains(".web"))
            {
                window.InstallJSHTML(dir);
                return;
            }

            // wpf app
            if (dir.Contains(".app"))
            {
                window.InstallWPF(dir);
            }
        }
        public void alias(string alias, string path)
        {
            computer.OS.CommandLine.Aliases.Add(alias, Runtime.GetResourcePath(path, ".js") ?? "not found");
        }
        public static Dictionary<string, Func<string, object?, object?>> RegisteredEventHandlers = new();
        /// <summary>
        /// this returns the callback, no need for extra listening
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventType"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public object? pushEvent(string id, string eventType, object? data)
        {
            if (RegisteredEventHandlers.TryGetValue(eventType, out var handler))
            {
                return handler.Invoke(id, data);
            }
            return null;
        }
        public void addEventHandler(string identifier, string methodName, int type)
        {
            _ = computer.OS.JavaScriptEngine.CreateEventHandler(identifier, methodName, type);
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
