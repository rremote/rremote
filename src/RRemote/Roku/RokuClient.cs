using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RRemote.Roku
{
    public class RokuClient
    {
        public const string DISCOVER_MESSAGGE = "M-SEARCH * HTTP/1.1\r\n" +
                                                "HOST: {0}:{1}\r\n" +
                                                "ST:roku:ecp\r\n" +
                                                "MAN:\"ssdp:discover\"\r\n";

        public const string LOCATION_PATTERN = @"location:\s?(?<address>http://[^/]+/)";

        //Get a list of Available Roku Devices
        public static async Task Discover(Action<RokuDevice> deviceDiscovered,
            string ip = "239.255.255.250",
            int port = 1900, int waitSeconds = 15)
        {
            try
            {
                List<Task> tasks = new List<Task>();
                NetworkInterface[] nics = GetUsableNics();
                foreach (var nic in nics)
                {
                    foreach (var addr in nic.GetIPProperties().UnicastAddresses
                        .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork))
                    {
                        IPEndPoint ep = new IPEndPoint(addr.Address, 0);
                        tasks.Add(Task.Run(() =>
                        {
                            DiscoverHelp(deviceDiscovered, ep, ip, port, waitSeconds);
                        }));
                    }
                }
                await Task.WhenAll(tasks);
            }
            catch(Exception ex)
            {
                App.Log.Error("Discover", ex);
            }
        }
        private static async Task DiscoverHelp(Action<RokuDevice> deviceDiscovered,
            IPEndPoint ep,
            string ip = "239.255.255.250",
            int port = 1900, int waitSeconds = 15)
        {
            try
            {
                var token = new CancellationTokenSource();

                var mcEndpoint = new IPEndPoint(IPAddress.Parse(ip), port);
                var data = string.Format(DISCOVER_MESSAGGE, ip, port);

                var discoverBytes = Encoding.UTF8.GetBytes(data);

                var startTime = DateTime.Now;
                token.CancelAfter(TimeSpan.FromSeconds(waitSeconds));
                //var deviceQueries = new List<Task<RokuDevice>>();
                using (var udp = new UdpClient(ep))
                {
                    //udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, (int)IPAddress.HostToNetworkOrder(p.Index));
                    //udp.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));
                    await udp.SendAsync(discoverBytes, discoverBytes.Length, mcEndpoint);

                    while (!token.IsCancellationRequested)
                    {
                        var operation = udp.ReceiveAsync();

                        try
                        {
                            operation.Wait(token.Token); //Wait for result until cancelled
                        }
                        catch (OperationCanceledException ex)
                        {
                            //We either have no devices or we didn't get any additional devices
                            //Not sure where to log this but can't display to user
                        }

                        if (operation.IsCompleted && !operation.IsFaulted)
                        {
                            var result = operation.Result;
                            var deviceResponse = Encoding.ASCII.GetString(result.Buffer);

                            var match = Regex.Match(deviceResponse, LOCATION_PATTERN, RegexOptions.IgnoreCase);
                            if (match.Success && deviceResponse.ToLower().Contains("roku"))
                            {
                                var address = match.Groups["address"].Value;
                                //Don't allow device info request to hold up discovering additional devices
                                var deviceQuery = ReadDevice(deviceDiscovered, address);

                                //deviceQueries.Add(deviceQuery);
                            }
                        }
                    }
                }

                //Wait seconds does not apply to this
                //var devices = await Task.WhenAll(deviceQueries);
                //return devices;
            }
            catch (Exception ex)
            {
                App.Log.Error("DiscoverHelp", ex);
            }
        }


        //Get additional device info
        public static async Task ReadDevice(Action<RokuDevice> deviceDiscovered, string endpoint, bool isStatic = false)
        {
            try
            {
                var client = new HttpClient();
                var requestUrl = endpoint + "query/device-info";
                var result = await client.GetStreamAsync(requestUrl);

                var serializer = new XmlSerializer(typeof(RokuDevice));
                var device = (RokuDevice)serializer.Deserialize(result);
                device.Endpoint = endpoint;
                device.IsStatic = isStatic;

                deviceDiscovered?.Invoke(device);
            }
            catch (Exception ex)
            {
                App.Log.Error("ReadDevice", ex);
            }
        }

        //Get a list of apps installed on a device
        public static async Task<IEnumerable<RokuApp>> ListDeviceApps(string endpoint)
        {
            DeviceAppWrapper wrapper = new DeviceAppWrapper();
            try
            {
                var client = new HttpClient();
                var requestUrl = endpoint + "query/apps";
                var result = await client.GetStreamAsync(requestUrl);

                var serializer = new XmlSerializer(typeof(DeviceAppWrapper));
                wrapper = (DeviceAppWrapper)serializer.Deserialize(result);
            }
            catch (Exception ex)
            {
                App.Log.Error("ListDeviceApps", ex);
            }
            return wrapper.Apps;
        }

        //Get the current running app
        public static async Task<RokuActiveApp> GetCurrentApp(string endpoint)
        {
            RokuActiveApp activeApp = new RokuActiveApp();
            try
            {
                var client = new HttpClient();
                var requestUrl = endpoint + "query/active-app";
                var result = await client.GetStreamAsync(requestUrl);

                var serializer = new XmlSerializer(typeof(RokuActiveApp));
                activeApp = (RokuActiveApp)serializer.Deserialize(result);
            }
            catch (Exception ex)
            {
                App.Log.Error("GetCurrentApp", ex);
            }
            return activeApp;
        }

        //Launch an app with the app id returned from the app listing
        public static async Task LaunchApp(string endpoint, string id)
        {
            try
            {
                var client = new HttpClient();
                var requestUrl = string.Format(endpoint + "launch/{0}", id);
                await client.PostAsync(requestUrl, null);
            }
            catch (Exception ex)
            {
                App.Log.Error("LaunchApp", ex);
            }
        }

        //Emulate the roku remote by sending a key command
        //Reference here: https://sdkdocs.roku.com/display/sdkdoc/External+Control+Guide#ExternalControlGuide-KeypressKeyValues
        public static async Task PressKey(string endpoint, string key)
        {
            try
            {
                var client = new HttpClient();
                var requestUrl = string.Format(endpoint + "keypress/{0}", key);
                await client.PostAsync(requestUrl, null);
            }
            catch (Exception ex)
            {
                App.Log.Error("PressKey", ex);
            }
        }

        public static NetworkInterface[] GetUsableNics()
        {
            List<NetworkInterface> ret = new List<NetworkInterface>();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                switch(adapter.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Ethernet:
                    case NetworkInterfaceType.Ethernet3Megabit:
                    case NetworkInterfaceType.FastEthernetFx:
                    case NetworkInterfaceType.FastEthernetT:
                    case NetworkInterfaceType.GigabitEthernet:
                    case NetworkInterfaceType.Wireless80211:
                        break;
                    default:
                        continue;
                }
                IPInterfaceProperties ip_properties = adapter.GetIPProperties();
                if (!adapter.GetIPProperties().MulticastAddresses.Any())
                    continue; // most of VPN adapters will be skipped
                if (!adapter.SupportsMulticast)
                    continue; // multicast is meaningless for this type of connection
                if (OperationalStatus.Up != adapter.OperationalStatus)
                    continue; // this adapter is off or not connected
                IPv4InterfaceProperties p = adapter.GetIPProperties().GetIPv4Properties();
                if (null == p)
                    continue; // IPv4 is not configured on this adapter
                ret.Add(adapter);
            }
            return ret.ToArray();
        }
    }
}