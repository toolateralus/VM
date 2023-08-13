using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VM.OS.Network
{
    using System;
    using System.Buffers.Text;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;

    public class NetworkConfiguration
    {
        private TcpClient client;
        private NetworkStream stream;

        const int DEFAULT_PORT = 8080;
        public static string LAST_KNOWN_SERVER_IP => "192.168.0.138";

        public static IPAddress SERVER_IP = IPAddress.Parse(LAST_KNOWN_SERVER_IP);

        public Action<byte[]>? OnMessageRecieved;

        public Thread receiveThread;

        public NetworkConfiguration(Computer computer, Action onDisconnect)
        {

            onDisconnect += TryHaltCurrentConnection;
            if (computer?.OS?.Config?.Value<bool>("ALWAYS_CONNECT") is bool connect && connect)
            {
                if (computer?.OS?.Config?.Value<string>("DEFAULT_SERVER_IP") is string _IP && IPAddress.Parse(_IP) is IPAddress ip)
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
            try
            {
                var ip_str = ip.ToString();
                client = new TcpClient(ip_str, DEFAULT_PORT);
                stream = client.GetStream();
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error connecting to the server: " + e.Message);
            }
        }

        public void ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    byte[] header = new byte[4]; // Assuming a 4-byte header size

                    // Read the message length
                    if (stream?.Read(header, 0, 4) <= 0)
                        break;
                    int messageLength = BitConverter.ToInt32(header, 0);

                    // Read the message content
                    byte[] dataBytes = new byte[messageLength];
                    if (stream?.Read(dataBytes, 0, messageLength) <= 0)
                        break;
                    OnMessageRecieved?.Invoke(dataBytes);
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                // server disconnect
                stream?.Close();
                client?.Close();
            }
        }


        internal void OnSendMessage(byte[] dataBytes)
        {
            MessageBox.Show($"Sent message on channel {dataBytes[0]}");
            int messageLength = dataBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(messageLength);
            stream?.Write(lengthBytes, 0, 4); // Assuming a 4-byte header size
            stream?.Write(dataBytes, 0, dataBytes.Length);
        }

        internal void TryHaltCurrentConnection()
        {
            client?.Close();
            stream?.Close();
            Task.Run(()=>receiveThread?.Join());
        }
    }
}
