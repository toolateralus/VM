using System.Net.Sockets;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Lemur.JS;
using static Lemur.Network.Server.Server;
using Lemur.Network;
using Lemur.GUI;
using Lemur.Network.Server;

namespace Lemur.Network
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
        public static Dictionary<int, Queue<(object? val, int replyCh)>> NetworkEvents = new();

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
        internal static void Broadcast(int outCh, int inCh, object? msg)
        {
            if (!NetworkEvents.TryGetValue(outCh, out _))
                NetworkEvents.Add(outCh, new());

            NetworkEvents[outCh].Enqueue((msg, inCh));

            foreach (var userWindow in Computer.Current.UserWindows)
            {
                if (userWindow.Value.JavaScriptEngine?.EventHandlers == null)
                    continue;

                foreach (var eventHandler in userWindow.Value.JavaScriptEngine.EventHandlers)
                    if (eventHandler is NetworkEvent networkEventHandler)
                        networkEventHandler.InvokeEvent(outCh, inCh, msg);
            }
        }
        public static async Task<(object? value, int reply)> PullEventAsync(int channel, Lemur.Computer computer, int timeout = 20_000, [CallerMemberName] string callerName = "unknown")
        {
            Queue<(object? val, int replyCh)> queue;

            var timeoutTask = Task.Delay(timeout);

            CancellationTokenSource cts = new(); 

            Task<(object ? value, int reply)?> loop = new(delegate {

                while (!NetworkEvents.TryGetValue(channel, out queue)
                        || queue is null
                        || (queue.Count == 0
                        && !computer.disposing
                        && computer.Network.IsConnected()))
                {/* ----------------------------------------------- */
                    Task.Delay(16);
                }

                var val = queue?.Dequeue();

                if (queue?.Count == 0)
                    NetworkEvents.Remove(channel);

                return val;
            }, cts);

            // did time out
            if (await Task.WhenAny(loop, timeoutTask) == timeoutTask)
            {
                cts.Cancel();
                Notifications.Now("Network timed out while polling an event");
            }

            return loop.Result ?? default;
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
        public void StopHosting(object?[] args)
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
                    // ! operator usage
                    // IsConnected() is a widely used way to validate that stream & client aren't null and are connected.
                    // They cannot be null
                    var packet = RecieveMessage(stream!, client!, false);

                    int messageLength = packet.Metadata.Value<int>("size");
                    int sender_ch = packet.Metadata.Value<int>("ch");
                    int reciever_ch = packet.Metadata.Value<int>("reply");
                    var path = packet.Metadata.Value<string>("path");
                    var data = packet.Metadata.Value<string>("data") ?? "";
                    packet.Metadata["data"] = Encoding.UTF8.GetString(Convert.FromBase64String(data));

                    // normal messages
                    // send the whole packet? or deconstruct for the user?
                    // this way implies you need to send formatted json in send message which is false, it formats your message for you.
                    if (path is null)
                        Broadcast(sender_ch, reciever_ch, packet.Metadata.ToString());
                        

                    // downloads // file transfer
                    else
                        Broadcast(sender_ch, reciever_ch, packet.Metadata);
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
        internal void OnSendMessage(byte[] dataBytes, TransmissionType type, int ch, int reply, bool isDir = false)
        {
            var metadata = ToJson(dataBytes.Length, dataBytes, type, ch, reply, isDir);

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
