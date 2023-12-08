using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;
using Lemur.FS;
using Lemur.GUI;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Embedded;
using Lemur.JS.Embedded;
using Lemur.Windowing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Lemur.Computer;

namespace Lemur.JS
{
    public class key
    {
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

        private readonly Thread executionThread;
        public readonly Dictionary<string, object?> Modules = new();

        public readonly List<InteropFunction> EventHandlers = new();
        public readonly Dictionary<string, object> EmbeddedObjects = new();
        private readonly ConcurrentDictionary<int, (string code, Action<object?> output)> CodeDictionary = new();
        public bool Disposing { get; private set; }

        public network NetworkModule { get; }
        public interop InteropModule { get; }
        public graphics GraphicsModule { get; }
        public conv ConvModule { get; }
        public app AppModule { get; }
        public file_t FileModule { get; }
        public term TermModule { get; }
        public key KeyModule { get; }

        public Engine()
        {

            engineSwitcher = JsEngineSwitcher.Current;
            engineSwitcher.EngineFactories.AddV8();
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;
            m_engine_internal = engineSwitcher.CreateDefaultEngine();

            NetworkModule = new network();

            InteropModule = new interop();
            InteropModule.OnModuleImported += ImportModule;

            GraphicsModule = new graphics();
            ConvModule = new conv();
            AppModule = new app();
            FileModule = new file_t();
            TermModule = new term();
            KeyModule = new key();

            EmbedObject("conv", ConvModule);
            EmbedObject("network", NetworkModule);
            EmbedObject("interop", InteropModule);
            EmbedObject("gfx", GraphicsModule);
            EmbedObject("app", AppModule);
            EmbedObject("file", FileModule);
            EmbedObject("term", TermModule);
            EmbedObject("Key", KeyModule);
            EmbedType("Stopwatch", typeof(System.Diagnostics.Stopwatch));
            EmbedObject("config", Computer.Current.Config);

            EmbedAllObjects();
            executionThread = new Thread(ExecuteAsync);
            executionThread.Start();

            // the basic modules that are auto-included with each context.
            LoadModules(FileSystem.GetResourcePath("do_not_delete"));

            InteropModule.OnModuleExported = (path, obj) =>
            {
                Modules[path] = obj;
            };


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
        public string IncludedFiles = "";
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
                // Notifications.Now("require was called with an empty string and aborted");
                return;
            }

            FileSystem.ProcessDirectoriesAndFilesRecursively(sourceDir, (_, _) => { }, file);

            void file(string d, string f)
            {
                try
                {
                    m_engine_internal.Execute(File.ReadAllText(f));
                }
                catch (Exception e)
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
#pragma warning disable CA5394
        // we don't need a cryptographically secure random number generator here
        private int GetUniqueHandle()
        {
            int handle = Random.Shared.Next();

            while (CodeDictionary.TryGetValue(handle, out _))
                handle = Random.Shared.Next();

            return handle;
        }
#pragma warning restore CA5394
        internal void ExecuteScript(string absPath)
        {
            if (string.IsNullOrEmpty(absPath))
                return;

            var script = File.ReadAllText(absPath);
            Task.Run(() => Execute(script));
        }
        internal async Task CreateEventHandler(string identifier, string targetControl, string methodName, int type)
        {
            var wnd = Computer.Current.Window;

            // check if this event already exists
            var result = await Execute($"{identifier} != null").ConfigureAwait(true);

            if (result is not bool ID_EXISTS || !ID_EXISTS)
            {
                Notifications.Now($"App not found : {identifier}..  that is NOT good...");
                return;
            }

            // check if this method already exists
            result = await Execute($"{identifier}.{methodName} != null").ConfigureAwait(true);

            string processClass = Computer.GetProcessClass(identifier).Replace(".app", "", StringComparison.CurrentCulture);

            if (result is not bool METHOD_EXISTS || !METHOD_EXISTS)
            {
                Notifications.Now($"'app.eventHandler(...)' threw an exception : {processClass}.{methodName} not found. Make sure {methodName} is defined and spelled correctly in both the hook function call and the definition.");
                return;
            }

            InteropEvent? eh = default;

            wnd.Dispatcher.Invoke(() =>
            {
                var content = Computer.GetProcess(identifier)?.UI?.JavaScriptEngine?.AppModule?.GetUserContent();

                if (content == null)
                {
                    Notifications.Now($"control {identifier} not found!");
                    return;
                }

                FrameworkElement? element = null;

                if (targetControl.ToLower(CultureInfo.CurrentCulture).Trim() == "this")
                    element = content;
                else
                    element = Embedded.app.FindControl(content, targetControl)!;


                if (element == null)
                {
                    Notifications.Now($"control {targetControl} of {content.Name} not found.");
                    return;
                }

                eh = new InteropEvent(element, (XAML_EVENTS)type, this, identifier, methodName);

            });

            if (GetProcess(identifier) is not Process p)
            {
                Notifications.Now("Creating an event handler failed : this is an engine bug. report it on GitHub if you'd like");
                return;
            }

            var disposed = false;

            /// this was an attempt to force close the app on too many errors
            /// stack overflow, trying to finally decouple UI.
            /// 
            //eh.OnEventDisposed += () => {
            //    if (disposed)
            //        return; 

            //    App.Current.Dispatcher.Invoke(app.Close);
            //    disposed = true;
            //};

            p.OnProcessTermination += () =>
            {
                if (disposed)
                    return;

                if (EventHandlers.Contains(eh))
                    EventHandlers.Remove(eh);

                eh.ForceDispose();
                disposed = true;
            };

            EventHandlers.Add(eh);
        }

        public void Dispose()
        {
            Disposing = true;
            m_engine_internal.Dispose();
            executionThread.Join();
        }
    }
}
