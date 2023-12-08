using System;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using lemur.Windowing;

namespace Lemur.JS
{
    public class InteropFunction
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
        protected Action? onDispose;
        public Thread? executionThread = null;
        public Engine? javaScriptEngine;
        public bool Running { get; set; }
        
        public void ForceDispose() => onDispose?.Invoke();
        public virtual void HeavyWorkerLoop()
        {
            Running = true;
            while (Running && javaScriptEngine?.Disposing == false)
            {
                try
                {
                    if (javaScriptEngine?.Disposing == false && javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                        InvokeEventImmediate(null, null);
                }
                catch (Exception e1)
                {
                    Notifications.Exception(e1);
                }
            }
        }
        public virtual void InvokeGeneric(object? sender, object? arguments)
        {
            InvokeEventBackground();
        }
        public virtual void InvokeEventBackground(object? arg1 = null, object? arg2 = null)
        {
            try
            {
                Task.Run(() => { 
                    if (javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                        javaScriptEngine?.m_engine_internal?.CallFunction(functionHandle, arg1, arg2);
                });
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
        public virtual void InvokeEventImmediate(object? arg1 = null, object? arg2 = null)
        {
            try
            {
                javaScriptEngine.m_engine_internal.CallFunction(functionHandle, arg1, arg2);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
        public virtual async Task<string> CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{argsString}";
            var id = $"{identifier}{methodName}";
            string func = $"function {id} {argsString} {{ {event_call}; }}";
            await javaScriptEngine?.Execute(func);
            return id;
        }
    }
}