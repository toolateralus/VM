using Microsoft.ClearScript.JavaScript;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Printing.IndexedProperties;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media.Media3D;
using VM.GUI;
using VM.OS.Network;

namespace VM.OS.JS
{
    public class JSNetworkHelpers
    {
        // data, type, outCh, replyCh, PATH, IS_DIR,
        public event Action<byte[], NetworkConfiguration.TransmissionType, int, int, bool> OnSent;
        public Action<byte[]> OnRecieved;
        Computer Computer;
        public JSNetworkHelpers(Computer computer, Action<byte[], NetworkConfiguration.TransmissionType, int, int, bool> OutStream)
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


                Runtime.NetworkEvents[reciever] = (bytes, sender);
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
            IPAddress targetIP = null;

            if (ip is string IPString && IPAddress.TryParse(IPString, out targetIP))
            {
                ConnectToIP(targetIP, IPString);
            }
            else if (Computer.OS.Config?.Value<string>("DEFAULT_SERVER_IP") is string defaultIP && IPAddress.TryParse(defaultIP, out targetIP))
            {
                ConnectToIP(targetIP, defaultIP);
            }
        }
        private void ConnectToIP(IPAddress targetIP, string ipString)
        {
            Computer.OS.JavaScriptEngine.InteropModule.print($"Trying to connect to: {ipString}");

            Computer.Network.TryHaltCurrentConnection();

            try
            {
                Computer.Network.StartClient(targetIP);

                if (Computer.Network.IsConnected())
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Successfully connected to {ipString}.");
                }
                else
                {
                    Computer.OS.JavaScriptEngine.InteropModule.print($"Failed to connect to {ipString} :: Not found.");
                }
            }
            catch (Exception e)
            {
                Computer.OS.JavaScriptEngine.InteropModule.print($"Failed to connect to {ipString} :: {e.Message}");
            }
        }
        public void upload(string path)
        {
            var isDir = false;

            if (Runtime.GetResourcePath(path) is not string AbsPath)
            {
                // non existent file.
                return;
            }

            isDir = Directory.Exists(AbsPath) && !File.Exists(AbsPath);

            if (isDir)
            {
                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    if (Directory.Exists(item))
                    {
                        OnSent?.Invoke(Encoding.UTF8.GetBytes(item.Replace(AbsPath, "")), NetworkConfiguration.TransmissionType.Path, -1, -1, true);
                        Notifications.Now($"Uploading directory item: from {path}::{item}");
                    }
                    else if (File.Exists(item))
                    {
                        OnSent?.Invoke(Encoding.UTF8.GetBytes(item.Replace(AbsPath, "")), NetworkConfiguration.TransmissionType.Path, -1, -1, false);
                        OnSent?.Invoke(File.ReadAllBytes(item), NetworkConfiguration.TransmissionType.Data, -1, -1, false);
                        Notifications.Now("Uploading path: " + item);
                    }
                }
            }
            else
            {
                OnSent?.Invoke(Encoding.UTF8.GetBytes(path), NetworkConfiguration.TransmissionType.Path, -1, -1, false);
                OnSent?.Invoke(File.ReadAllBytes(AbsPath), NetworkConfiguration.TransmissionType.Data, -1, -1, false);
                Notifications.Now("Uploading path: " + path);
            }
        }
        public void send(params object?[]? parameters)
        {
            int outCh, inCh;
            object? msg;
            byte[] outgoingData = null;

            if (parameters is not null && parameters.Length > 2)
            {
                msg = parameters[2];

                // Process and convert the message to byte array if necessary

                if (outgoingData != null)
                {
                    // Specify the appropriate channel and reply values
                    outCh = 0; // Specify the outgoing channel
                    inCh = 0;  // Specify the reply channel

                    OnSent?.Invoke(outgoingData, NetworkConfiguration.TransmissionType.Message, outCh, inCh,  false);
                    Runtime.Broadcast(outCh, inCh, Encoding.UTF8.GetString(outgoingData)); 
                }
            }
        }
        public object? recieve(params object?[]? parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch) 
            {
                var result = Runtime.PullEvent(ch, Computer);
                var val = result.value;

                if (val is byte[] message)
                {
                    byte[] InChannel = BitConverter.GetBytes(ch);
                    byte[] ReplyChannel = BitConverter.GetBytes(result.reply);

                    byte[] combinedBytes = new byte[message.Length + sizeof(int) + sizeof(int)];

                    Array.Copy(message, 0, combinedBytes, 0, message.Length);
                    Array.Copy(InChannel, 0, combinedBytes, message.Length, sizeof(int));
                    Array.Copy(InChannel, 0, combinedBytes, message.Length + sizeof(int), sizeof(int));

                    OnRecieved?.Invoke(combinedBytes);

                }
                return val;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            return null;
        }
    }
}
