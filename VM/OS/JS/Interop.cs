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
        public void Print(string message)
        {
            Debug.WriteLine(message);
        }
    }

    public class JSInterop
    {
        IJsEngine engine;
        IJsEngineSwitcher engineSwitcher;

        public JSInterop()
        {
            engineSwitcher = JsEngineSwitcher.Current;

            engineSwitcher.EngineFactories.AddV8();
            
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;
            engine = engineSwitcher.CreateDefaultEngine();
            
            engine.EmbedHostObject("interop" ,new JSHelpers());

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
