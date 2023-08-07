using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server;
const int port = 8080;
const int maxClients = 100;
List<TcpClient> connectedClients = new();

Console.WriteLine("Server started. Waiting for connections...");
server = new TcpListener(IPAddress.Any, port);
server.Start();


while (true)
{
    await HandleClientAsync();
}

async Task HandleClientAsync()
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


static async Task BroadcastMessage(List<TcpClient> connectedClients, TcpClient client, byte[] broadcastBuffer)
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