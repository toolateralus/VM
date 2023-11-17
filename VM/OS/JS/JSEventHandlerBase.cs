using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using VM.GUI;

namespace VM.JS
{
    public class JSEventHandler : IDisposable
    {
        private const string ARGS_STRING = "(arg1, arg2)";
        
        /// <summary>
        /// this is the actual call handle
        /// </summary>
        public string? FUNCTION_HANDLE;
        
        /// <summary>
        /// this is the handle relative to the object, we don't call this
        /// </summary>
        public readonly string? m_MethodHandle;

        public Action? OnDispose;
        public Thread? ExecutionThread = null;
        public JavaScriptEngine? JavaScriptEngine;

        public int IterationDelay { get; set; }
        public bool Disposing { get; set; }
        public virtual string CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{ARGS_STRING}";
            var id = $"{identifier}{methodName}";
            string func = $"function {id} {ARGS_STRING} {{ {event_call}; }}";
            Task.Run(() => JavaScriptEngine?.Execute(func));
            return id;
        }
      
        public virtual void HeavyWorkerLoop()
        {
            while (!Disposing && !JavaScriptEngine.Disposing)
            {
                InvokeEventUnsafe(null, null);
                Thread.Sleep(IterationDelay);
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
                    JavaScriptEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, arg1, arg2);
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
                JavaScriptEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, arg1, arg2);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposing)
            {
                if (disposing)
                    Task.Run(() => ExecutionThread?.Join());
                Disposing = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~JSEventHandler()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}