namespace VM.OS.Network
{
    using System.Net;
    using System.Net.NetworkInformation;

    public static class LANIPFetcher
    {
        public static string GetLocalIPAddress()
        {
            string localIP = "";

            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                    foreach (UnicastIPAddressInformation ipInformation in ipProperties.UnicastAddresses)
                    {
                        if (ipInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ipInformation.Address))
                        {
                            localIP = ipInformation.Address.ToString();
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(localIP))
                    {
                        break;
                    }
                }
            }

            return localIP;
        }
    }
}
