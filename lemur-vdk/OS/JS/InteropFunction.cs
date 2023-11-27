using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Lemur.JS
{
    public class InteropFunction : IDisposable
    {
        
        private const string argsString = "(arg1, arg2)";
        
        /// <summary>
        /// this is the actual call handle
        /// </summary>
        public string? functionHandle;
        
        /// <summary>
        /// this is the handle relative to the object, we don't call this
        /// </summary>
        public readonly string? m_MethodHandle;

        public void ForceDispose() => onDispose?.Invoke();

        protected Action? onDispose;
        public Thread? executionThread = null;
        public Engine? javaScriptEngine;
        public bool Running { get; private set; }

        public bool Disposing { get; set; }
        public virtual async Task<string> CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{argsString}";
            var id = $"{identifier}{methodName}";
            string func = $"function {id} {argsString} {{ {event_call}; }}";
            await javaScriptEngine?.Execute(func);
            return id;
        }
      
        public virtual void HeavyWorkerLoop(object? sender, DoWorkEventArgs e)
        {
            Running = true;
            while (Running)
            {
                try
                {
                    if (javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                        InvokeEventImmediate(null, null);
                }
                catch (Exception e)
                {
                    Notifications.Exception(e);
                }
            }
            Dispose();
        }
        public virtual void InvokeGeneric(object? sender, object? arguments)
        {
            InvokeEventBackground();
        }
        public virtual void InvokeEventBackground(object? arg1 = null, object? arg2 = null)
        {
            Task.Run(() =>
            {
                try
                {
                    if (javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                        javaScriptEngine?.m_engine_internal?.CallFunction(functionHandle, arg1, arg2);
                }
                catch (Exception e)
                {
                    Notifications.Exception(e);
                }
            });
        }
        public virtual void InvokeEventImmediate(object? arg1 = null, object? arg2 = null)
        {
            try
            {
           
                if (javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                    javaScriptEngine.m_engine_internal.CallFunction(functionHandle, arg1, arg2);
                else
                {
                    Notifications.Now("Attempted to call a javascript function that didn't exist");
                }
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
                    Task.Run(() => executionThread?.Join());
                Disposing = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}