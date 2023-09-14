using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;
using VM.FS;

namespace VM.JS
{
    public class JavaScriptEngine : IDisposable
    {
        internal IJsEngine ENGINE_JS;
        IJsEngineSwitcher engineSwitcher;
        public readonly Dictionary<string, object?> Modules_UNUSED = new();
        public readonly JSNetworkHelpers NetworkModule;
        public readonly JSInterop InteropModule;
        private readonly ConcurrentDictionary<int, (string code, Action<object?> output)> CodeDictionary = new();
        public readonly Dictionary<string, object> EmbeddedObjects = new();
        public readonly List<JSEventHandler> EventHandlers = new();

        private Computer Computer { get; set; }
        private readonly Thread executionThread;
        public bool Disposing { get; private set; }

        public JavaScriptEngine(Computer computer)
        {
            Computer = computer;

            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();

            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            ENGINE_JS = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new JSNetworkHelpers(computer, computer.Network.OnSendMessage);

            InteropModule = new JSInterop(computer);
            InteropModule.OnModuleImported += ImportModule;

            EmbeddedObjects["network"] = NetworkModule;
            EmbeddedObjects["interop"] = InteropModule;

            EmbedAllObjects();

            executionThread = new Thread(ExecuteAsync);
            executionThread.Start();

            string jsDirectory = Computer.SearchForParentRecursive("VM");

            LoadModules(jsDirectory + "/OS-JS");

            _ = Execute($"os.id = {computer.ID}");

            InteropModule.OnComputerExit += computer.Exit;
        }
        public void EmbedObject(string name, object? obj)
        {
            ENGINE_JS.EmbedHostObject(name, obj);
        }
        public void EmbedType(string name, Type obj)
        {
            ENGINE_JS.EmbedHostType(name, obj);
        }
        public void EmbedAllObjects()
        {
            foreach (var item in EmbeddedObjects)
                ENGINE_JS.EmbedHostObject(item.Key, item.Value);

        }
        // Resource intensive loops
        private async void ExecuteAsync()
        {
            while (!Disposing)
            {
                if (!CodeDictionary.IsEmpty)
                {
                    var pair = CodeDictionary.Last();
                    CodeDictionary.Remove(pair.Key, out _);

                    try
                    {
                        var result = ENGINE_JS.Evaluate(pair.Value.code);
                        pair.Value.output?.Invoke(result);
                    }
                    catch (Exception e)
                    {
                        Notifications.Exception(e);
                        Computer.JavaScriptEngine.InteropModule.print(e.Message);
                    }
                   
                    continue;
                }
                await Task.Delay(1);
            }
            if (!Disposing)
            {
                throw new JsEngineException("Something happened");
            }
        }
        public object? GetVariable(string name)
        {
            return ENGINE_JS.GetVariableValue(name);
        }
        private object? ImportModule(string arg)
        {
            if (FileSystem.GetResourcePath(arg) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
            {
                ENGINE_JS.ExecuteFile(AbsPath);
            }
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
                        Modules_UNUSED[path] = obj;
                    }

                    InteropModule.OnModuleExported += (path, o) => AddModule(o, path);

                    try
                    {
                        ENGINE_JS.Execute(File.ReadAllText(file));
                    }
                    catch (Exception e)
                    {
                        Notifications.Exception(e);
                    }
                }

                foreach (var subDir in Directory.GetDirectories(directory))
                {
                    RecursiveLoad(subDir);
                }
            }

            RecursiveLoad(sourceDir);
        }
        public async Task<object?> Execute(string jsCode, CancellationToken token = default)
        {
            object? result = null;

            void callback(object? e) { result = e; };

            int handle = GetUniqueHandle();

            CodeDictionary.TryAdd(handle, (jsCode, callback));

            while (CodeDictionary.TryGetValue(handle, out _) && !token.IsCancellationRequested)
                await Task.Delay(1, token);

            if (token.IsCancellationRequested)
            {
                // cancel execution
                CodeDictionary.TryRemove(handle, out _);
                return null;
            }

            return result;
        }
        private int GetUniqueHandle()
        {
            int handle = Random.Shared.Next();

            while (CodeDictionary.TryGetValue(handle, out _))
                handle = Random.Shared.Next();

            return handle;
        }
       
        internal void ExecuteScript(string absPath)
        {
            if (File.Exists(absPath))
                Task.Run(()=> { try { ENGINE_JS.Execute(File.ReadAllText(absPath)); } catch { } });
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
                ENGINE_JS.Execute(code);
            }
            catch(Exception e)
            {
                Notifications.Exception(e);
            }
        }
        internal async Task CreateEventHandler(string identifier, string targetControl, string methodName, int type)
        {
            
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
            // wnd.Dispatcher.Invoke(() =>
            // {
            //     var content = JSInterop.GetUserContent(identifier, Computer);

            //     if (content == null)
            //     {
            //         Notifications.Now($"control {identifier} not found!");
            //         return;
            //     }


            //     FrameworkElement element = null;
            //     if (targetControl.ToLower().Trim() == "this")
            //     {
            //         element = content;
            //     }
            //     else
            //     {
            //         element = JSInterop.FindControl(content, targetControl);
            //     }


            //     if (element == null)
            //     {
            //         Notifications.Now($"control {targetControl} of {content.Name} not found.");
            //         return;
            //     }

            //     var eh = new XAMLJSEventHandler(element, (XAML_EVENTS)type, this, identifier, methodName);

            //     if (Computer.USER_WINDOW_INSTANCES.TryGetValue(identifier, out var app))
            //     {
            //         app.OnClosed += () =>
            //         {
            //             if (EventHandlers.Contains(eh))
            //                 EventHandlers.Remove(eh);

            //             eh.OnDispose?.Invoke();
            //         };
            //     }

            //     EventHandlers.Add(eh);
            // });
        }
        internal async Task CreateNetworkEventHandler(string identifier, string methodName)
        {

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

            var eh = new NetworkEventHandler(this, identifier, methodName);

            if (Computer.USER_WINDOW_INSTANCES.TryGetValue(identifier, out var app))
            {
               
            }

            EventHandlers.Add(eh);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                {
                    ENGINE_JS?.Dispose();

                    Task.Run(() => executionThread.Join());
                    Task.Run(() =>
                    {
                        for (int i = 0; i < EventHandlers.Count; i++)
                        {
                            JSEventHandler? eventHandler = EventHandlers[i];
                            eventHandler?.Dispose();
                        }
                    });
                }

                ENGINE_JS = null!;
                Computer = null!;
                Disposing = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
