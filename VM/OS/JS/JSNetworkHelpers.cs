using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using VM.GUI;

namespace VM.OS.JS
{
    public class JSNetworkHelpers
    {
        public event Action<byte[]> OnSent;
        public Action<byte[]> OnRecieved;
        Computer Computer;
        public JSNetworkHelpers(Computer computer, Action<byte[]> Output, Action<byte[]> Input)
        {
            OnSent = Output;
            OnRecieved = Input;
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
            int outCh, inCh;
            object msg;

            if (parameters is not null && parameters.Length > 2)
            {
                msg = parameters[2];

                if (msg is string inputString)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(inputString);

                    if (parameters.Length >= 3 && parameters[0] is int __out && parameters[1] is int __in)
                    {
                        byte[] outBytes = BitConverter.GetBytes(__out);
                        byte[] inBytes = BitConverter.GetBytes(__in);

                        byte[] combinedBytes = new byte[byteArray.Length + sizeof(int) + sizeof(int)];
                        Array.Copy(byteArray, 0, combinedBytes, 0, byteArray.Length);
                        Array.Copy(outBytes, 0, combinedBytes, byteArray.Length, sizeof(int));
                        Array.Copy(inBytes, 0, combinedBytes, byteArray.Length + sizeof(int), sizeof(int));

                        OnSent?.Invoke(combinedBytes);
                    }
                    
                }
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
                var result = Runtime.PullEvent(ch);
                var val = result.value;

                if (val is string inputString)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(inputString);

                    byte[] outBytes = BitConverter.GetBytes(ch);
                    byte[] inBytes = BitConverter.GetBytes(result.reply);

                    byte[] combinedBytes = new byte[byteArray.Length + sizeof(int) + sizeof(int)];
                    Array.Copy(byteArray, 0, combinedBytes, 0, byteArray.Length);
                    Array.Copy(outBytes, 0, combinedBytes, byteArray.Length, sizeof(int));
                    Array.Copy(inBytes, 0, combinedBytes, byteArray.Length + sizeof(int), sizeof(int));

                    OnRecieved?.Invoke(combinedBytes);

                }
                return val;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            return null;
        }
    }
}
