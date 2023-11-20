using System;
using System.Threading.Tasks;

namespace Lemur.JS
{
    internal class NetworkEventHandler : JSEventHandler
    {
        public const string ARGS_STRING = "(channel, replyChannel, data)";
        public NetworkEventHandler(JavaScriptEngine javaScriptEngine, string identifier, string methodName)
        {
            JavaScriptEngine = javaScriptEngine;
            FUNCTION_HANDLE = CreateFunction(identifier, methodName);
        }

        public override string CreateFunction(string identifier, string methodName)
        {
            var event_call = $"{identifier}.{methodName}{ARGS_STRING}";
            var id = $"Network{identifier}{methodName}";
            string func = $"function {id} {ARGS_STRING} {{ {event_call}; }}";
            Task.Run(() => JavaScriptEngine?.Execute(func));
            return id;
        }
        private new void InvokeEvent(object? arg1 = null, object? arg2 = null) { }
        public void InvokeEvent(object? channel = null, object? replyChannel = null, object? data = null)
        {
            try
            {
                JavaScriptEngine.ENGINE_JS.CallFunction(FUNCTION_HANDLE, channel, replyChannel, data);
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
        }
    }
}