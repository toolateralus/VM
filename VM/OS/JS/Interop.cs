using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

namespace VM.OPSYS.JS
{
    public static class JSHelpers
    {
        public static void Print(string message)
        {
            Console.WriteLine(message);
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

            RegisterAllJSHelpersFunctions();
        }

        private void RegisterAllJSHelpersFunctions()
        {
            Type jshelpersType = typeof(JSHelpers);
            MethodInfo[] methods = jshelpersType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (MethodInfo method in methods)
            {
                string methodName = method.Name;
                Type returnType = method.ReturnType;

                if (returnType == typeof(void))
                {
                    Action<object[]> action = args => method.Invoke(null, args);
                    engine.Execute($"function {methodName}() {{ dotNetInterop.{methodName}.apply(dotNetInterop, arguments); }}");
                }
                else
                {
                    Delegate function = method.CreateDelegate(typeof(Func<,>).MakeGenericType(method.GetParameters().Select(p => p.ParameterType).Concat(new[] { returnType }).ToArray()), null);
                    engine.Execute($"function {methodName}() {{ return dotNetInterop.{methodName}.apply(dotNetInterop, arguments); }}");
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


        public void RegisterFunction(string functionName, Delegate function)
        {
            // Optionally, you can implement this method to register .NET functions as JavaScript functions
            engine.Execute($"dotNetInterop.{functionName} = dotNetInterop.createDelegate({functionName});"); // Again, change "dotNetInterop" to the name you want to use in JavaScript
        }

        public void Dispose()
        {
            engine.Dispose();
        }
    }
}
