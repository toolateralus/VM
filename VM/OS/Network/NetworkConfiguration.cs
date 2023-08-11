using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VM.OS.Network
{
    using System;
    using System.Net;
    using System.Windows;

    public class NetworkConfiguration
    {
        private TcpClient client;
        private NetworkStream stream;
        const int DEFAULT_PORT = 8080;
        public event Action<string>? OnMessageRecieved;
        public event Action<string>? OnNetworkException;
        public event Action<string>? OnNetworkDisconneted;
        public event Action<string>? OnNetworkConneted;
        public NetworkConfiguration()
        {
            IPAddress localIP = LANIPFetcher.GetLocalIPAddress();
            StartClient(localIP);

        }
        public void StartClient(IPAddress ip)
        {
            try
            {
                var ip_str = ip.ToString();
                client = new TcpClient(ip_str, DEFAULT_PORT);
                stream = client.GetStream();
                OnNetworkConneted?.Invoke($"Connected to {client.Client.AddressFamily}");
                Thread receiveThread = new Thread(ReceiveMessages);
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
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    OnMessageRecieved?.Invoke(message);
                }
            }
            catch (Exception e)
            {
                OnNetworkException?.Invoke(e.Message);
            }
            finally
            {
                // server disconnect
                stream.Close();
                client.Close();
                OnNetworkDisconneted?.Invoke("Disconnected");
            }
        }

        internal void InputChannel(object?[]? obj)
        {
            MessageBox.Show($"Begin listening on channel {obj[0]}");


        }

        internal void OutputChannel(object?[]? obj)
        {
            // "send", args order
            // outCh, replyCh, message (any)
            MessageBox.Show($"Sent message on channel {obj[0]}");
        }
    }
}
