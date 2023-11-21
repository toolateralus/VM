using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Lemur.JS
{
    public class Function : IDisposable
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
        public JavaScriptEngine? javaScriptEngine;

        public bool Disposing { get; set; }
        public virtual string CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{argsString}";
            var id = $"{identifier}{methodName}";
            string func = $"function {id} {argsString} {{ {event_call}; }}";
            Task.Run(() => javaScriptEngine?.Execute(func));
            return id;
        }
      
        public virtual void HeavyWorkerLoop()
        {
            while (!Disposing && !javaScriptEngine.Disposing)
            {
                InvokeEventUnsafe(null, null);
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
                    javaScriptEngine.m_engine_internal.CallFunction(functionHandle, arg1, arg2);
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
                // TODO: open an issue for this

                // BUG : when opening xaml/wpf apps that have a call to create an event in the constructor (every app ever)
                // there is unpredictable latency between the call to create the event and the end of the ctor, 
                // so there's always a window where the event will be looking to get called when the member function / method 
                // might not exit yet.
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