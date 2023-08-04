using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

namespace VM.OPSYS.JS
{
    public class JSHelpers
    {
        public void print(string message)
        {
            Debug.WriteLine(message);
        }
    }

    public class JavaScriptEngine
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;

        public JavaScriptEngine()
        {
            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();
            
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;

            engine = engineSwitcher.CreateDefaultEngine();

            var interop = new JSHelpers();

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
