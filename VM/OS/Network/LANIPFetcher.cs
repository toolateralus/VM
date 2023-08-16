namespace VM.Network
{
    using System.Net;
    using System.Net.NetworkInformation;

        public static class LANIPFetcher
        {
            public static IPAddress GetLocalIPAddress()
            {
                IPAddress localIP = null;

                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                        (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                         networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    {
                        IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                        foreach (UnicastIPAddressInformation ipInformation in ipProperties.UnicastAddresses)
                        {
                            if (ipInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(ipInformation.Address))
                            {
                                localIP = ipInformation.Address;
                                break;
                            }
                        }

                        if (localIP != null)
                        {
                            break;
                        }
                    }
                }

                return localIP;
        }

    }
}
