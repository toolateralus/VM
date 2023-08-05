using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server;
const int port = 8080;
const int maxClients = 100;

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
    Console.WriteLine("Client connected");

    NetworkStream stream = client.GetStream();
    byte[] buffer = new byte[1024];
    int bytesRead;

    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        Console.WriteLine("Received: " + message);

        // todo: implement the appropriate methods and functionality once the fake network is established.

        // Broadcast the message to all connected clients
        byte[] broadcastBuffer = Encoding.ASCII.GetBytes(message);
        foreach (TcpClient connectedClient in server.Pending() ? new TcpClient[] { server.AcceptTcpClient() } : new TcpClient[] { })
        {
            if (connectedClient != client)
            {
                NetworkStream connectedStream = connectedClient.GetStream();
                await connectedStream.WriteAsync(broadcastBuffer, 0, broadcastBuffer.Length);
            }
        }
    }

    client.Close();
    Console.WriteLine("Client disconnected");
}
