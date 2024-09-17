using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

using Lemur.FS;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Embedded;
using Lemur.JS.Embedded;
using Lemur.Windowing;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Lemur.JS {
    public class Key_t : embedable {
        public Key_t(Computer computer) : base(computer) {
        }
        [ApiDoc("Call Keyboard.ClearFocus(). this will deselect any textboxes etc.")]
        public void clearFocus() {
            Computer.Current.Window?.Dispatcher?.Invoke(() => {
                Keyboard.ClearFocus();
            });

        }
        [ApiDoc("return whether a key such as 'R', 'LeftCtrl' or whatever is down.")]
        public bool isDown(string key) {
            var task = Computer.Current.Window?.Dispatcher?.InvokeAsync(() => {
                if (Enum.TryParse<System.Windows.Input.Key>(key, out var _key))
                    return Keyboard.IsKeyDown(_key);
                Notifications.Now($"Failed to parse key {key}");
                return false;
            });
            task.Wait();
            return task.Result;
        }
    }
    public class Engine : IDisposable {

        internal IJsEngine m_engine_internal;
        private IJsEngineSwitcher engineSwitcher;

        private readonly CancellationTokenSource cts = new();
        public Network_t NetworkModule { get; }
        public Interop_t InteropModule { get; }
        public Convert_t ConvModule { get; }
        public Embedded.App_t AppModule { get; }
        public File_t FileModule { get; }
        public Terminal_t TermModule { get; }
        public Key_t KeyModule { get; }
        string includedFiles = "";

        private readonly Thread executionThread;

        public readonly Dictionary<string, object?> Modules = [];
        public readonly List<InteropFunction> EventHandlers = [];


        private readonly ConcurrentDictionary<int, (string code, Action<object?> output)> CodeDictionary = [];
        public bool Disposing { get; private set; }
        public Engine(Computer computer, string name) {
            engineSwitcher = JsEngineSwitcher.Current;
            engineSwitcher.EngineFactories.AddV8();
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;
            m_engine_internal = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new Network_t(computer);
            InteropModule = new Interop_t(computer);
            AppModule = new Embedded.App_t(computer);
            TermModule = new Terminal_t(computer);
            KeyModule = new Key_t(computer);

            InteropModule.OnModuleImported += ImportModule;

            ConvModule = new Convert_t();
            FileModule = new File_t();

            EmbedObject("deferCached", (object)Defer);
            EmbedObject("loadConfig", (object)Computer.LoadConfig);
            EmbedObject("saveConfig", (object)Computer.SaveConfig);
            EmbedObject("config", Computer.Current.Config);


            EmbedObject("Convert", ConvModule);
            EmbedObject("Network", NetworkModule);
            EmbedObject("Interop", InteropModule);
            EmbedObject("App", AppModule);
            EmbedObject("File", FileModule);
            EmbedObject("Terminal", TermModule);
            EmbedObject("Key", KeyModule);
            EmbedType("Stopwatch", typeof(System.Diagnostics.Stopwatch));
            EmbedType("GraphicsContext", typeof(GraphicsContext_t));

            EmbedType("Vector2", typeof(Vector2));
            EmbedType("Vector3", typeof(Vector3));
            EmbedType("Vector4", typeof(Vector4));
            EmbedType("Color", typeof(Color4));
            EmbedType("Shader", typeof(Shader));
            EmbedType("Mesh", typeof(Mesh));
            EmbedType("Camera", typeof(Camera));
            EmbedType("GLSurface", typeof(Renderer));

            var jsonPalette = $"const palette = {JsonConvert.SerializeObject(GraphicsContext_t.Palette)}";
            _ = Execute(jsonPalette);

            executionThread = new Thread(ExecuteAsync);
            executionThread.Start();

            // the basic modules that are auto-included with each context.
            // this differs from 'include' where that's a deferred loading strategy
            // aka lazy loading on demand.
            LoadModules(FileSystem.GetResourcePath("do_not_delete"));

            // TODO: add a better way to embed environment variable and interpret time vars.
            Task.Run(async () => {
                _ = await Execute(@$"
                    const __FILE__= '{name}'
                ").ConfigureAwait(false);
            });

            InteropModule.OnModuleExported = (path, obj) => {
                Modules[path] = obj;
            };
        }
        public void Defer(int delay, int index) {
            try {
                Task.Run(async delegate {
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                    m_engine_internal.Evaluate($"__executeDeferredFunc({index});");
                }, cts.Token);
            }
            catch (OperationCanceledException) { }
        }
        public void EmbedObject(string name, object? obj) {
            m_engine_internal.EmbedHostObject(name, obj);
        }
        public void EmbedType(string name, Type obj) {
            m_engine_internal.EmbedHostType(name, obj);
        }

        private async void ExecuteAsync() {
            while (!Disposing) {
                if (!CodeDictionary.IsEmpty) {
                    var pair = CodeDictionary.Last();

                    try {
                        var result = m_engine_internal.Evaluate(pair.Value.code);
                        pair.Value.output?.Invoke(result);
                    }
                    catch (Exception e) {
                        if (e is not JsInterruptedException)
                            Notifications.Exception(e);
                    }
                    finally {
                        CodeDictionary.Remove(pair.Key, out _);
                    }

                    continue;
                }
                await Task.Delay(1).ConfigureAwait(false);
            }
            if (!Disposing) {
                throw new JsEngineException("JavaScript execution thread died unexpectedly.");
            }
        }
        public void ImportModule(string arg) {
            if (FileSystem.GetResourcePath(arg) is string AbsPath && !string.IsNullOrEmpty(AbsPath)) {
                if (!includedFiles.Contains(AbsPath)) {
                    includedFiles += AbsPath;
                    try {
                        var code = File.ReadAllText(AbsPath);
                        m_engine_internal.Execute(code);
                    }
                    catch (Exception e) {
                        Notifications.Exception(e);
                    }

                }
            }
        }
        public void LoadModules(string sourceDir) {
            if (string.IsNullOrEmpty(sourceDir)) {
                Notifications.Now("require was called with an empty string and aborted");
                return;
            }
            FileSystem.ProcessDirectoriesAndFilesRecursively(sourceDir, (_, _) => { }, process_file);


            void process_file(string d, string f) {
                try {
                    if (!f.EndsWith(".js", StringComparison.CurrentCulture))
                        return;

                    var code = File.ReadAllText(f);

                    if (!string.IsNullOrEmpty(code))
                        m_engine_internal.Execute(code);
                }
                catch (Exception e)  // todo: remove al the catchall exceptions in our solution.
                {
                    Notifications.Exception(e);
                }
            }
        }
        public async Task<object?> Execute(string jsCode, CancellationToken token = default) {
            object? result = null;

            void callback(object? e) { result = e; };

            int handle = GetUniqueHandle();

            // enqueue our code to be executed
            CodeDictionary.TryAdd(handle, (jsCode, callback));

            // wait for the result.
            while (!Disposing && CodeDictionary.TryGetValue(handle, out _) && !token.IsCancellationRequested)
                await Task.Delay(1, token).ConfigureAwait(false);

            // if we cancelled, force removal of the code from the dictionary.
            // Note that this doesn't actually stop the code from executing: it just 
            // stops us from waiting for its result.
            if (token.IsCancellationRequested) {
                // cancel execution
                CodeDictionary.TryRemove(handle, out _);
                return null;
            }

            return result;
        }
        private int GetUniqueHandle() {
            int handle = Random.Shared.Next();

            while (CodeDictionary.TryGetValue(handle, out _))
                handle = Random.Shared.Next();

            return handle;
        }
        internal void ExecuteScript(string absPath) {
            if (string.IsNullOrEmpty(absPath))
                return;

            var script = File.ReadAllText(absPath);

            ThreadPool.QueueUserWorkItem(async _ => {
                var path_from_root = absPath.Replace(FileSystem.Root, string.Empty);
                _ = Execute(script).ConfigureAwait(false);
            });
        }
        public void Dispose() {
            Disposing = true;

            try {
                m_engine_internal.Interrupt();
            }
            catch (Exception e) {
                Notifications.Exception(e);
            }

            try {
                cts.Cancel();
                cts.Dispose();
                m_engine_internal.Dispose();
                executionThread.Join();
                AppModule.ReleaseThread();

            }
            catch (Exception e) {
                Notifications.Exception(e);
                var ans = MessageBox.Show($"The application has encountered a serious problem. You should only continue if you know it's harmless or unimportant. Do you want to quit now? \n\n {e}", "Please exit now.", MessageBoxButton.YesNo);

                if (ans == MessageBoxResult.Yes)
                    Environment.Exit(1);
            }
            GC.Collect();
        }
        internal void CreateNetworkEventHandler(string processID, string methodName) {
            ArgumentNullException.ThrowIfNull(processID);
            ArgumentNullException.ThrowIfNull(methodName);

            var nwEvent = new NetworkEvent(this, processID, methodName);
            EventHandlers.Add(nwEvent);
        }
        internal void RemoveNetworkEventHandler(string processID, string methodName) {
            ArgumentNullException.ThrowIfNull(processID);
            ArgumentNullException.ThrowIfNull(methodName);

            var nwEvent = EventHandlers.FirstOrDefault(e => e.functionHandle.Contains(processID) && e.functionHandle.Contains(methodName));
            if (nwEvent != null)
                EventHandlers.Remove(nwEvent);
        }
    }
}
