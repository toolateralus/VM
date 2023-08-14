using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace VM.OS.Network
{
    using CefNet.WinApi;
    using System;
    using System.Buffers.Text;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Shapes;
    using VM.GUI;

    public class NetworkConfiguration
    {
        private TcpClient client;
        private NetworkStream stream;
        const int DEFAULT_PORT = 8080;
        public static string LAST_KNOWN_SERVER_IP => "192.168.0.141";
        public static IPAddress SERVER_IP = IPAddress.Parse(LAST_KNOWN_SERVER_IP);
        public Action<byte[]>? OnMessageRecieved;
        public Thread receiveThread;

        const string START = "START_UPLOAD";
        const string END = "END_UPLOAD";
        const string NEXTDIR = "NEXTDIR_UPLOAD";
        const string METADATA = "::METADATA::";
        const string PATH = "PATH";
        const string DATA = "DATA";

        static byte[] PATH_MESSAGE = UPLOADING_PATH();
        static byte[] DATA_MESSAGE = UPLOADING_DATA();
        static byte[] UPLOADING_DATA()
        {
            return Encoding.UTF8.GetBytes(DATA);
        }
        static byte[] UPLOADING_PATH()
        {
            return Encoding.UTF8.GetBytes(DATA);
        }
        static byte[] UPLOAD_START_MESSAGE(string path, bool isDir)
        {
            return Encoding.UTF8.GetBytes($"{START}{METADATA}{{path{path},isDir:{isDir}}}");
        }
        static byte[] UPLOAD_END_MESSAGE(string path)
        {
            return Encoding.UTF8.GetBytes($"{END}{METADATA}{{path:{path}}}");
        }
        static byte[] UPLOAD_NEXT_DIR_MESSAGE(string path, string dir)
        {
            return Encoding.UTF8.GetBytes($"{NEXTDIR}{METADATA}{{path:{path},dir:{dir}}}");
        }

        public NetworkConfiguration(Computer computer)
        {
            computer.OnShutdown += TryHaltCurrentConnection;
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
            Task.Run(() =>
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
                    Console.WriteLine("Error connecting to the server: " + e.Message + Environment.NewLine + e.InnerException);
                }
            });
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
        internal void OnUploadFile(byte[] path)
        {

            string inputPath = Encoding.UTF8.GetString(path);
            byte[] endMsg = UPLOAD_END_MESSAGE(inputPath);

            if (Runtime.GetResourcePath(inputPath) is string AbsPath)
            {
                var isDir = Directory.Exists(AbsPath);
                var isFile = File.Exists(AbsPath);

                var msg = UPLOAD_START_MESSAGE(inputPath, isDir);
                stream.Write(msg, 0, msg.Length);

                if (isDir)
                {
                    UploadDirectories(path, AbsPath);
                }
                if (isFile)
                {
                    var file = File.ReadAllBytes(AbsPath);
                    UploadFile(file, path);
                }
            }

            // close uploading
            stream.Write(endMsg, 0, endMsg.Length);
        }

        private void UploadDirectories(byte[] path, string AbsPath)
        {
            var entries = Directory.GetFileSystemEntries(AbsPath);

            foreach (var entry in entries)
            {
                if (Directory.Exists(entry))
                {
                    var message = UPLOAD_NEXT_DIR_MESSAGE(AbsPath, entry);
                    stream.Write(message, 0, message.Length);
                }
                if (File.Exists(entry))
                {
                    var file = File.ReadAllBytes(entry);
                    UploadFile(file, path);
                }
            }
        }

        private void UploadFile(byte[] data, byte[] path)
        {
            if (stream != null && stream.CanWrite)
            {
                stream.Write(PATH_MESSAGE, 0, PATH_MESSAGE.Length);
                stream.Write(path, 0, path.Length);
                stream.Write(PATH_MESSAGE, 0, PATH_MESSAGE.Length);
                stream.Write(data, 0, data.Length);
            }
        }

        

        internal void OnSendMessage(byte[] dataBytes)
        {
            int messageLength = dataBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(messageLength);

            if (stream != null && stream.CanWrite)
            {
                stream.Write(lengthBytes, 0, 4);
                stream.Write(dataBytes, 0, dataBytes.Length);
            }

        }
        internal void TryHaltCurrentConnection()
        {
            client?.Close();
            stream?.Close();
            Task.Run(()=>receiveThread?.Join());
        }

        internal bool IsConnected()
        {
            return client.Connected;
        }
    }
}
