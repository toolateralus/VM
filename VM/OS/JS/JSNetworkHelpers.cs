using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using VM.GUI;
using VM.OS.Network;
using VM.OS.Network.Server;

namespace VM.OS.JS
{
    public class JSNetworkHelpers
    {
        // data, type, outCh, replyCh, IS_DIR,
        public event Action<byte[], TransmissionType, int, int, bool> OnSent;
        public Action<byte[]> OnRecieved;
        Computer Computer;
        public JSNetworkHelpers(Computer computer, Action<byte[], TransmissionType, int, int, bool> OutStream)
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

                Runtime.Broadcast(sender_ch, reciever_ch, dataBytes);
            };
            Computer = computer;
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

                OnSent?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);

                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    string strPath = item.Replace(AbsPath, root_dir);
                    bytePath = Encoding.UTF8.GetBytes(strPath);
                    try
                    {
                        byte[] fileBytes = File.ReadAllBytes(item);

                        if (Directory.Exists(item))
                        {
                            OnSent?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);
                            Notifications.Now($"Uploading directory item: from {strPath}::{item}");
                        }
                        else if (File.Exists(item))
                        {
                            OnSent?.Invoke(bytePath, TransmissionType.Path, -1, -1, false);
                            OnSent?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);
                            Notifications.Now("Uploading path: " + item);
                        }
                    }
                    catch (Exception ex) when (ex is not UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Caught exception: {ex.Message}");
                    }
                }
            }
            else
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                byte[] fileBytes = File.ReadAllBytes(AbsPath.Split('\\').Last());

                OnSent?.Invoke(pathBytes, TransmissionType.Path, -1, -1, false);
                OnSent?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);

                Notifications.Now("Uploading path: " + path);
            }
        }
        public object downloads()
        {
            OnSent?.Invoke(Encoding.UTF8.GetBytes("GET_DOWNLOADS"), TransmissionType.Request, -1, NetworkConfiguration.REQUEST_RESPONSE_CHANNEL, false);
            var response = Runtime.PullEvent(NetworkConfiguration.REQUEST_RESPONSE_CHANNEL, Computer);
            var stringResponse = Encoding.UTF8.GetString(response.value as byte[] ?? Encoding.UTF8.GetBytes("No data found"));
            return stringResponse;
        }
        public object? download(string path)
        {
            OnSent?.Invoke(Encoding.UTF8.GetBytes(path), TransmissionType.Download, 0, NetworkConfiguration.DOWNLOAD_RESPONSE_CHANNEL, false);

            var root = Computer.OS.FS_ROOT + "\\downloads";

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            while (true)
            {
                var pathItem = Encoding.UTF8.GetString((byte[])Runtime.PullEvent(NetworkConfiguration.DOWNLOAD_RESPONSE_CHANNEL, Computer).value);
                var data = (byte[])Runtime.PullEvent(NetworkConfiguration.DOWNLOAD_RESPONSE_CHANNEL, Computer).value;

                if (pathItem == "END_DOWNLOAD")
                {
                    break;
                }

                // Combine the root path and the received path item to get the full file path
                var fullPath = Path.Combine(root, pathItem);

                // Create the directory structure if it doesn't exist
                var directoryPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Save the file using the full path
                File.WriteAllBytes(fullPath, data);
            }

            // Return whatever you want as the result of the download method
            return null;
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

                    OnSent?.Invoke(outgoingData, TransmissionType.Message, outCh, inCh,  false);
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
