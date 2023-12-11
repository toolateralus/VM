﻿using Lemur.GUI;
using Lemur.Windowing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Lemur.JavaScript.Network
{
    class Host
    {
        public int openPort { get; internal set; } = 8080;
        public static IPAddress GetLocalIPAddress()
        {
            IPAddress localIP = null;

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                    foreach (UnicastIPAddressInformation ipInformation in ipProperties.UnicastAddresses)
                    {
                        if (ipInformation.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ipInformation.Address))
                        {
                            localIP = ipInformation.Address;
                            break;
                        }
                    }

                    if (localIP != null)
                    {
                        break;
                    }
                }
            }

            return localIP;
        }
        internal bool Running;
        TcpListener? SERVER;
        public async Task Open(int port)
        {
            openPort = port;

            Running = true;

            List<TcpClient> CLIENTS = new();

            Notifications.Now($"SERVER : Starting on :: {{'ip':{GetLocalIPAddress().MapToIPv4()}, port':{openPort}}}. Waiting for connections...");

            SERVER = new TcpListener(IPAddress.Any, openPort);

            SERVER.Start();

            Server networkConfig = new Server();

            while (true)
            {
                await networkConfig.ConnectClientAsync(SERVER, CLIENTS);
            }
        }
        internal void Dispose()
        {
            SERVER?.Stop();
            SERVER ??= null;
            Running = false;
        }
    }

    public class Packet(JObject header, string message, TcpClient client, NetworkStream stream)
    {
        public JObject Metadata = header;
        public string Data = message;
        public TcpClient Client = client;
        public NetworkStream Stream = stream;
    }
    public enum TransmissionType
    {
        Path,
        Data,
        Message,
        Download,
        Request,
    }

    public class Server
    {
        public const int RequestReplyChannel = 6996;
        public const int DownloadReplyChannel = 6997;
        private readonly string UploadDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Lemur_SERVER_DATA";
        private readonly Dictionary<string, TcpClient> IncomingFileTransfersPending = [];
        private readonly List<string> AvailableForDownload = [];

        public Server()
        {
            Task.Run(() =>
            {
                if (!Directory.Exists(UploadDirectory))
                    Directory.CreateDirectory(UploadDirectory);
                // this could take awhile, do it in the background.
                foreach (var item in Directory.EnumerateFileSystemEntries(UploadDirectory))
                    AvailableForDownload.Add(item.Split('\\').Last());
            });
        }
        internal static Packet ListenForPacket(NetworkStream stream, TcpClient client, string source)
        {

            // this header will indicate the size of the actual metadata
            byte[] header = new byte[4];

            // 4 byte header for i/o channels
            if (stream.Read(header, 0, 4) <= 0)
                return default;

            int metadataLength = BitConverter.ToInt32(header, 0);

            byte[] metaData = new byte[metadataLength];

            // metadata file, json object with info's and the actual data for the xfer
            if (stream.Read(metaData, 0, metadataLength) <= 0)
                return default;

            var metadata = ParseMetadata(metaData);

            // length of the data message, we don't use this now, but for doing fragmented transfers for larger files,
            // we'd want to know the full length of the incoming data for reconstruction.
            // we can decide on a fixed buffer length for an incoming file transfer and fragment it accordingly on client side.
            int messageLength = metadata.Value<int>("size");

            // channel
            int channel = metadata.Value<int>("ch");

            // reply channel
            int reply = metadata.Value<int>("reply");

            // base64 string representation of data
            string dataString = metadata.Value<string>("data") ?? $"{source} : Data not found! something has gone wrong with the other's json construction";

            var bytesLength = Encoding.UTF8.GetByteCount(dataString);

            string message = $"\n{source}, ch {reply} -> {channel}, {FormatBytes(bytesLength)}";

            Computer.Current.Window.Dispatcher.Invoke(() => {
                foreach (var cmd in Computer.TryGetAllProcessesOfType<CommandPrompt>())
                    cmd.output.AppendText(message);
            });
            return new(metadata, dataString, client, stream);
        }
        public static JObject ParseMetadata(byte[] metaData)
        {
            try
            {
                return JObject.Parse(Encoding.UTF8.GetString(metaData));
            }
            catch (Exception e)
            {
                Notifications.Now($"Metadata parsing {e.GetType().Name}: {e.Message}");
            }
            return [];
        }
        internal async Task ConnectClientAsync(TcpListener server, List<TcpClient> connectedClients)
        {
            TcpClient client = await server.AcceptTcpClientAsync().ConfigureAwait(false);
            connectedClients.Add(client);
            Notifications.Now($"SERVER:Client {client.GetHashCode()} connected ");
            await HandleClientCommunicationAsync(client, connectedClients).ConfigureAwait(false);
        }
        private async Task HandleClientCommunicationAsync(TcpClient client, List<TcpClient> connectedClients)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                while (true)
                {
                    Packet packet = ListenForPacket(stream, client, "server");
                    await TryHandleMessages(packet, connectedClients).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Notifications.Now($"SERVER:Client {client.GetHashCode()} errored:: \n{ex.Message}\n{ex.InnerException}\n{ex}");
            }
            finally
            {
                client.Close();
                connectedClients.Remove(client);
                Notifications.Now($"SERVER:Client {client.GetHashCode()} disconnected");
            }
        }
        private async Task TryHandleMessages(Packet packet, List<TcpClient> clients)
        {
            if (packet?.Metadata?.Value<string>("type") is string tTypeStr)
            {
                var transmissionType = Enum.Parse<TransmissionType>(tTypeStr);

                switch (transmissionType)
                {
                    case TransmissionType.Path:
                        HandleIncomingPathTransmission(packet);
                        break;
                    case TransmissionType.Data:
                        HandleIncomingDataTransmission(packet);
                        break;
                    case TransmissionType.Message:
                        await HandleMessageTransmission(packet, clients);
                        break;
                    case TransmissionType.Download:
                        await HandleDownloadRequest(packet);
                        break;
                    case TransmissionType.Request:
                        HandleRequest(packet.Data, packet);
                        break;
                }
            }
        }
        private async Task HandleDownloadRequest(Packet packet)
        {
            var file = packet.Data;
            if (AvailableForDownload.Contains(file))
            {
                await SendDataRecusive(file);
                await SendDownloadMessage(packet, "END_DOWNLOAD");
            }

            async Task SendDataRecusive(string file)
            {
                string path = file;

                if (!file.Contains(UploadDirectory))
                    path = UploadDirectory + "\\" + file;

                file = file.Replace(UploadDirectory + "\\", "");

                if (File.Exists(path))
                {
                    var metadata = ToJson(path, TransmissionType.Download, DownloadReplyChannel, -1, false, file);
                    await SendJsonToClient(packet.Client, JObject.Parse(metadata));
                }
                else if (Directory.Exists(path))
                {
                    var directoryContents = Directory.GetFileSystemEntries(path);

                    foreach (var entry in directoryContents)
                    {
                        await SendDataRecusive(entry);
                    }
                }
                else
                {
                    await SendDownloadMessage(packet, "FAILED_DOWNLOAD");
                }

            }
        }
        private static async Task SendDownloadMessage(Packet packet, string Message)
        {
            // message signaling the end of the download.
            JObject endPacket = JObject.Parse(ToJson(Message, TransmissionType.Download, DownloadReplyChannel, -1));
            await SendJsonToClient(packet.Client, endPacket).ConfigureAwait(false);
        }
        private static async Task HandleMessageTransmission(Packet packet, List<TcpClient> clients)
        {
            string data = packet.Metadata.Value<string>("data") ?? "";
            var responseMetadata = JObject.Parse(ToJson(data, TransmissionType.Message, packet.Metadata.Value<int>("ch"), packet.Metadata.Value<int>("reply"), false));
            await BroadcastMessage(clients, packet.Client, responseMetadata).ConfigureAwait(false);
        }
        private void HandleIncomingDataTransmission(Packet packet)
        {
            string toRemove = "";
            foreach (var item in IncomingFileTransfersPending)
            {
                if (item.Value == packet.Client)
                {
                    if (packet.Metadata.Value<bool>("isDir"))
                    {
                        Directory.CreateDirectory(UploadDirectory + "\\" + packet.Data);
                    }
                    else
                    {
                        string path = "";

                        if (item.Key.StartsWith('\\'))
                            path = item.Key.Remove(0, 1);

                        path = UploadDirectory + "\\" + item.Key;

                        File.WriteAllBytes(path, Convert.FromBase64String(packet.Data));
                    }
                    toRemove = item.Key;
                }
            }
            if (toRemove != "")
            {
                IncomingFileTransfersPending.Remove(toRemove);
            }
        }
        private void HandleIncomingPathTransmission(Packet packet)
        {
            // write the dir, or we wait for file data.
            string path = packet.Data;
            if (packet.Metadata.Value<bool>("isDir"))
            {
                Directory.CreateDirectory(UploadDirectory + "\\" + path);
            }
            else
            {
                IncomingFileTransfersPending[path] = packet.Client;
            }
        }
        public static string ToJson(string data, TransmissionType type, int ch, int reply, bool isDir = false, string? path = null)
        {
            var json = new
            {
                size = Encoding.UTF8.GetByteCount(data),
                data,
                type = type.ToString(),
                ch,
                reply,
                isDir,
                path,
            };

            return JsonConvert.SerializeObject(json);
        }
        private async void HandleRequest(string requestType, Packet packet)
        {
            Notifications.Now($"SERVER:Client {packet.Client.GetHashCode()} has made a {requestType} request.");
            switch (requestType)
            {
                case "GET_DOWNLOADS":

                    var names = string.Join(",\n", AvailableForDownload);
                    JObject metadata = JObject.Parse(ToJson(names, TransmissionType.Request, RequestReplyChannel, -1, false));
                    await SendJsonToClient(packet.Client, metadata).ConfigureAwait(false);

                    Notifications.Now($"SERVER:Responding with {names}");
                    break;
                default:
                    Notifications.Now($"SERVER:Client made unrecognized request for : {requestType}");
                    break;
            }

        }
        public static string FormatBytes(long bytes, int decimals = 2)
        {
            if (bytes == 0) return "0 Bytes";

            const int k = 1024;
            string[] units = { "'Bytes'", "'KB'", "'MB'", "'GB'", "'TB'", "'PB'", "'EB'", "'ZB'", "'YB'" };

            int i = Convert.ToInt32(Math.Floor(Math.Log(bytes) / Math.Log(k)));
            return string.Format("{0:F" + decimals + "} {1}", bytes / Math.Pow(k, i), units[i]);
        }
        private static async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, JObject header)
        {
            foreach (TcpClient connectedClient in connectedClients)
                if (connectedClient != client)
                    await SendJsonToClient(connectedClient, header);
        }
        private static async Task SendJsonToClient(TcpClient client, JObject data)
        {
            NetworkStream connectedStream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(data.ToString());
            var length = BitConverter.GetBytes(bytes.Length);

            // header defining length of message.
            await connectedStream.WriteAsync(length, 0, 4);
            await connectedStream.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
