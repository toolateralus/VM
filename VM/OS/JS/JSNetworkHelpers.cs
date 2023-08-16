﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using VM.GUI;
using VM.Network;
using VM.Network.Server;
using VM;

namespace VM.JS
{
    public delegate void TransmissionStream(byte[] data, TransmissionType type, int outCh, int replyCh, bool isDir);
    public class JSNetworkHelpers
    {
        public event TransmissionStream OnTransmit;
        Computer Computer;
        private int size;

        public JSNetworkHelpers(Computer computer, TransmissionStream transmissionStream)
        {
            OnTransmit = transmissionStream;
            Computer = computer;
        }
        
        public string? ip()
        {
            return NetworkConfiguration.LAST_KNOWN_SERVER_IP;
        }
        public void connect(object? ip)
        {
            IPAddress targetIP = null;

            if (ip is string IPString && IPAddress.TryParse(IPString, out targetIP))
            {
                ConnectToIP(targetIP, IPString);
            }
            else if (Computer.Config?.Value<string>("DEFAULT_SERVER_IP") is string defaultIP && IPAddress.TryParse(defaultIP, out targetIP))
            {
                ConnectToIP(targetIP, defaultIP);
            }
        }
        private void ConnectToIP(IPAddress targetIP, string ipString)
        {
            Computer.JavaScriptEngine.InteropModule.print($"Trying to connect to: {ipString}");

            Computer.Network.StopClient();

            try
            {
                Computer.Network.StartClient(targetIP);

                if (Computer.Network.IsConnected())
                {
                    Computer.JavaScriptEngine.InteropModule.print($"Successfully connected to {ipString}.");
                }
                else
                {
                    Computer.JavaScriptEngine.InteropModule.print($"Failed to connect to {ipString} :: Not found.");
                }
            }
            catch (Exception e)
            {
                Computer.JavaScriptEngine.InteropModule.print($"Failed to connect to {ipString} :: {e.Message}");
            }
        }
        public async void upload(string path)
        {
            var isDir = false;

            if (Runtime.GetResourcePath(path) is not string AbsPath)
            {
                // non existent file.
                return;
            }

            isDir = Directory.Exists(AbsPath) && !File.Exists(AbsPath);

            if (isDir)
            {
                string root_dir = AbsPath.Split('\\').Last();
                byte[] bytePath = Encoding.UTF8.GetBytes(root_dir);

                OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);

                foreach (var item in Directory.GetFileSystemEntries(AbsPath))
                {
                    string strPath = item.Replace(AbsPath, root_dir);
                    bytePath = Encoding.UTF8.GetBytes(strPath);
                    try
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(item);

                        if (Directory.Exists(item))
                        {
                            OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, true);
                            Notifications.Now($"Uploading directory item: from {strPath}::{item}");
                        }
                        else if (File.Exists(item))
                        {
                            OnTransmit?.Invoke(bytePath, TransmissionType.Path, -1, -1, false);
                            OnTransmit?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);
                            Notifications.Now("Uploading path: " + item);
                        }
                    }
                    catch (Exception ex) when (ex is not UnauthorizedAccessException)
                    {
                        Console.WriteLine($"Caught exception: {ex.Message}");
                    }
                }
            }
            else
            {
                byte[] pathBytes = Encoding.UTF8.GetBytes(path);
                byte[] fileBytes = await File.ReadAllBytesAsync(AbsPath.Split('\\').Last());

                OnTransmit?.Invoke(pathBytes, TransmissionType.Path, -1, -1, false);
                OnTransmit?.Invoke(fileBytes, TransmissionType.Data, -1, -1, false);

                Notifications.Now("Uploading path: " + path);
            }
        }
        public async void check_for_downloadable_content()
        {
            OnTransmit?.Invoke(Encoding.UTF8.GetBytes("GET_DOWNLOADS"), TransmissionType.Request, -1, Server.REQUEST_REPLY_CHANNEL, false);
            var response = await Runtime.PullEvent(Server.REQUEST_REPLY_CHANNEL, Computer);
            var stringResponse = Encoding.UTF8.GetString(response.value as byte[] ?? Encoding.UTF8.GetBytes("No data found"));
            Notifications.Now(stringResponse);
            return;
        }
        public async void download(string path)
        {

            if (!Computer.Network.IsConnected())
            {
                Notifications.Now("Not connected to network");
                return;
            }

            Notifications.Now($"Downloading {path}..");

            var root = Computer.FS_ROOT + "\\downloads";

            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            OnTransmit?.Invoke(Encoding.UTF8.GetBytes(path), TransmissionType.Download, 0, Server.DOWNLOAD_REPLY_CHANNEL, false);

            while (Computer.Network.IsConnected())
            {
                (object? value, int reply) = await Runtime.PullEvent(Server.DOWNLOAD_REPLY_CHANNEL, Computer);
                string pathString = null;

                if (value is not JObject metadata)
                {
                    if (value is byte[] bytes && Encoding.UTF8.GetString(bytes) is string dataStr)
                    {
                        switch (dataStr)
                        {
                            case "END_DOWNLOAD":
                                Notifications.Now($"{{{Server.FormatBytes(size)}}} downloads\\{path} downloaded.. run  the <install '{path}' to install it.");
                                return;
                            case "FAILED_DOWNLOAD":
                                Notifications.Now($"Download failed for {path}");
                                return;
                        }
                    }
                    Notifications.Now($"Invalid data gotten from server for {path}");
                    return;
                }

                if (metadata.Value<string>("data") is not string dataString || Convert.FromBase64String(dataString) is not byte[] dataBytes)
                {
                    Notifications.Now($"Invalid data for {path}");
                    return;
                }

                if (Convert.FromBase64String(metadata.Value<string>("path")) is not byte[] pathBytes)
                {
                    Notifications.Now($"Invalid path for {path}");
                    return;
                }

                pathString = Encoding.UTF8.GetString(pathBytes);


                var fullPath = Path.Combine(root, pathString);

                var directoryPath = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllBytes(fullPath, dataBytes);
                size += dataBytes.Length;

                await Task.Delay(1);
            }
            Notifications.Now("Not connected to network, or download failed");
        }
      
        public bool IsConnected => Computer.Network.IsConnected();
        public void send(params object?[]? parameters)
        {
            int outCh, inCh;
            object? msg;
            byte[] outgoingData = null;

            if (parameters is not null && parameters.Length > 2)
            {
                msg = parameters[2];

                // Process and convert the message to byte array if necessary

                if (outgoingData != null)
                {
                    // Specify the appropriate channel and reply values
                    outCh = 0; // Specify the outgoing channel
                    inCh = 0;  // Specify the reply channel

                    OnTransmit?.Invoke(outgoingData, TransmissionType.Message, outCh, inCh,  false);
                    Runtime.Broadcast(outCh, inCh, Encoding.UTF8.GetString(outgoingData)); 
                }
            }
        }
        public object? recieve(params object?[]? parameters)
        {
            if (parameters != null && parameters.Length > 0 && parameters[0] is int ch) 
            {
                var TaskOutcome = Task.Run<(object? value, int reply)>(async () => await Runtime.PullEvent(ch, Computer));
                
                var val = TaskOutcome.Result.value;

                if (val is byte[] message)
                {
                    byte[] InChannel = BitConverter.GetBytes(ch);
                    byte[] ReplyChannel = BitConverter.GetBytes(TaskOutcome.Result.reply);

                    byte[] combinedBytes = new byte[message.Length + sizeof(int) + sizeof(int)];

                    Array.Copy(message, 0, combinedBytes, 0, message.Length);
                    Array.Copy(InChannel, 0, combinedBytes, message.Length, sizeof(int));
                    Array.Copy(InChannel, 0, combinedBytes, message.Length + sizeof(int), sizeof(int));

                }
                return val;
            }
            Notifications.Now("Insufficient arguments for a network connection");
            return null;
        }
    }
}
