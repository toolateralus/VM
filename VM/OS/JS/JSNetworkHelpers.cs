using System;
using System.Diagnostics;
using System.Net;
using VM.GUI;

namespace VM.OS.JS
{
    public class JSNetworkHelpers
    {
        // record level nullability
        public event Action<object?[]?>? OnSent;
        public Action<object?[]?>? OnRecieved;
        Computer Computer;
        public JSNetworkHelpers(Computer computer, Action<object?[]?>? Output, Action<object?[]?>? Input)
        {
            OnSent += Output;
            OnRecieved += Input;
            Computer = computer;
        }
        public void print(object message)
        {
            Debug.WriteLine(message);
        }
        public void connect(object? ip)
        {
            if (ip is string IPString && IPAddress.Parse(IPString) is IPAddress IP)
            {
                Computer.OS.JavaScriptEngine.InteropModule.print($"Trying to connect to : {IPString}");

                Computer.Network.TryHaltCurrentConnection();
                try
                {
                    Computer.Network.StartClient(IP);
                }
                catch (Exception e)
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Failed to connect to : {IPString} :: {e.Message}");
                }
                finally
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Successfully connected to {IPString}.");
                }
            }
            else if (Computer.OS.Config?.Value<string>("DEFAULT_SERVER_IP") is string _IP && IPAddress.Parse(_IP) is var __IP)
            {
                Computer.OS.JavaScriptEngine.InteropModule.print($"Trying to connect to : {__IP}");

                Computer.Network.TryHaltCurrentConnection();
                try
                {
                    Computer.Network.StartClient(__IP);
                }
                catch (Exception e)
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Failed to connect to : {__IP} :: {e.Message}");
                }
                finally
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Successfully connected to {__IP}.");
                }
            }
        }
        public void send(params object?[]? parameters)
        {
            OnSent?.Invoke(parameters);

            int outCh, inCh;
            object msg;

            if (parameters is not null && parameters.Length > 2)
            {
                msg = parameters[2];
                if (parameters[0] is int _out && parameters[1] is int _in)
                {
                    outCh = _out;
                    inCh = _in;
                    Runtime.Broadcast(outCh, inCh, msg);
                    return;
                }
            }
            Notifications.Now("Insufficient arguments for a network connection");


        }
        public object? recieve(params object?[]? parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch &&
                parameters != null && parameters.Length > 0 && parameters[0] is int replyCh)
            {
                var val = Runtime.PullEvent(ch).value;
                OnRecieved?.Invoke(new[] { val });
                return val;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            return null;
        }
    }
}
