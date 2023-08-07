using System;
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
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;
using Microsoft.VisualBasic.Devices;

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

        public JavaScriptEngine(string ProjectRoot, Computer computer)
        {
            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();

            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            engine = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new JSNetworkHelpers(computer.Network.OutputChannel, computer.Network.InputChannel);
            engine.EmbedHostObject("network", NetworkModule);

            InteropModule = new JSInterop(computer);
            InteropModule.OnModuleImported += ImportModule;
            engine.EmbedHostObject("interop", InteropModule);

            LoadModules(ProjectRoot);

            executionThread = new Thread(Execute);
            executionThread.Start();

        }

        private object? ImportModule(string arg)
        {
            if (modules.TryGetValue(arg, out var val))
                return val;
            return null;

        }

        private void LoadModules(string ProjectRoot)
        {
            bool subscribed = false;

            foreach (var file in Directory.EnumerateFiles(ProjectRoot + "\\OS-JS").Where(f => f.EndsWith(".js")))
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

        public Dictionary<string, Action> MethodBindings = new();
        internal Action? FetchMethodBinding(string name)
        {
            if (MethodBindings.TryGetValue(name, out var val))
            {
                return val;
            }
            return null;
        }
    }
}
