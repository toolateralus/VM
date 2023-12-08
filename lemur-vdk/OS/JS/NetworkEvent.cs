using System;
using System.Threading.Tasks;

namespace Lemur.JS
{
    internal class NetworkEvent : InteropFunction
    {
        public const string ARGS_STRING = "(channel, replyChannel, data)";
        public NetworkEvent(Engine javaScriptEngine, string identifier, string methodName)
        {
            base.javaScriptEngine = javaScriptEngine;
            Task.Run(async delegate { functionHandle = await CreateFunction(identifier, methodName); });
        }

        public override async Task<string> CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{ARGS_STRING}";
            var id = $"Network{identifier}{methodName}";
            string func = $"function {id} {ARGS_STRING} {{ {event_call}; }}";
            Task.Run(() => javaScriptEngine?.Execute(func));
            return id;
        }
        /// <summary>
        /// This is intentionally hidden
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private new void InvokeEvent(object? arg1 = null, object? arg2 = null) { }
        public void InvokeEvent(object? channel = null, object? replyChannel = null, object? data = null)
        {
            try
            {
                javaScriptEngine.m_engine_internal.CallFunction(functionHandle, channel, replyChannel, data);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
    }
}