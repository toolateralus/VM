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
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
    }

    public class JavaScriptEngine
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;

        public JavaScriptEngine(string ProjectRoot)
        {
            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();
            
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            engine = engineSwitcher.CreateDefaultEngine();

            var interop = new JSHelpers();

            foreach (var file in Directory.EnumerateFiles(ProjectRoot + "\\OS-JS").Where(f => f.EndsWith(".js")))
            {
                try { engine.ExecuteFile(file); }
                catch(Exception e)
                {
                    Notifications.Now($"{file} failed to execute javascript.. {e.Message}");
                }
            }

            engine.EmbedHostObject("interop" , interop);
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
