namespace VM.OPSYS
{
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
}
