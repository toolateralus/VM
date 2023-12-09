using Lemur.JS;
using Lemur.Windowing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lemur.JavaScript.Api
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
        internal protected Action? OnEventDisposed;
        public Thread? executionThread = null;
        public Engine? javaScriptEngine;
        public bool Running { get; set; }

        private int ErrorCount;
        private const int MaxErrorsBeforeTermination = 10;

        public void ForceDispose() => OnEventDisposed?.Invoke();
        public virtual void RenderLoop()
        {
            Running = true;
            while (Running && javaScriptEngine?.Disposing == false)
            {
                try
                {
                    if (javaScriptEngine?.Disposing == false && javaScriptEngine.m_engine_internal.HasVariable(functionHandle))
                        InvokeEventImmediate(null, null);
                }
                catch (Exception e)
                {
                    Throw(e);
                }
            }
        }
        public virtual void InvokeGeneric(object? sender, object? arguments)
        {
            InvokeEventBackground();
        }
        public virtual async void InvokeEventBackground(object? arg1 = null, object? arg2 = null)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    if (javaScriptEngine is null)
                        return;

                    if (javaScriptEngine.m_engine_internal.Evaluate<bool>($"{functionHandle} === undefined"))
                        throw new MissingMethodException();

                    javaScriptEngine?.m_engine_internal?.CallFunction(functionHandle, arg1, arg2);
                }
                catch (Exception e)
                {
                    Throw(e);
                }
            });
        }

        private void Throw(Exception e)
        {
            ErrorCount++;
            if (ErrorCount > MaxErrorsBeforeTermination)
            {
                ForceDispose();
            }
            else
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
                Throw(e);
            }
        }
        public virtual async Task<string> CreateFunction(string procID, string methodName)
        {
            var event_call = $"{procID}.{methodName}{argsString}";
            var id = $"{procID}{methodName}";
            string func = $"function {id} {argsString} {{ {event_call}; }}";
            await javaScriptEngine?.Execute(func);
            return id;
        }
    }
}