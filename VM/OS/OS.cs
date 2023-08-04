using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using VM.GUI;
using VM.OPSYS.JS;

namespace VM.OPSYS
{
    using System;
    using System.Net;
    using System.Net.NetworkInformation;

    public static class LANIPFetcher
    {
        public static string GetLocalIPAddress()
        {
            string localIP = "";

            // Get all network interfaces on the machine
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                // Ignore loopback and non-operational interfaces
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                    foreach (UnicastIPAddressInformation ipInformation in ipProperties.UnicastAddresses)
                    {
                        // Check if the IP address is IPv4 and not a loopback address
                        if (ipInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ipInformation.Address))
                        {
                            localIP = ipInformation.Address.ToString();
                            break;
                        }
                    }

                    // If we found the local IP address, break the loop
                    if (!string.IsNullOrEmpty(localIP))
                    {
                        break;
                    }
                }
            }

            return localIP;
        }
    }
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
            string localIP = LANIPFetcher.GetLocalIPAddress();
            StartClient(localIP);

        }
        private void StartClient(string ip)
        {
            try
            {
                client = new TcpClient(ip, DEFAULT_PORT);
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

        private void ReceiveMessages()
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
                Console.WriteLine("Disconnected from the server.");
            }
        }
    }

    public class Computer
    {
      
        public Computer(uint id)
        {
            OS = new(id, this);
        }
        public uint ID() => OS.ID;

        public OS OS;

        internal void Exit(int exitCode)
        {
            MainWindow.GetPCWindow(this).Close();

            if (MainWindow.Computers.Count > 0 && exitCode != 0)
            {
                Notifications.Now($"Computer {ID()} has exited, most likely due to an error. code:{exitCode}");
            }
        }
        

    }

    public class OS
    {
        public FileSystem FS;
        public JavaScriptEngine JavaScriptEngine;

        public readonly uint ID;
        
        public readonly string FS_ROOT;
        public readonly string PROJECT_ROOT;

        public FontFamily SystemFont { get; internal set; } = new FontFamily("Consolas");

        public OS(uint id, Computer pc)
        {
            var EXE_DIR = Directory.GetCurrentDirectory();
            PROJECT_ROOT = Path.GetFullPath(Path.Combine(EXE_DIR, @"..\..\.."));
            FS_ROOT = $"{PROJECT_ROOT}\\computer{id}";
            FS = new(FS_ROOT, pc);
            JavaScriptEngine = new(PROJECT_ROOT);
            JavaScriptEngine.Execute($"OS.id = {id}");
            JavaScriptEngine.InteropModule.OnComputerExit += pc.Exit;
        }
    }
}
