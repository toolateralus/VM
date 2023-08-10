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
using System.Windows.Forms;
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

            NetworkModule = new JSNetworkHelpers(computer.Network.OutputChannel, computer.Network.InputChannel);
            engine.EmbedHostObject("network", NetworkModule);

            InteropModule = new JSInterop(computer);
            InteropModule.OnModuleImported += ImportModule;
            engine.EmbedHostObject("interop", InteropModule);

            executionThread = new Thread(Execute);
            executionThread.Start();
            CompositionTarget.Rendering += Render;

        }

        private void Render(object? sender, EventArgs e)
        {
            foreach (var item in EventHandlers.Where(e => e.Event == XAML_EVENTS.RENDER))
            {
                item.Invoke(null,null);
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
            bool subscribed = false;

            foreach (var file in Directory.EnumerateFiles(sourceDir).Where(f => f.EndsWith(".js")))
            {
                void AddModule(object? obj, string path)
                {
                    modules[path] = obj;
                }

                if (!subscribed)
                {
                    InteropModule.OnModuleExported += (path, o) => AddModule(o, path);
                    subscribed = true;
                }

                try
                {
                    engine.Execute(File.ReadAllText(file));
                }
                catch (Exception e)
                {
                    Notifications.Now(e.Message);
                }
            }
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
            Task.Run(() => executionThread.Join());
        }
        internal void ExecuteScript(string absPath)
        {
            if (File.Exists(absPath))
                Task.Run(()=>engine.Execute(File.ReadAllText(absPath)));
        }
        internal void DIRECT_EXECUTE(string code)
        {
            Task.Run(() => engine.Execute(code));
        }
        public List<JSEventHandler> EventHandlers = new();

        // async void for specific use
        internal async Task CreateEventHandler(string identifier, string methodName, int type)
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
                    var eh = new JSEventHandler((XAML_EVENTS)type, computer.OS.JavaScriptEngine, identifier, methodName, "OS", "null");
                    EventHandlers.Add(eh);
                    break;
            }
        }
    }
}
