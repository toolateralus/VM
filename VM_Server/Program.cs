using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const int port = 8080;
            List<TcpClient> connectedClients = new();

            Console.WriteLine("Server started. Waiting for connections...");
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();

            NetworkConfiguration networkConfig = new NetworkConfiguration();

            while (true)
            {
                await networkConfig.HandleClientAsync(server, connectedClients);
            }
        }
    }

    public class NetworkConfiguration
    {
        const int DEFAULT_PORT = 8080;
        Dictionary<string, Func<string, Task>> messageHandlers = new();

        public NetworkConfiguration()
        {
            messageHandlers["SERVER_DISCOVERY"] = HandleDiscoveryMessage;
        }

        private async Task HandleDiscoveryMessage(string ipAddress)
        {
            await SendResponse(ipAddress, "SERVER_DISCOVERY_RESPONSE");
        }

        private async Task SendResponse(string ipAddress, string responseMessage)
        {
            byte[] responseBuffer = Encoding.ASCII.GetBytes(responseMessage);

            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    await tcpClient.ConnectAsync(ipAddress, DEFAULT_PORT);
                    NetworkStream stream = tcpClient.GetStream();
                    await stream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                }
                catch (Exception)
                {
                    // Handle connection errors if needed
                }
            }
        }

        public async Task HandleClientAsync(TcpListener server, List<TcpClient> connectedClients)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            connectedClients.Add(client);
            Console.WriteLine($"Client {client.GetHashCode()} connected ");

            _ = Task.Run(async () =>
            {
                NetworkStream stream = client.GetStream();
                try
                {
                    while (true)
                    {
                        byte[] header = new byte[4]; // Assuming a 4-byte header size

                        // Read the message length
                        if (stream.Read(header, 0, 4) <= 0)
                            break;
                        int messageLength = BitConverter.ToInt32(header, 0);

                        // Read the message content
                        byte[] dataBytes = new byte[messageLength];
                        if (stream.Read(dataBytes, 0, messageLength) <= 0)
                            break;

                        var bytesLength = dataBytes.Length;
                        int reciever = BitConverter.ToInt32(dataBytes, bytesLength - 8);
                        int sender = BitConverter.ToInt32(dataBytes, bytesLength - 4);
                        string message = Encoding.ASCII.GetString(dataBytes, 0, bytesLength - 8);

                        Console.WriteLine($"Received from client {client.GetHashCode()}: {sender} to {reciever} \"{message}\"");

                        if (messageHandlers.TryGetValue(message, out var handler))
                        {
                            await handler.Invoke(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                        }

                        // Handle other message types or logic here

                        await BroadcastMessage(connectedClients, client, dataBytes, header);
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
            });
        }

        private async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, byte[] broadcastBuffer, byte[] header)
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
