using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Lemur.FS;
using Lemur.GUI;
using Lemur.Network;
using Lemur.Network.Server;

namespace Lemur.JS
{
    public delegate void TransmissionStream(byte[] data, TransmissionType type, int outCh, int replyCh, bool isDir);
    public class Network
    {
        public event TransmissionStream OnTransmit;
        Computer Computer;
        private int size;

        public Network(Computer computer, TransmissionStream transmissionStream)
        {
            OnTransmit = transmissionStream;
            Computer = computer;
        }
        
        public string? ip()
        {
            return LANIPFetcher.GetLocalIPAddress().MapToIPv4().ToString();
        }
        public void disconnect()
        {
            Computer.Network.StopClient();
            Notifications.Now("Stopping connection to client");
        }

        public void connect(object? ip)
        {
            IPAddress? targetIP = null;

            if (ip is string IPString && IPAddress.TryParse(IPString, out targetIP))
                ConnectToIP(targetIP, IPString);
            else if (Computer.config?.Value<string>("DEFAULT_SERVER_IP") is string defaultIP && IPAddress.TryParse(defaultIP, out targetIP))
                ConnectToIP(targetIP, defaultIP);
            else
            {
                Notifications.Now("DEFAULT_SERVER_IP not found in this computer's config, nor was an IP provided. please, enter an IP address to connect to.");
            }
        }
        private async Task ConnectToIP(IPAddress targetIP, string ipString)
        {
            Computer.javaScript.InteropModule.print($"Trying to connect to: {ipString}");

            Computer.Network.StopClient();

            try
            {
                await Computer.Network.StartClient(targetIP);

                if (Computer.Network.IsConnected())
                {
                    Computer.javaScript.InteropModule.print($"Successfully connected to {ipString}.");
                }
                else
                {
                    Computer.javaScript.InteropModule.print($"Failed to connect to {ipString} :: Not found.");
                }
            }
            catch (Exception e)
            {
                Computer.javaScript.InteropModule.print($"Failed to connect to {ipString} :: {e.Message}");
            }
        }
        public async void upload(string path)
        {
            var isDir = false;

            if (FileSystem.GetResourcePath(path) is not string AbsPath)
            {
                // non existent file.
                return;
            }

            isDir = Directory.Exists(AbsPath) && !File.Exists(AbsPath);

            if (isDir)
            {
                string root_dir = AbsPath.Split('\\').Last();
                byte[] bytePath = Encoding.UTF8.GetBytes(root_dir);

                OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);

                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    string strPath = item.Replace(AbsPath, root_dir);
                    bytePath = Encoding.UTF8.GetBytes(strPath);
                    try
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(item);

                        if (Directory.Exists(item))
                        {
                            OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);
                            Notifications.Now($"Uploading directory item: from {strPath}::{item}");
                        }
                        else if (File.Exists(item))
                        {
                            OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, false);
                            OnTransmit?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);
                            Notifications.Now("Uploading path: " + item);
                        }
                    }
                    catch (Exception ex) when (ex is not UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Caught exception: {ex.Message}");
                    }
                }
            }
            else if (File.Exists(AbsPath))
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                byte[] fileBytes = await File.ReadAllBytesAsync(AbsPath);

                OnTransmit?.Invoke(pathBytes, TransmissionType.Path, -1, -1, false);
                OnTransmit?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);

                Notifications.Now("Uploading path: " + path);
            }
        }
        public async void check_for_downloadable_content()
        {
            OnTransmit?.Invoke(Encoding.UTF8.GetBytes("GET_DOWNLOADS"), TransmissionType.Request, -1, Server.REQUEST_REPLY_CHANNEL, false);
            var response = await NetworkConfiguration.PullEventAsync(Server.REQUEST_REPLY_CHANNEL, Computer);
            if (response.value is string rVal &&
                JObject.Parse(rVal).Value<string>("data") is string data)
            {
                Notifications.Now(data);
            }
            return;
        }
        public async void download(string path)
        {

            if (!Computer.Network.IsConnected())
            {
                Notifications.Now("Not connected to network");
                return;
            }

            Notifications.Now($"Downloading {path}..");

            var root = Computer.FileSystemRoot + "\\downloads";

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            OnTransmit?.Invoke(Encoding.UTF8.GetBytes(path), TransmissionType.Download, 0, Server.DOWNLOAD_REPLY_CHANNEL, false);

            while (Computer.Network.IsConnected())
            {
                (object? value, int reply) = await NetworkConfiguration.PullEventAsync(Server.DOWNLOAD_REPLY_CHANNEL, Computer);
                string pathString = null;

                if (value is not JObject metadata)
                {
                    if (value is string s && JObject.Parse(s).Value<string>("data") is string dataStr)
                    {
                        switch (dataStr)
                        {
                            case "END_DOWNLOAD":
                                Notifications.Now($"{{{Server.FormatBytes(size)}}} downloads\\{path} downloaded.. run  the <install '{path}' to install it.");
                                return;
                            case "FAILED_DOWNLOAD":
                                Notifications.Now($"Download failed for {path}");
                                return;
                        }
                    }
                    Notifications.Now($"Invalid data gotten from server for {path}");
                    return;
                }

                if (metadata.Value<string>("data") is not string dataString || Encoding.UTF8.GetBytes(dataString) is not byte[] dataBytes)
                {
                    Notifications.Now($"Invalid data for {path}");
                    return;
                }

                if (Convert.FromBase64String(metadata.Value<string>("path")) is not byte[] pathBytes)
                {
                    Notifications.Now($"Invalid path for {path}");
                    return;
                }

                pathString = Encoding.UTF8.GetString(pathBytes);


                var fullPath = Path.Combine(root, pathString);

                var directoryPath = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllBytes(fullPath, dataBytes);
                size += dataBytes.Length;

                await Task.Delay(1);
            }
            Notifications.Now("Not connected to network, or download failed");
        }
      
        public bool IsConnected => Computer.Network.IsConnected();
        public void send(params object?[]? parameters)
        {
            if (parameters is null || parameters.Length <= 2)
            {
                return;
            }
            if (parameters[0] is int channel &&
                parameters[1] is int replyChannel &&
                parameters[2] is string message)
            {
                byte[] outgoingData = Encoding.UTF8.GetBytes(message);
                if (outgoingData != null)
                {
                    OnTransmit?.Invoke(outgoingData, TransmissionType.Message, replyChannel, channel, false);
                    var json = Server.ToJson(outgoingData.Length, outgoingData, TransmissionType.Message, replyChannel, channel, false);
                    NetworkConfiguration.Broadcast(channel, replyChannel, json);
                }
            }
        }
        public object? receive(params object?[]? parameters)
        {
            (object? value, int reply) @event = default;

            Task get_task = new(async delegate { 
                if (parameters is null || parameters.Length == 0)
                {
                    Notifications.Now("Insufficient parameters for a network connection");
                    return;
                }
                if (parameters[0] is string p1 && int.TryParse(p1, out int ch))
                {
                    @event = await NetworkConfiguration.PullEventAsync(ch, Computer);
                }
                else if (parameters[0] is int chInt)
                {
                    @event = await NetworkConfiguration.PullEventAsync(chInt, Computer);
                }
                else
                {
                    Notifications.Now($"Invalid parameter for receive {parameters[0]}");
                    return;
                }
            });

            return @event.value;
        }
        public void eventHandler(string identifier, string methodName)
        {
            if (Computer.Windows.TryGetValue(identifier, out var app))
                app.JavaScriptEngine?.CreateNetworkEventHandler(identifier, methodName);
        }
    }
}
