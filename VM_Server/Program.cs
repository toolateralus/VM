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
                byte[] buffer = new byte[1024];
                int bytesRead;

                try
                {
                    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                    {
                        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        
                        Console.WriteLine($"Received from client {client.GetHashCode()}: {message}");

                        if (messageHandlers.TryGetValue(message, out var handler))
                        {
                            await handler.Invoke(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                        }

                        // Handle other message types or logic here

                        byte[] broadcastBuffer = Encoding.ASCII.GetBytes(message);
                        await BroadcastMessage(connectedClients, client, broadcastBuffer);
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

        private async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, byte[] broadcastBuffer)
        {
            foreach (TcpClient connectedClient in connectedClients)
            {
                if (connectedClient != client)
                {
                    NetworkStream connectedStream = connectedClient.GetStream();
                    await connectedStream.WriteAsync(broadcastBuffer, 0, broadcastBuffer.Length);
                }
            }
        }
    }
}
