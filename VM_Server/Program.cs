using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
        public byte[] Header = Array.Empty<byte>();
        public byte[] Data = Array.Empty<byte>();
        public TcpClient Client = default!;
        public NetworkStream stream = default!;

        public Packet(byte[] header, byte[] message, TcpClient client, NetworkStream stream)
        {
            this.Header = header;
            this.Data = message;
            this.Client = client;
            this.stream = stream;
        }
    }

    public class Server
    {
        public Dictionary<byte[], Func<Packet, Task<Packet>>> ServerTasks = new();
        public List<TcpClient> ActiveUploaders = new();
        public Dictionary<byte[], byte[]> UploadChannels = new();
        
        string UPLOAD_DIR = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VM_SERVER_DATA";

        public Server()
        {
            const string START = "START_UPLOAD";
            const string END = "END_UPLOAD";
            const string NEXTDIR = "NEXTDIR_UPLOAD";

            // we need to add a parser to our messages and 
            // parse our metadata out.
            const string METADATA = "::METADATA::";

            const string PATH = "PATH";
            const string DATA = "DATA";

            ServerTasks[Encoding.UTF8.GetBytes(START)] = HandleUpload;
            ServerTasks[Encoding.UTF8.GetBytes(END)] = HandleUpload;

            ServerTasks[Encoding.UTF8.GetBytes(NEXTDIR)] = HandleNextDir;
            ServerTasks[Encoding.UTF8.GetBytes(PATH)] = HandlePath;
            ServerTasks[Encoding.UTF8.GetBytes(DATA)] = TryUpload;
        }

        private Task<Packet> HandleNextDir(Packet arg)
        {
            if (UploadChannels.TryGetValue(arg.Header, out var ch))
            {
                if ()
            }
        }

        private Task<Packet> HandlePath(Packet arg)
        {
            throw new NotImplementedException();
        }

        public async Task<Packet> HandleUpload(Packet packet)
        {
            if (ActiveUploaders.Contains(packet.Client))
            {
                ActiveUploaders.Remove(packet.Client);

                File.WriteAllBytes(UPLOAD_DIR, UploadChannels[packet.Header]);

                UploadChannels.Remove(packet.Header);
            }
            else
            {
                // start upload
                ActiveUploaders.Add(packet.Client);
                UploadChannels.Add(packet.Header, Array.Empty<byte>());
            }
            return default!;
        }
        public async Task<Packet> TryHandleMessage(Packet packet)
        {
            Packet reply_transmission = null;

            await Task.Run(async () => { 
            
                if (ServerTasks.TryGetValue(packet.Data, out var handler))
                {
                    reply_transmission = await handler.Invoke(packet);
                }
            });

            return reply_transmission;
        }
        public Packet RecieveMessage(NetworkStream stream, TcpClient client)
        {
            byte[] header = new byte[4]; 

            // 4 byte header for i/o channels
            if (stream.Read(header, 0, 4) <= 0)
                return default;

            int messageLength = BitConverter.ToInt32(header, 0);

            byte[] dataBytes = new byte[messageLength];

            if (stream.Read(dataBytes, 0, messageLength) <= 0)
                return default;

            var bytesLength = dataBytes.Length;

            int reciever = BitConverter.ToInt32(dataBytes, bytesLength - 8);
            int sender = BitConverter.ToInt32(dataBytes, bytesLength - 4);

            if (bytesLength <= 1000)
            {
                string message = Encoding.ASCII.GetString(dataBytes, 0, bytesLength - 8);
                Console.WriteLine($"Received from client {client.GetHashCode()}: {sender} to {reciever} \"{message}\"");
            }
            else
            {
                Console.WriteLine($"Received from client {client.GetHashCode()}: {sender} to {reciever}, {FormatBytes(bytesLength)}");
            }

            return new(header, dataBytes, client, stream);
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

                    var messageResult = await TryHandleMessage(packet);



                    if (messageResult != default)
                    {
                        await BroadcastMessage(connectedClients, client, messageResult.Header, messageResult.Data);
                    }
                    else
                    {
                        await BroadcastMessage(connectedClients, client, packet.Header, packet.Data);
                    }
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
        private Task<Packet> TryUpload(Packet packet)
        {
           

            if (ActiveUploaders.Contains(packet.Client))
            {
                if (UploadChannels.TryGetValue(packet.Header, out var bytes))
                {
                    var newData = new byte[bytes.Length + packet.Data.Length];

                    Array.Copy(bytes, newData, bytes.Length);
                    Array.Copy(packet.Data, 0, newData, bytes.Length, packet.Data.Length);

                    UploadChannels[packet.Header] = newData;
                }
            }
            return null;
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
