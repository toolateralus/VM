using Lemur.FS;
using Lemur.JavaScript.Api;
using Lemur.JavaScript.Network;
using Lemur.Windowing;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lemur.JavaScript.Embedded
{
    public delegate void TransmissionStream(string data, TransmissionType type, int outCh, int replyCh, bool isDir);
    public class network
    {
        public event TransmissionStream OnTransmit;
        private int size;

        internal string processID;

        public network()
        {
            OnTransmit = Computer.Current.NetworkConfiguration.OnSendMessage;
        }

        public void addListener(string methodName)
        {
            if (Computer.GetProcess(processID) is Process p)
            {
                if (p.UI.JavaScriptEngine.Disposing)
                {
                    Notifications.Now("JavaScript engine was disposing");
                    return;
                }

                p.UI.JavaScriptEngine.CreateNetworkEventHandler(processID, methodName);
            }
        }

        public void removeListener(string methodName)
        {
            if (Computer.GetProcess(processID) is Process p)
            {
                if (p.UI.JavaScriptEngine.Disposing)
                {
                    Notifications.Now("JavaScript engine was disposing");
                    return;
                }

                p.UI.JavaScriptEngine.RemoveNetworkEventHandler(processID, methodName);
            }
        }


        public string? ip()
        {
            return LANIPFetcher.GetLocalIPAddress().MapToIPv4().ToString();
        }
        public void disconnect()
        {
            Computer.Current.NetworkConfiguration.StopClient();
            Notifications.Now("Stopping connection to client");
        }

        public void connect(object? ip)
        {
            IPAddress? targetIP = null;

            if (ip is string IPString && IPAddress.TryParse(IPString, out targetIP))
                ConnectToIP(targetIP, IPString);
            else if (Computer.Current.Config?.Value<string>("DEFAULT_SERVER_IP") is string defaultIP && IPAddress.TryParse(defaultIP, out targetIP))
                ConnectToIP(targetIP, defaultIP);
            else
            {
                Notifications.Now("DEFAULT_SERVER_IP not found in this computer's config, nor was an IP provided. please, enter an IP address to connect to.");
            }
        }
        private void ConnectToIP(IPAddress targetIP, string ipString)
        {
            Notifications.Now($"Trying to connect to: {ipString}");

            Computer.Current?.NetworkConfiguration?.StopClient();

            try
            {
                Computer.Current?.NetworkConfiguration?.StartClient(targetIP);

                if (Computer.Current.NetworkConfiguration.IsConnected())
                {
                    Notifications.Now($"Successfully connected to {ipString}.");
                }
                else
                {
                    Notifications.Now($"Failed to connect to {ipString} :: Not found.");
                }
            }
            catch (Exception e)
            {
                Notifications.Now($"Failed to connect to {ipString} :: {e.Message}");
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

                OnTransmit?.Invoke(root_dir, TransmissionType.Path, -1, -1, true);

                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    string strPath = item.Replace(AbsPath, root_dir);
                    try
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(item).ConfigureAwait(false);

                        if (Directory.Exists(item))
                        {
                            OnTransmit?.Invoke(strPath, TransmissionType.Path, -1, -1, true);
                            Notifications.Now($"Uploading directory item: from {strPath}::{item}");
                        }
                        else if (File.Exists(item))
                        {
                            OnTransmit?.Invoke(strPath, TransmissionType.Path, -1, -1, false);
                            OnTransmit?.Invoke(Convert.ToBase64String(fileBytes), TransmissionType.Data, -1, -1, false);
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
                byte[] fileBytes = await File.ReadAllBytesAsync(AbsPath).ConfigureAwait(false);

                OnTransmit?.Invoke(path, TransmissionType.Path, -1, -1, false);
                OnTransmit?.Invoke(Convert.ToBase64String(fileBytes), TransmissionType.Data, -1, -1, false);

                Notifications.Now("Uploading path: " + path);
            }
        }
        public async void check_for_downloadable_content()
        {
            OnTransmit?.Invoke("GET_DOWNLOADS", TransmissionType.Request, -1, Server.RequestReplyChannel, false);
            var (value, reply) = await NetworkConfiguration.PullEventAsync(Server.RequestReplyChannel).ConfigureAwait(false);
            if (value is string rVal && JObject.Parse(rVal).Value<string>("data") is string data)
            {
                Notifications.Now(data);
            }
            return;
        }
        public async void download(string path)
        {

            if (!Computer.Current.NetworkConfiguration.IsConnected())
            {
                Notifications.Now("Not connected to network");
                return;
            }

            Notifications.Now($"Downloading {path}..");

            string root;

            if (Computer.Current.Config.ContainsKey("DOWNLOAD_PATH"))
            {
                root = Computer.Current.Config.Value<string>("DOWNLOAD_PATH") ?? throw new InvalidDataException("invalid value as DOWNLOAD_PATH in config.");
            }
            else
                root = FileSystem.Root + "/home/downloads";

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            OnTransmit?.Invoke(path, TransmissionType.Download, 0, Server.DownloadReplyChannel, false);

            while (Computer.Current.NetworkConfiguration.IsConnected())
            {
                (object? value, int reply) = await NetworkConfiguration.PullEventAsync(Server.DownloadReplyChannel);

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

                if (metadata.Value<string>("path") is not string pathString)
                {
                    Notifications.Now($"Invalid path for {path}");
                    return;
                }
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

        public bool IsConnected => Computer.Current.NetworkConfiguration.IsConnected();
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
                OnTransmit?.Invoke(message, TransmissionType.Message, channel, replyChannel, false);
                var json = Server.ToJson(message, TransmissionType.Message, channel, replyChannel, false);
                NetworkConfiguration.Broadcast(channel, replyChannel, json);
            }
        }
        public object? listen(params object?[]? parameters)
        {
            (object? value, int reply) @event = default;

            if (parameters is null || parameters.Length == 0)
            {
                Notifications.Now("Insufficient parameters for a network connection");
                return null;
            }
            if (parameters[0] is string channelString && int.TryParse(channelString, out int ch))
            {
                @event = NetworkConfiguration.PullEvent(ch);
            }
            else if (parameters[0] is int chInt)
            {
                @event = NetworkConfiguration.PullEvent(chInt);
            }
            else
            {
                Notifications.Now($"Invalid parameter for listen {parameters[0]}");
                return null;
            }

            return @event.value;
        }

    }
}
