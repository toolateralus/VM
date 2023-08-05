using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
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
using VM.GUI;

namespace VM.OPSYS.JS
{
    public class JSHelpers
    {
        public Action<object?>? OnModuleExported;
        public Action<int>? OnComputerExit;
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void _export(object? obj)
        {
            OnModuleExported?.Invoke(obj);
        }
        public void exit(int code)
        {
            OnComputerExit?.Invoke(code);
        }
    }
    public class JSNetworkHelpers
    {
        // record level nullability
        public event Action<object?[]?>? OnSent;
        public Action<object?[]?>? OnRecieved;

        public JSNetworkHelpers(Action<object?[]?>? Output, Action<object?[]?>? Input)
        {
            OnSent += Output;
            OnRecieved += Input;
        }
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void send(params object?[]? parameters)
        {
            OnSent?.Invoke(parameters);

            int outCh, inCh;
            object msg;

            if (parameters is not null && parameters.Length > 2)
            {
                msg = parameters[2];
                if (parameters[0] is int _out && parameters[1] is int _in)
                {
                    outCh = _out;
                    inCh = _in;
                    Runtime.Broadcast(outCh, inCh, msg);
                    return;
                }
            }
            Notifications.Now("Insufficient arguments for a network connection");


        }
        public object? recieve(params object?[]? parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch &&
                parameters != null && parameters.Length > 0 && parameters[0] is int replyCh)
            {
                return Runtime.PullEvent(ch).value;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            return null;
        }
    }

    public class JavaScriptEngine
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;
        public Dictionary<string, object?> modules = new();

        public JSNetworkHelpers NetworkModule { get; }
        public JSHelpers InteropModule { get; }
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

            InteropModule = new JSHelpers();
            engine.EmbedHostObject("interop", InteropModule);

            LoadModules(ProjectRoot);

            executionThread = new Thread(Execute);
            executionThread.Start();
            
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
                    InteropModule.OnModuleExported += (o) => AddModule(o, file);
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
    }
}
