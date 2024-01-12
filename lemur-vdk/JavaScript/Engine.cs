using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;
using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Embedded;
using Lemur.JS.Embedded;
using Lemur.Windowing;
using Newtonsoft.Json;
using OpenTK.Graphics.Egl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using static Lemur.Computer;

namespace Lemur.JS
{
    public class key : embedable
    {
        public key(Computer computer) : base(computer)
        {
        }
        public void clearFocus() {
            Computer.Current.Window?.Dispatcher?.Invoke(() =>
            {
                Keyboard.ClearFocus();
            });

        }
        public bool isDown(string key)
        {
            bool result = false;

            Computer.Current.Window?.Dispatcher?.Invoke(() =>
            {
                if (Enum.TryParse<System.Windows.Input.Key>(key, out var _key))
                    result = Keyboard.IsKeyDown(_key);
                else Notifications.Now($"Failed to parse key {key}");
            });

            return result;
        }
    }
    public class Engine : IDisposable
    {
        internal IJsEngine m_engine_internal;
        IJsEngineSwitcher engineSwitcher;
        CancellationTokenSource cts = new();
        public network NetworkModule { get; }
        public interop InteropModule { get; }
        public conv ConvModule { get; }
        public app_t AppModule { get; }
        public file_t FileModule { get; }
        public term_t TermModule { get; }
        public key KeyModule { get; }
        public string IncludedFiles = "";
        private readonly Thread executionThread;
        public readonly Dictionary<string, object?> Modules = new();
        public readonly List<InteropFunction> EventHandlers = new();
        public readonly Dictionary<string, object> EmbeddedObjects = new();
        private readonly ConcurrentDictionary<int, (string code, Action<object?> output)> CodeDictionary = new();
        public bool Disposing { get; private set; }
        public Engine(Computer computer, string name)
        {
            engineSwitcher = JsEngineSwitcher.Current;
            engineSwitcher.EngineFactories.AddV8();
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;
            m_engine_internal = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new network(computer);
            InteropModule = new interop(computer);
            AppModule = new app_t(computer);
            TermModule = new term_t(computer);
            KeyModule = new key(computer);

            InteropModule.OnModuleImported += ImportModule;

            ConvModule = new conv();
            FileModule = new file_t();

            EmbedObject("deferCached", (object)Defer);
            EmbedObject("Convert", ConvModule);
            EmbedObject("Network", NetworkModule);
            EmbedObject("Interop", InteropModule);
            EmbedObject("App", AppModule);
            EmbedObject("File", FileModule);
            EmbedObject("Terminal", TermModule);
            EmbedObject("Key", KeyModule);
            EmbedType("Stopwatch", typeof(System.Diagnostics.Stopwatch));
            EmbedType("GraphicsContext", typeof(GraphicsContext));
            EmbedObject("config", Computer.Current.Config);

            var joinedPalette = $"const palette = {JsonConvert.SerializeObject(GraphicsContext.Palette)}";
            Execute(joinedPalette);


            EmbedAllObjects();
            executionThread = new Thread(ExecuteAsync);
            executionThread.Start();

            // the basic modules that are auto-included with each context.
            // this differs from 'include' where that's a deferred loading strategy
            // aka lazy loading on demand.
            LoadModules(FileSystem.GetResourcePath("do_not_delete"));

            Task.Run(async () =>
            {


#if DEBUG
await Execute(@$"
    const __NAME__ = '{name}'
    const __DEBUG__ = true;
");
#else
await Execute(@$"
    const __NAME__ = '{name}'
    const __DEBUG__ = false;
");
#endif

            });

            InteropModule.OnModuleExported = (path, obj) =>
            {
                Modules[path] = obj;
            };
        }
        public void Defer(int delay, int index)
        {
            try
            {
                Task.Run(async delegate
                {
                    await Task.Delay(delay, cts.Token).ConfigureAwait(false);
                    m_engine_internal.Evaluate($"__executeDeferredFunc({index});");
                }, cts.Token);
            }
            catch (OperationCanceledException) { }
        }
        public void EmbedObject(string name, object? obj)
        {
            m_engine_internal.EmbedHostObject(name, obj);
        }
        public void EmbedType(string name, Type obj)
        {
            m_engine_internal.EmbedHostType(name, obj);
        }
        public void EmbedAllObjects()
        {
            foreach (var item in EmbeddedObjects)
                m_engine_internal.EmbedHostObject(item.Key, item.Value);
        }
        // Resource intensive loops
        private async void ExecuteAsync()
        {
            while (!Disposing)
            {
                if (!CodeDictionary.IsEmpty)
                {
                    var pair = CodeDictionary.Last();

                    try
                    {
                        var result = m_engine_internal.Evaluate(pair.Value.code);
                        pair.Value.output?.Invoke(result);
                    }
                    catch (Exception e)
                    {
                        if (e is not JsInterruptedException)
                            Notifications.Exception(e);
                    }
                    finally
                    {
                        CodeDictionary.Remove(pair.Key, out _);
                    }

                    continue;
                }
                await Task.Delay(1);
            }
            if (!Disposing)
            {
                throw new JsEngineException("JavaScript execution thread died unexpectedly.");
            }
        }
        public void ImportModule(string arg)
        {
            if (FileSystem.GetResourcePath(arg) is string AbsPath && !string.IsNullOrEmpty(AbsPath))
            {
                if (!IncludedFiles.Contains(AbsPath))
                {
                    IncludedFiles += AbsPath;
                    try
                    {
                        var code = File.ReadAllText(AbsPath);
                        m_engine_internal.Execute(code);
                    }
                    catch (Exception e)
                    {
                        Notifications.Exception(e);
                    }

                }
            }
        }
        public void LoadModules(string sourceDir)
        {
            if (string.IsNullOrEmpty(sourceDir))
            {
                //Notifications.Now("require was called with an empty string and aborted");
                return;
            }

            FileSystem.ProcessDirectoriesAndFilesRecursively(sourceDir, (_, _) => { }, file);

            void file(string d, string f)
            {
                try
                {
                    if (!f.EndsWith(".js"))
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
        public async Task<object?> Execute(string jsCode, CancellationToken token = default)
        {
            object? result = null;

            void callback(object? e) { result = e; };

            int handle = GetUniqueHandle();

            CodeDictionary.TryAdd(handle, (jsCode, callback));

            while (!Disposing && CodeDictionary.TryGetValue(handle, out _) && !token.IsCancellationRequested)
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
            if (string.IsNullOrEmpty(absPath))
                return;

            var script = File.ReadAllText(absPath);
            Task.Run(() => Execute(script));
        }
        public void Dispose()
        {
            Disposing = true;
            try
            {
                m_engine_internal.Interrupt();
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
            try
            {
                cts.Cancel();
                cts.Dispose();
                m_engine_internal.Dispose();
                executionThread.Join();
                AppModule.ReleaseThread();

            } catch (Exception e) 
            {
                Notifications.Exception(e);
                var ans = MessageBox.Show($"The application has encountered a serious problem. You should only continue if you know it's harmless or unimportant. Do you want to quit now? \n\n {e}", "Please exit now.", MessageBoxButton.YesNo);

                if (ans == MessageBoxResult.Yes)
                    Environment.Exit(1);
            }
            GC.Collect();
        }
        internal void CreateNetworkEventHandler(string processID, string methodName)
        {
            ArgumentNullException.ThrowIfNull(processID);
            ArgumentNullException.ThrowIfNull(methodName);

            var nwEvent = new NetworkEvent(this, processID, methodName);
            EventHandlers.Add(nwEvent);
        }
        internal void RemoveNetworkEventHandler(string processID, string methodName)
        {
            ArgumentNullException.ThrowIfNull(processID);
            ArgumentNullException.ThrowIfNull(methodName);

            var nwEvent = EventHandlers.FirstOrDefault(e => e.functionHandle.Contains(processID) && e.functionHandle.Contains(methodName));
            if (nwEvent != null)
                EventHandlers.Remove(nwEvent);
        }
    }
}
