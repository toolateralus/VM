using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace VM.JS
{
    public class JSEventHandler : IDisposable
    {

        const string ARGS_STRING = "(arg1, arg2)";
        /// <summary>
        /// this is the actual call handle
        /// </summary>
        public string FUNCTION_HANDLE;
        /// <summary>
        /// this is the handle relative to the object, we don't call this
        /// </summary>
        public readonly string methodHandle;
        public Action OnUnhook;
        public Thread? thread = null;
        public JavaScriptEngine jsEngine;
        public int DELAY_BETWEEN_WORK_ITERATIONS { get; set; }

        public bool Disposing { get; set; }
        public virtual string CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{ARGS_STRING}";
            var id = $"{identifier}{methodName}";
            string func = $"function {id} {ARGS_STRING} {{ {event_call}; }}";
            Task.Run(() => jsEngine?.Execute(func));
            return id;
        }
        public virtual void Dispose()
        {
            if (!Disposing)
                thread?.Join(10_000);

            Disposing = true;
        }
        public virtual void HeavyWorkerLoop()
        {
            while (!Disposing && !jsEngine.Disposing)
            {
                InvokeEventUnsafe(null, null);
                Thread.Sleep(DELAY_BETWEEN_WORK_ITERATIONS);
            }
            Dispose();
        }
        public virtual void InvokeGeneric(object? sender, object? arguments)
        {
            InvokeEvent();
        }
        public virtual void InvokeEvent(object? arg1 = null, object? arg2 = null)
        {
            Task.Run(() =>
            {
                try
                {
                    jsEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, arg1, arg2);
                }
                catch (Exception e)
                {
                    Notifications.Exception(e);
                }
            });
        }
        public virtual void InvokeEventUnsafe(object? arg1 = null, object? arg2 = null)
        {
            try
            {
                jsEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, arg1, arg2);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
    }
}