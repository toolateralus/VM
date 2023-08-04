using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VroomJs;

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
        JsEngine engine = new();
        JsContext context = null!;
        public JSInterop()
        {
            context = engine.CreateContext();
            RegisterAllJSHelpersFunctions();

        }
        private void RegisterAllJSHelpersFunctions()
        {
            Type jshelpersType = typeof(JSHelpers);
            MethodInfo[] methods = jshelpersType.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (MethodInfo method in methods)
            {
                string methodName = method.Name;
                Delegate function = method.CreateDelegate(typeof(Func<,>).MakeGenericType(method.GetParameters().Select(p => p.ParameterType).Concat(new[] { method.ReturnType }).ToArray()), null);
                context.SetFunction(methodName, function);
            }
        }

        public object Execute(string jsCode, string name = "<UnnamedScript>", bool compile = false)
        {
            if (compile)
            {
                return context.Execute(engine.CompileScript(jsCode, name));
            }
            return context.Execute(jsCode, name);
        }

        public void RegisterFunction(string functionName, Delegate function)
        {
            context.SetFunction(functionName, function);
        }

        
        public void Dispose()
        {
            engine.Dispose();
        }

    }
}
