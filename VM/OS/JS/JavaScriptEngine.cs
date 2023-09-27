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
        public IJsEngine ENGINE_JS;
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

            string jsDirectory = FileSystem.SearchForParentRecursive("VM/VM");

            // ############### START BUG REPORT ################
                // by tool_ateralus : 11:40:35AM tues sept 19
                /*
                    this is just a problem with how we're searching for the OS-JS source js files, we should really embed the entire JS-OS into the DLL so we don't have to rely on any external
                    file structuring. the 'else' case fixes a bug where if you load the library from outside of the source project, it fails to find the source code.
                    You MUST have the source code added as a reference to your other project to use this currently.
                    It should be okay to rely on that, as in the future replacing it with a traditional DLL reference will be presumably seamless. 
                */
                /* 
                    This system still HEAVILY relies on the source code being on the computer but also in a specific location and completely intact, 
                    when used from any other library. This needs to be fixed more than anything in the project.
                */
                LoadModules(jsDirectory + "/OS-JS");

            //  ############### END BUG REPORT ############### 

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
                        if (ENGINE_JS.HasVariable("DEBUG_OUTPUT") || Computer.Debug)
                        {
                            Notifications.Exception(e);
                            Computer.JavaScriptEngine.InteropModule.print(e.Message);
                        }
                    }
                   
                    continue;
                }
                await Task.Delay(1);
            }
            if (!Disposing)
            {
                throw new JsEngineException("Something happened, and the javascript engine exited unexpectedly.");
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
                foreach (var file in Directory.GetFileSystemEntries(directory, "*.js"))
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
        public void ExecuteScript(string absPath)
        {
            if (File.Exists(absPath))
                Task.Run(()=> { try { ENGINE_JS.Execute(File.ReadAllText(absPath)); } catch { } });
        }
        /// <summary>
        /// this method is used for executing js events
        /// </summary>
        /// <param name="code"></param>
        public void DIRECT_EXECUTE(string code)
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
        public async Task CreateEventHandler(string identifier, string targetControl, string methodName, int type)
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
            //         IO.Out($"control {identifier} not found!");
            //         return;
            //     }


            //     ContentControlworkElement element = null;
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
            //         IO.Out($"control {targetControl} of {content.Name} not found.");
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
        public async Task CreateNetworkEventHandler(string identifier, string methodName)
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
