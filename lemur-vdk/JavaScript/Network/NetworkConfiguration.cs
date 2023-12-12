using Lemur.JavaScript.Api;
using Lemur.Windowing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Lemur.JavaScript.Network.Server;

namespace Lemur.JavaScript.Network
{
    public class NetworkConfiguration
    {
        private Host? host;

        private TcpClient? client;

        private Thread? listenerThread;

        private NetworkStream? stream;

        public const int defaultPort = 8080;

        internal Action<byte[]>? OnMessagelistend;

        public static string ServerIP { get; set; } = "192.168.0.138";
        public static int ServerPort { get; set; }
        public static IPAddress Server => IPAddress.Parse(ServerIP);

        public static ConcurrentDictionary<int, Queue<(object? val, int replyCh)>> NetworkEvents = new();

        public NetworkConfiguration()
        {
            if (Computer.Current?.Config?.Value<bool>("ALWAYS_CONNECT") is bool connect && connect)
            {
                if (Computer.Current?.Config?.Value<string>("DEFAULT_SERVER_IP") is string ipString
                    && IPAddress.Parse(ipString) is IPAddress ip)
                    StartClient(ip);
                else
                    StartClient(Server);
            }
        }
        internal void StartClient(IPAddress ip, string? name = null)
        {
            try
            {
                var ip_str = ip.ToString();
                var port = defaultPort;
                ServerPort = port;

                client = new TcpClient(ip_str, port);
                stream = client.GetStream();
                name ??= client.GetHashCode().ToString(CultureInfo.CurrentCulture);
                listenerThread = new Thread(() => listenMessages(name));
                listenerThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error connecting to the server: " + e.Message + Environment.NewLine + e.InnerException);
            }
        }
        internal async Task<bool> StartHosting(int port)
        {
            if (host != null && host.Running)
                return false;

            host ??= new();

            await host.Open(port).ConfigureAwait(false);

            return host.Running;
        }

        internal static void Broadcast(int channel, int reply, object? msg)
        {
            if (!NetworkEvents.TryGetValue(channel, out _))
                NetworkEvents.TryAdd(channel, new());

            NetworkEvents[channel].Enqueue((msg, reply));

            foreach (var userWindow in Computer.ProcessClassTable.SelectMany(i => i.Value.Select(i => i)))
            {
                if (userWindow?.UI.Engine?.EventHandlers == null)
                    continue;

                foreach (var eventHandler in userWindow?.UI?.Engine.EventHandlers)
                    if (eventHandler is NetworkEvent networkEventHandler)
                        networkEventHandler.InvokeEvent(channel, reply, msg);
            }
        }
        public static (object? value, int reply) PullEvent(int channel, int timeout = 20_000, [CallerMemberName] string callerName = "unknown")
        {
            Queue<(object? val, int replyCh)>? queue = null;

            bool timedOut = false;

            Task.Run(async () =>
            {
                await Task.Delay(timeout);
                timedOut = true;
            });

            bool messageNotRecieved() => !NetworkEvents.TryGetValue(channel, out queue) || queue is null || queue.Count == 0;

            bool shouldWait() => !timedOut && !Computer.Current.disposing && Computer.Current.NetworkConfiguration.IsConnected();

            while (shouldWait() && messageNotRecieved())
            {/* ----------------------------------------------- */
                Thread.Sleep(16);
            }

            var val = queue?.Dequeue();

            if (queue?.Count == 0)
                NetworkEvents.TryRemove(channel, out _);

            return val ?? new("failed", -1);
        }
        public static async Task<(object? value, int reply)> PullEventAsync(int channel, int timeout = 20_000, [CallerMemberName] string callerName = "unknown")
        {
            Queue<(object? val, int replyCh)> queue;

            var timeoutTask = Task.Delay(timeout);

            CancellationTokenSource cts = new();

            Task<(object? value, int reply)?> loop = new(delegate
            {

                while (!NetworkEvents.TryGetValue(channel, out queue)
                        || queue is null
                        || queue.Count == 0
                        && !Computer.Current.disposing
                        && Computer.Current.NetworkConfiguration.IsConnected())
                {/* ----------------------------------------------- */
                    Task.Delay(16);
                }

                var val = queue?.Dequeue();

                if (queue?.Count == 0)
                    NetworkEvents.Remove(channel, out _);

                return val;
            }, cts);

            var result = await loop;

            // did time out
            if (await Task.WhenAny(loop, timeoutTask) == timeoutTask)
            {
                cts.Cancel();
                Notifications.Now("Network timed out while polling an event");
            }

            return result ?? ("Failed", -1);
        }
        internal void StopClient()
        {
            client?.Close();
            stream?.Close();
            listenerThread?.Join();
            Notifications.Now($"Disconnected from {ServerIP}::{ServerPort}");
        }

        public void StopHosting()
        {
            host?.Dispose();
        }
        internal bool IsConnected()
        {
            return stream != null && client != null && client.Connected;
        }
        internal object GetIPPortString()
        {
            return $"{LANIPFetcher.GetLocalIPAddress().MapToIPv4()}:{host?.openPort}";
        }
        public void listenMessages(string name)
        {
            try
            {
                while (IsConnected())
                {
                    // ! operator usage
                    // IsConnected() is a widely used way to validate that stream & client aren't null and are connected.
                    // They cannot be null
                    var packet = ListenForPacket(stream!, client!, name);

                    if (packet is null)
                    {
                        Notifications.Now($"Recieved a null packet from {name}");
                        return; 
                    }

                    int messageLength = packet.Metadata.Value<int>("size");
                    int channel = packet.Metadata.Value<int>("ch");
                    int reply = packet.Metadata.Value<int>("reply");
                    var path = packet.Metadata.Value<string>("path");
                    var data = packet.Metadata.Value<string>("data") ?? "";

                    // normal messages
                    // send the whole packet? or deconstruct for the user?
                    // this way implies you need to send formatted json in send message which is false, it formats your message for you.
                    if (path is null)
                        Broadcast(channel, reply, packet.Metadata.ToString());


                    // downloads // file transfer
                    else
                        Broadcast(channel, reply, packet.Metadata);
                }
            }
            catch (Exception e)
            {
                Notifications.Exception(e);
            }
            finally
            {
                stream?.Close();
                client?.Close();
            }
        }
        internal void OnSendMessage(string data, TransmissionType type, int ch, int reply, bool isDir = false)
        {
            var metadata = ToJson(data, type, ch, reply, isDir);

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
