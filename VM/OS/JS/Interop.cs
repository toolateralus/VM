using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

namespace VM.OPSYS.JS
{
    public class JSHelpers
    {
        public Action<object?>? OnModuleExported;
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void _export(object? obj)
        {
            OnModuleExported?.Invoke(obj);
        }
    }

    public class JavaScriptEngine
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;
        public Dictionary<string, object?> modules = new();

        public JavaScriptEngine(string ProjectRoot)
        {
            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();
            
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            engine = engineSwitcher.CreateDefaultEngine();

            var interop = new JSHelpers();

            var subscribed = false;

            engine.EmbedHostObject("interop" , interop);

            foreach (var file in Directory.EnumerateFiles(ProjectRoot + "\\OS-JS").Where(f => f.EndsWith(".js")))
            {
                void AddModule(object? obj, string path)
                {
                    modules[path] = obj;
                }

                if (!subscribed)
                {
                    interop.OnModuleExported += (o) => AddModule(o, file);
                    subscribed = true;
                }


                try { engine.Execute(File.ReadAllText(file)); }
                catch (Exception e)
                {
                    Notifications.Now(e.Message);
                }
            }

        }

       

        public object Execute(string jsCode, bool compile = false)
        {
            if (compile)
            {
                return engine.Evaluate(jsCode);
            }
            return engine.Evaluate(jsCode);
        }


        

        public void Dispose()
        {
            engine.Dispose();
        }
    }
}
