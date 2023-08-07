﻿using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        public async void register_app(string dir)
        {
            var data = Runtime.GetAppDefinition(computer, dir);

            var control = XamlJsInterop.ParseUserControl(data.XAML);

            var js_obj = await computer.OS.JavaScriptEngine.Execute(data.JS);

            if (js_obj == null)
            {
                Notifications.Now($"Javascript for a {dir} backend did not compile or return any valid data");
            }

            App.Current.Dispatcher.Invoke(delegate {
                ComputerWindow window = Runtime.GetWindow(computer);
                window.RegisterCustomApp(dir, control);
            });

            // bind ui events to js methods here.
            // XamlJsInterop.InitializeControl(computer, control, new() { XamlJsInterop.EventInitializer }, new() { });
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
