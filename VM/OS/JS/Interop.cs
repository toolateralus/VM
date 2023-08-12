using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;
using Microsoft.VisualBasic.Devices;
using VM.GUI;
using static VM.OS.JS.JSInterop;

namespace VM.OS.JS
{
    public class JavaScriptEngine
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;
        public Dictionary<string, object?> modules = new();
        public JSNetworkHelpers NetworkModule { get; }
        public JSInterop InteropModule { get; }
        public bool Disposing { get; private set; }
        private readonly ConcurrentDictionary<int, (string code, Action<object?> output)> CodeDictionary = new();
        private readonly Thread executionThread;

        Computer computer;
        public JavaScriptEngine(string ProjectRoot, Computer computer)
        {
            this.computer = computer;

            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();

            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            engine = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new JSNetworkHelpers(computer, computer.Network.OnSendMessage, computer.Network.OnMessageRecieved);
            engine.EmbedHostObject("network", NetworkModule);

            InteropModule = new JSInterop(computer);
            InteropModule.OnModuleImported += ImportModule;
            engine.EmbedHostObject("interop", InteropModule);

            executionThread = new Thread(Execute);
            executionThread.Start();

            renderThread = new Thread(Render);
            renderThread.Start();

        }
        Thread renderThread;
        private void Render()
        {
            while (true)
            {
                if (Disposing)
                    return;

                var collection = EventHandlers.Where(e => e.Event == XAML_EVENTS.RENDER);
                for (int i = 0; i < collection.Count(); ++i)
                {
                    var item = collection.ElementAt(i);
                    if (!item.Disposed)
                    {
                        item?.InvokeGeneric(null, null);
                    }
                    else
                    {
                        EventHandlers.Remove(item);
                    }
                }
                Thread.Sleep(16);
            }
        }

        public object? GetVariable(string name)
        {
            return engine.GetVariableValue(name);
        }
        private object? ImportModule(string arg)
        {
            if (modules.TryGetValue(arg, out var val))
                return val;
            return null;

        }
        public void LoadModules(string sourceDir)
        {
            void RecursiveLoad(string directory)
            {
                foreach (var file in Directory.GetFiles(directory, "*.js"))
                {
                    void AddModule(object? obj, string path)
                    {
                        modules[path] = obj;
                    }

                    InteropModule.OnModuleExported += (path, o) => AddModule(o, path);

                    try
                    {
                        engine.Execute(File.ReadAllText(file));
                    }
                    catch (Exception e)
                    {
                        Notifications.Now(e.Message);
                    }
                }

                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    RecursiveLoad(subDir);
                }
            }

            RecursiveLoad(sourceDir);
        }
        public void Execute()
        {
            while (true && !Disposing)
            {
                if (!CodeDictionary.IsEmpty)
                {
                    var pair = CodeDictionary.Last();
                    CodeDictionary.Remove(pair.Key, out _);

                    try
                    {
                        var result = engine.Evaluate(pair.Value.code);
                        pair.Value.output?.Invoke(result);
                    }
                    catch (Exception e)
                    {
                        Notifications.Now(e.Message);
                    }
                }
                Thread.SpinWait(1);
            }
            if (!Disposing)
            {
                throw new JsEngineException("Something happened");
            }
        }
        public async Task<object?> Execute(string jsCode)
        {
            object? result = null;

            void callback(object? e) { result = e; };

            int handle = GetUniqueHandle();

            CodeDictionary.TryAdd(handle, (jsCode, callback));

            while (CodeDictionary.TryGetValue(handle, out _))
                await Task.Delay(1);

            return result;
        }
        private int GetUniqueHandle()
        {
            int handle = Random.Shared.Next();

            while (CodeDictionary.TryGetValue(handle, out _))
                handle = Random.Shared.Next();

            return handle;
        }
        public void Dispose()
        {
            Disposing = true;
            engine.Dispose();
            engine = null;


            Task.Run(() => executionThread.Join());
            Task.Run(() => renderThread.Join());
        }
        internal void ExecuteScript(string absPath)
        {
            if (File.Exists(absPath))
                Task.Run(()=> { try { engine.Execute(File.ReadAllText(absPath)); } catch { } });
        }
        /// <summary>
        /// this method is used for executing js events
        /// </summary>
        /// <param name="code"></param>
        internal void DIRECT_EXECUTE(string code)
        {
            if (Disposing)
                return;
            try
            {
                engine.Execute(code);
            }
            catch(Exception e)
            {
                Notifications.Now(e.Message);
            }
        }
        public List<JSEventHandler> EventHandlers = new();

        // async void for specific use
        internal async Task CreateEventHandler(string identifier, string targetControl, string methodName, int type)
        {
            var wnd = Runtime.GetWindow(computer);

            var result = await Execute($"{identifier} != null");

            if (result is not bool ID_EXISTS || !ID_EXISTS)
            {
                return;
            }

            result = await Execute($"{identifier}.{methodName} != null");

            if (result is not bool METHOD_EXISTS || !METHOD_EXISTS)
            {
                return;
            }
            wnd.Dispatcher.Invoke(() =>
            {
                var content = InteropModule.GetUserContent(identifier);

                if (content == null)
                {
                    Notifications.Now($"control {identifier} not found!");
                    return;
                }


                FrameworkElement element = null;
                if (targetControl.ToLower().Trim() == "this")
                {
                    element = content;
                }
                else
                {
                    element = InteropModule.FindControl(content, targetControl);
                }


                if (element == null)
                {
                    Notifications.Now($"control {targetControl} of {content.Name} not found.");
                    return;
                }

                var eh = new JSEventHandler(element, (XAML_EVENTS)type, computer.OS.JavaScriptEngine, identifier, methodName);
                    
                if (wnd.Windows.TryGetValue(identifier, out var app))
                {
                    app.OnClosed += () =>
                    {
                        if (EventHandlers.Contains(eh))
                            EventHandlers.Remove(eh);

                        eh.OnUnhook?.Invoke();
                    };
                }

                EventHandlers.Add(eh);
            });
        }
    }
}
