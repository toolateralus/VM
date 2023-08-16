using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VM.Network
{
    using CefNet.WinApi;
    using Microsoft.ClearScript.JavaScript;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Buffers.Text;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Shapes;
    using VM.GUI;
    using VM.Network.Server;
    using static Server.Server;
    public class NetworkConfiguration
    {
        private TcpClient client;
        private NetworkStream stream;
        public const int DEFAULT_PORT = 8080;
        public static string LAST_KNOWN_SERVER_IP => "192.168.0.138";

        public void StopHosting(object?[] args)
        {
            host?.Dispose();
        }

        public static IPAddress SERVER_IP => IPAddress.Parse(LAST_KNOWN_SERVER_IP);

        public static int LAST_KNOWN_SERVER_PORT { get; internal set; }

        public Action<byte[]>? OnMessageRecieved;
        public Thread receiveThread;
        private Host? host = null;

        public NetworkConfiguration(Computer computer)
        {
            computer.OnShutdown += StopClient;
            if (computer?.Config?.Value<bool>("ALWAYS_CONNECT") is bool connect && connect)
            {
                if (computer?.Config?.Value<string>("DEFAULT_SERVER_IP") is string _IP && IPAddress.Parse(_IP) is IPAddress ip)
                {
                    StartClient(ip);
                }
                else
                {
                    StartClient(SERVER_IP);
                }
            }
        }
        public void StartClient(IPAddress ip)
        {
            Task.Run(() =>
            {
                try
                {
                    var ip_str = ip.ToString();
                    // for when we support any port
                    var port = DEFAULT_PORT;
                    LAST_KNOWN_SERVER_PORT = port;

                    client = new TcpClient(ip_str, port);
                    stream = client.GetStream();

                    receiveThread = new Thread(ReceiveMessages);
                    receiveThread.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error connecting to the server: " + e.Message + Environment.NewLine + e.InnerException);
                }
            });
        }
        public void ReceiveMessages()
        {
            try
            {
                while (IsConnected())
                {
                    // blocking call to server that actually reads data.
                    var packet = RecieveMessage(stream, client, false);
                    int messageLength = packet.Metadata.Value<int>("size");
                    int sender_ch = packet.Metadata.Value<int>("ch");
                    int reciever_ch = packet.Metadata.Value<int>("reply");

                    var path = packet.Metadata.Value<string>("path");
                    if (path is null)
                    {
                        Runtime.Broadcast(sender_ch, reciever_ch, packet.Data);
                    }
                    else
                    {
                        Runtime.Broadcast(sender_ch, reciever_ch, packet.Metadata);
                    }
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
            finally
            {
                // server disconnect
                stream?.Close();
                client?.Close();
            }
        }
        
      
        internal void OnSendMessage(byte[] dataBytes, TransmissionType type, int ch, int reply, bool isDir = false)
        {
            var metadata = Server.Server.ToJson(dataBytes.Length, dataBytes, type, ch, reply, isDir);

            byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
            byte[] lengthBytes = BitConverter.GetBytes(metadataBytes.Length);

            if (stream != null && stream.CanWrite)
            {
                // header to indicate Metadata length.
                stream.Write(lengthBytes, 0, 4);
                // metadata for file transfer, contains the data for the transfer as well.
                stream.Write(metadataBytes, 0, metadataBytes.Length);
            }
        }
        internal void StopClient()
        {
            client?.Close();
            stream?.Close();
            Task.Run(()=>receiveThread?.Join());
        }
        internal bool IsConnected()
        {
            return client?.Connected ?? false;
        }

        internal async Task<bool> StartHosting(int port)
        {
            if (host != null && host.Running)
            {
                return false;
            }

            host ??= new();

            await host.Open(port);

            return host.Running;
        }

        internal object GetIPPortString()
        {
            return $"{LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{host?.OPEN_PORT}";
        }
    }
}
