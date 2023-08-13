using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VM.GUI;
using VM.OS.Network;

namespace VM.OS.JS
{
    public class JSNetworkHelpers
    {
        public event Action<byte[]> OnSent;
        public Action<byte[]> OnRecieved;
        Computer Computer;
        public JSNetworkHelpers(Computer computer, Action<byte[]> OutStream)
        {
            OnSent = OutStream;
            computer.Network.OnMessageRecieved += OnRecieved;
            computer.Network.OnMessageRecieved += (bytes) =>
            {
                var bytesLength = bytes.Length;
                int reciever = BitConverter.ToInt32(bytes, bytesLength - 8);
                int sender = BitConverter.ToInt32(bytes, bytesLength - 4);
                string message = Encoding.ASCII.GetString(bytes, 0, bytesLength - 8);
                if (bytesLength <= 1000)
                    Console.WriteLine($"Received from server: {sender} to {reciever} \"{message}\"");
                else
                    Console.WriteLine($"Received from server: {sender} to {reciever}, {FormatBytes(bytesLength)}");
                Runtime.NetworkEvents[reciever] = (message, sender);
            };
            Computer = computer;
        }
        static string FormatBytes(long bytes, int decimals = 2)
        {
            if (bytes == 0) return "0 Bytes";

            const int k = 1024;
            string[] units = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int i = Convert.ToInt32(Math.Floor(Math.Log(bytes) / Math.Log(k)));
            return string.Format("{0:F" + decimals + "} {1}", bytes / Math.Pow(k, i), units[i]);
        }
        public string? ip()
        {
            return NetworkConfiguration.LAST_KNOWN_SERVER_IP;
        }
        public void connect(object? ip)
        {
            if (ip is string IPString && IPAddress.TryParse(IPString, out var IP))
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
            else if (Computer.OS.Config?.Value<string>("DEFAULT_SERVER_IP") is string _IP && IPAddress.TryParse(_IP, out var __IP))
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

        public async Task<TaskCompletionSource<object>>? recieve_async(params object?[]? parameters)
        {
            var tcs = new TaskCompletionSource<object>();

            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch)
            {
                var result = await Runtime.PullEvent(ch, Computer);

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
                tcs.SetResult(val);
                return tcs;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            tcs.SetException(new ArgumentNullException("Insufficient arguments for a network connection"));
            return tcs;
        }

        public object? recieve(params object?[]? parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch) 
            {
                var result = Runtime.PullEvent(ch, Computer);
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
