using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VM.OS.Network.Server
{
    class Host
    {
        public int OPEN_PORT { get; internal set; } = 8080;

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
                        if (ipInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
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
        TcpListener SERVER;
        public async Task Open(int port)
        {
            this.OPEN_PORT = port;

            Running = true;

            List<TcpClient> CLIENTS = new();

            Notifications.Now($"SERVER:Server started on IP {GetLocalIPAddress().MapToIPv4()}::{OPEN_PORT}. Waiting for connections...");

            SERVER = new TcpListener(IPAddress.Any, OPEN_PORT);

            SERVER.Start();

            Server networkConfig = new Server(CLIENTS);

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

    public class Packet
    {
        public JObject Metadata;
        public byte[] Data = Array.Empty<byte>();
        public TcpClient Client = default!;
        public NetworkStream stream = default!;
        public Packet(JObject header, byte[] message, TcpClient client, NetworkStream stream)
        {
            this.Metadata = header;
            this.Data = message;
            this.Client = client;
            this.stream = stream;
        }
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
        private const int REQUEST_REPLY_CHANNEL = 6996;
        const int DOWNLOAD_REPLY_CHANNEL = 6997;
        public Dictionary<byte[], Func<Packet, Task<Packet>>> ServerTasks = new();
        string UPLOAD_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM_SERVER_DATA";
        public Dictionary<string, TcpClient> FileTransfersPending = new();
        public List<string> AvailableForDownload = new();
        public List<TcpClient> Clients;
        public Server(List<TcpClient> cLIENTS)
        {
            this.Clients = cLIENTS;

            if (!Directory.Exists(UPLOAD_DIR))
                Directory.CreateDirectory(UPLOAD_DIR);

            foreach (var item in Directory.EnumerateFileSystemEntries(UPLOAD_DIR))
            {
                AvailableForDownload.Add(item.Split('\\').Last());
            }

        }
        public Packet RecieveMessage(NetworkStream stream, TcpClient client)
        {

            // this header will indicate the size of the actual metadata
            byte[] header = new byte[4];

            // 4 byte header for i/o channels
            if (stream.Read(header, 0, 4) <= 0)
                return default;

            int metadataLength = BitConverter.ToInt32(header, 0);

            byte[] metaData = new byte[metadataLength];

            // metadata file, json object with infos and the actual data for the xfer
            if (stream.Read(metaData, 0, metadataLength) <= 0)
                return default;

            var metadata = ParseMetadata(metaData);

            // length of the data message, we don't use this now, but for doing fragmented transfers for larger files,
            // we'd want to know the full length of the incoming data for reconstruction.
            // we can decide on a fixed buffer length for an incoming file transfer and fragment it accordingly on client side.
            int messageLength = metadata.Value<int>("size");


            // sender channel
            int sender_ch = metadata.Value<int>("ch");
            // reply channel
            int reciever_ch = metadata.Value<int>("reply");

            // Base64 string represnetation of data
            string dataString = metadata.Value<string>("data") ?? "Server:Data not found! something has gone wrong with the client's json construction";


            // byte representation of data, original.
            var dataBytes = Convert.FromBase64String(dataString);

            // sizeof data.
            var bytesLength = dataBytes.Length;

            if (bytesLength <= 1000)
            {
                Notifications.Now($"SERVER:Received from client {client.GetHashCode()}: {sender_ch} to {reciever_ch} \"{dataString}\"");
            }
            else
            {
                Notifications.Now($"SERVER:Received {FormatBytes(bytesLength)} client {{{client.GetHashCode()}: {{{sender_ch}->{reciever_ch}}}}}");
            }

            return new(metadata, dataBytes, client, stream);
        }
        private JObject ParseMetadata(byte[] metaData)
        {
            try
            {
                return JObject.Parse(Encoding.UTF8.GetString(metaData));
            }
            catch
            {

            }
            return new JObject();
        }
        public async Task ConnectClientAsync(TcpListener server, List<TcpClient> connectedClients)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            connectedClients.Add(client);
            Notifications.Now($"SERVER:Client {client.GetHashCode()} connected ");
            _ = HandleClientCommunicationAsync(client, connectedClients);
        }
        private async Task HandleClientCommunicationAsync(TcpClient client, List<TcpClient> connectedClients)
        {
            NetworkStream stream = client.GetStream();
            try
            {
                while (true)
                {
                    Packet packet = RecieveMessage(stream, client);
                    await TryHandleMessages(packet, connectedClients);
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
            if (packet.Metadata.Value<string>("type") is string tTypeStr)
            {
                var transmissionType = Enum.Parse<TransmissionType>(tTypeStr);

                switch (transmissionType)
                {
                    case TransmissionType.Path:
                        HandlePathTransmission(packet);
                        break;
                    case TransmissionType.Data:
                        HandleDataTransmission(packet);
                        break;
                    case TransmissionType.Message:
                        await HandleMessageTransmission(packet, clients);
                        break;
                    case TransmissionType.Download:
                        await HandleDownloadRequest(packet);
                        break;
                    case TransmissionType.Request:
                        HandleRequest(Encoding.UTF8.GetString(packet.Data), packet);
                        break;
                }
            }
        }

        private async Task HandleDownloadRequest(Packet packet)
        {
            var file = Encoding.UTF8.GetString(packet.Data);
            if (AvailableForDownload.Contains(file))
            {
                await SendDataRecusive(file);

                var message = Encoding.UTF8.GetBytes("END_DOWNLOAD");
                var length = message.Length;

                JObject endPacket = JObject.Parse(PopulateJsonTemplate(length, message, TransmissionType.Download, DOWNLOAD_REPLY_CHANNEL, -1));
                await SendJsonToClient(packet.Client, endPacket);
            }

            async Task SendDataRecusive(string file)
            {
                string path = file;
                if (!file.Contains(UPLOAD_DIR))
                    path = UPLOAD_DIR + "\\" + file;

                file = file.Replace(UPLOAD_DIR + "\\", "");

                if (File.Exists(path))
                {
                    var fileName = Encoding.UTF8.GetBytes(file);
                    var metadata = PopulateJsonTemplate(fileName.Length, fileName, TransmissionType.Download, DOWNLOAD_REPLY_CHANNEL, -1);
                    await SendJsonToClient(packet.Client, JObject.Parse(metadata));

                    var fileContents = File.ReadAllBytes(path);
                    metadata = PopulateJsonTemplate(fileContents.Length, fileContents, TransmissionType.Download, DOWNLOAD_REPLY_CHANNEL, -1);
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

            }
        }

        private async Task HandleMessageTransmission(Packet packet, List<TcpClient> clients)
        {
            var bytes = Convert.FromBase64String(packet.Metadata.Value<string>("data"));
            var responseMetadata = JObject.Parse(PopulateJsonTemplate(bytes.Length, bytes, TransmissionType.Message, packet.Metadata.Value<int>("reply"), -1, false));
            await BroadcastMessage(clients, packet.Client, responseMetadata);
        }

        private void HandleDataTransmission(Packet packet)
        {
            string toRemove = "";
            foreach (var item in FileTransfersPending)
            {
                if (item.Value == packet.Client)
                {
                    if (packet.Metadata.Value<bool>("isDir"))
                    {
                        Directory.CreateDirectory(UPLOAD_DIR + "\\" + Encoding.UTF8.GetString(packet.Data));
                    }
                    else
                    {
                        string path = "";

                        if (item.Key.StartsWith('\\'))
                            path = item.Key.Remove(0, 1);

                        path = UPLOAD_DIR + "\\" + item.Key;

                        File.WriteAllBytes(path, packet.Data);
                    }
                    toRemove = item.Key;
                }
            }
            if (toRemove != "")
            {
                FileTransfersPending.Remove(toRemove);
            }
        }

        private void HandlePathTransmission(Packet packet)
        {
            if (Encoding.UTF8.GetString(packet.Data) is string Path)
            {
                // write the dir, or we wait for file data.
                if (packet.Metadata.Value<bool>("isDir"))
                {
                    Directory.CreateDirectory(UPLOAD_DIR + "\\" + Path);
                }
                else
                {
                    FileTransfersPending[Path] = packet.Client;
                }
            }
        }

        private string PopulateJsonTemplate(int dataSize, byte[] data, TransmissionType type, int ch, int reply, bool isDir = false)
        {
            var json = new
            {
                size = dataSize,
                data = Convert.ToBase64String(data),
                type = type.ToString(),
                ch = ch,
                reply = reply,
                isDir = isDir
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
                    var bytes = Encoding.UTF8.GetBytes(names);

                    JObject metadata = JObject.Parse(PopulateJsonTemplate(bytes.Length, bytes, TransmissionType.Request, REQUEST_REPLY_CHANNEL, -1, false));
                    await SendJsonToClient(packet.Client, metadata);

                    Notifications.Now($"SERVER:Responding with {names}");
                    break;

                default:
                    break;
            }

        }
        static string FormatBytes(long bytes, int decimals = 2)
        {
            if (bytes == 0) return "0 Bytes";

            const int k = 1024;
            string[] units = { "Bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int i = Convert.ToInt32(Math.Floor(Math.Log(bytes) / Math.Log(k)));
            return string.Format("{0:F" + decimals + "} {1}", bytes / Math.Pow(k, i), units[i]);
        }
        private async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, JObject header)
        {
            foreach (TcpClient connectedClient in connectedClients)
                if (connectedClient != client)
                    await Server.SendJsonToClient(connectedClient, header);
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
