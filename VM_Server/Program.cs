using System;
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

namespace ServerExample
{
    class Program
    {
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
        static async Task Main(string[] args)
        {
            int MY_PORT = 8080;

            Console.WriteLine("Enter a port ie. '8080' \n or send an empty message to run on 8080, or your default port which can be set in the src");

            int.TryParse(Console.ReadLine(), out MY_PORT);

            List<TcpClient> CLIENTS = new();

            Console.WriteLine($"Server started on IP {GetLocalIPAddress().MapToIPv4()}::{MY_PORT}. Waiting for connections...");

            TcpListener SERVER = new TcpListener(IPAddress.Any, MY_PORT);

            SERVER.Start();

            Server networkConfig = new Server();

            while (true)
            {
                await networkConfig.ConnectClientAsync(SERVER, CLIENTS);
            }
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

    public class Server
    {
        public Dictionary<byte[], Func<Packet, Task<Packet>>> ServerTasks = new();
        // sender, path, data
        
        string UPLOAD_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM_SERVER_DATA";

        public enum TransmissionType
        {
            Path,
            Data,
            Message
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
            int reciever_ch  = metadata.Value<int>("reply");

            // Base64 string represnetation of data
            string dataString = metadata.Value<string>("data") ?? "Data not found! something has gone wrong with the client's json construction";

            // byte representation of data, original.
            var dataBytes = Convert.FromBase64String(dataString);

            // sizeof data.
            var bytesLength = dataBytes.Length;

            if (bytesLength <= 1000)
            {
                Console.WriteLine($"Received from client {client.GetHashCode()}: {sender_ch} to {reciever_ch} \n \"{dataString}\"\n");
            }
            else
            {
                Console.WriteLine($"Received from client {client.GetHashCode()}: {sender_ch} to {reciever_ch}, {FormatBytes(bytesLength)}");
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
            Console.WriteLine($"Client {client.GetHashCode()} connected ");
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
                    await BroadcastMessage(connectedClients, client, packet.Header, packet.Data);
                }
            }
            catch (Exception ex)
            {
                // Handle the exception, log the error, etc.
            }
            finally
            {
                client.Close();
                connectedClients.Remove(client);
                Console.WriteLine($"Client {client.GetHashCode()} disconnected");
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
        private async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, byte[] header, byte[] broadcastBuffer)
        {
            foreach (TcpClient connectedClient in connectedClients)
            {
                if (connectedClient != client)
                {
                    NetworkStream connectedStream = connectedClient.GetStream();
                    await connectedStream.WriteAsync(header, 0, header.Length);
                    await connectedStream.WriteAsync(broadcastBuffer, 0, broadcastBuffer.Length);
                }
            }
        }
    }
}
