using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VM.JS;
using static VM.Network.Server.Server;
using VM.Network;
using VM.Network.Server;

namespace VM.Network
{
    public class NetworkConfiguration : IDisposable
    {
        private Host? host = null;
        
        private TcpClient? client;
        
        public Thread? receiveThread;
        
        private NetworkStream? stream;

        public const int DEFAULT_PORT = 8080;
        
        public Action<byte[]>? OnMessageRecieved;

        public static string LAST_KNOWN_SERVER_IP => "192.168.0.138";
        public static IPAddress SERVER_IP => IPAddress.Parse(LAST_KNOWN_SERVER_IP);
        public static int LAST_KNOWN_SERVER_PORT { get; internal set; }

        public NetworkConfiguration(Computer computer)
        {
            if (computer?.Config?.Value<bool>("ALWAYS_CONNECT") is bool connect && connect)
            {
                if (computer?.Config?.Value<string>("DEFAULT_SERVER_IP") is string _IP && IPAddress.Parse(_IP) is IPAddress ip)
                {
                    _ = StartClient(ip);
                }
                else
                {
                    _ = StartClient(SERVER_IP);
                }
            }
        }
        
        public async Task StartClient(IPAddress ip)
        {
            await Task.Run(() =>
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
        public static Dictionary<int, Queue<(object? val, int replyCh)>> NetworkEvents = new();
        internal static void Broadcast(int outCh, int inCh, object? msg)
        {
            if (!NetworkEvents.TryGetValue(outCh, out _))
            {
                NetworkEvents.Add(outCh, new());
            }
            NetworkEvents[outCh].Enqueue((msg, inCh));

            // foreach (var computer in Computer.Computers)
            // {
            //     foreach (var userWindow in computer.Key.USER_WINDOW_INSTANCES)
            //     {
            //         if (userWindow.Value.JavaScriptEngine.EventHandlers == null)
            //             continue;

            //         foreach (var eventHandler in userWindow.Value.JavaScriptEngine.EventHandlers)
            //         {
            //             if (eventHandler is NetworkEventHandler networkEventHandler)
            //                 networkEventHandler.InvokeEvent(outCh, inCh, msg);
            //         }
            //     }
            // }
        }
        public static (object? value, int reply) PullEvent(int channel, Computer computer)
        {
            Queue<(object? val, int replyCh)>? queue;

            const int timeout = int.MaxValue;
            int it = 0;

            while ((!NetworkEvents.TryGetValue(channel, out queue) || queue is null || queue.Count == 0) && !computer.Disposing)
            {
                if (it < timeout)
                    it++;
                else break;

                Thread.Sleep(1);
            }

            var val = queue?.Dequeue();

            if (queue?.Count == 0)
                NetworkEvents.Remove(channel);

            return val ?? default;
        }
        public static async Task<(object? value, int reply)> PullEventAsync(int channel, VM.Computer computer, int timeout = 20_000, [CallerMemberName] string callerName = "unknown")
        {
            Queue<(object? val, int replyCh)> queue;
            var timeoutTask = Task.Delay(timeout);

            while (!NetworkEvents.TryGetValue(channel, out queue) || queue is null || queue.Count == 0 && !computer.Disposing && computer.Network.IsConnected())
            {
                var completedTask = await Task.WhenAny(Task.Delay(1), timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Notifications.Now($"timed out fetching from {callerName} event on channel {channel}");
                    return (null, -1);
                }
            }

            var val = queue?.Dequeue();

            if (queue?.Count == 0)
                NetworkEvents.Remove(channel);


            return val ?? default;
        }

        internal void StopClient()
        {
            Dispose();
        }

        public void Dispose()
        {
            client?.Close();
            stream?.Close();
            Task.Run(() => receiveThread?.Join());
            Notifications.Now($"Disconnected from {LAST_KNOWN_SERVER_IP}::{LAST_KNOWN_SERVER_PORT}");
        }

        public void StopHosting(object[]? args)
        {
            host?.Dispose();
        }

        internal bool IsConnected()
        {
            return stream != null && client != null && client.Connected;
        }
        internal object GetIPPortString()
        {
            return $"{LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{host?.OPEN_PORT}";
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
                    var data = packet.Metadata.Value<string>("data") ?? "";
                    packet.Metadata["data"] = Encoding.UTF8.GetString(Convert.FromBase64String(data));

                    if (path is null)
                    {
                        NetworkConfiguration.Broadcast(sender_ch, reciever_ch, packet.Metadata.ToString());
                    }
                    else
                    {
                        // DOWNLOADS // FILE TRANSFER
                        NetworkConfiguration.Broadcast(sender_ch, reciever_ch, packet.Metadata);
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
    }
}
