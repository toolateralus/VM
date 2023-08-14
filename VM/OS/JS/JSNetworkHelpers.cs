using Microsoft.ClearScript.JavaScript;
using Newtonsoft.Json.Linq;
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
                string JsonString = Encoding.UTF8.GetString(bytes);
                var metadata = JObject.Parse(JsonString);
                int messageLength = metadata.Value<int>("size");
                int sender_ch = metadata.Value<int>("ch");
                int reciever_ch = metadata.Value<int>("reply");
                string dataString = metadata.Value<string>("data") ?? "Data not found! something has gone wrong with the client's json construction";
                var dataBytes = Convert.FromBase64String(dataString);
                var bytesLength = dataBytes.Length;

                Runtime.Broadcast(NetworkConfiguration.RequestReplyChannel, -1, dataBytes);

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
                string root_dir = AbsPath.Split('\\').Last();
                byte[] bytePath = Encoding.UTF8.GetBytes(root_dir);

                OnSent?.Invoke(bytePath, NetworkConfiguration.TransmissionType.Path, -1, -1, true);

                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    string strPath = item.Replace(AbsPath, root_dir);
                    bytePath = Encoding.UTF8.GetBytes(strPath);
                    byte[] fileBytes = File.ReadAllBytes(item);

                    if (Directory.Exists(item))
                    {
                        OnSent?.Invoke(bytePath, NetworkConfiguration.TransmissionType.Path, -1, -1, true);
                        Notifications.Now($"Uploading directory item: from {strPath}::{item}");
                    }
                    else if (File.Exists(item))
                    {
                        OnSent?.Invoke(bytePath, NetworkConfiguration.TransmissionType.Path, -1, -1, false);
                        OnSent?.Invoke(fileBytes, NetworkConfiguration.TransmissionType.Data, -1, -1, false);
                        Notifications.Now("Uploading path: " + item);
                    }
                }
            }
            else
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                byte[] fileBytes = File.ReadAllBytes(AbsPath.Split('\\').Last());

                OnSent?.Invoke(pathBytes, NetworkConfiguration.TransmissionType.Path, -1, -1, false);
                OnSent?.Invoke(fileBytes, NetworkConfiguration.TransmissionType.Data, -1, -1, false);

                Notifications.Now("Uploading path: " + path);
            }
        }
        public object downloads()
        {
            OnSent?.Invoke(Encoding.UTF8.GetBytes("GET_DOWNLOADS"), NetworkConfiguration.TransmissionType.Request, -1, NetworkConfiguration.RequestReplyChannel, false);
            var response = Runtime.PullEvent(NetworkConfiguration.RequestReplyChannel, Computer);
            var stringResponse = Encoding.UTF8.GetString(response.value as byte[] ?? Encoding.UTF8.GetBytes("No data found"));
            return stringResponse;
        }
        public object? download(string path, int ch)
        {
            OnSent?.Invoke(Encoding.UTF8.GetBytes(path), NetworkConfiguration.TransmissionType.Download, -1, -1, false);

            var result = Runtime.PullEvent(ch, Computer);

            if (result.value is byte[] message)
            {
                byte[] InChannel = BitConverter.GetBytes(ch);
                byte[] ReplyChannel = BitConverter.GetBytes(result.reply);

                byte[] combinedBytes = new byte[message.Length + sizeof(int) + sizeof(int)];

                Array.Copy(message, 0, combinedBytes, 0, message.Length);
                Array.Copy(InChannel, 0, combinedBytes, message.Length, sizeof(int));
                Array.Copy(InChannel, 0, combinedBytes, message.Length + sizeof(int), sizeof(int));

                OnRecieved?.Invoke(combinedBytes);
            }
            return result;
        }
        public bool IsConnected => Computer.Network.IsConnected();
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
