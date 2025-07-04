using SharpOnvifClient;
using SharpOnvifClient.Events;
using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await MainAsync(args);
    }

    static async Task MainAsync(string[] args)
    {
        var devices = await OnvifDiscoveryClient.DiscoverAsyncWithMake();

        if (devices == null || devices.Count == 0)
        {
            Console.WriteLine("No ONVIF devices found. Please check your network connection and try again.");
            return;
        }

        foreach (var onvifDevice in devices)
        {
            Console.WriteLine($"Found device: Make = {onvifDevice.Make}, Model = {onvifDevice.Model}");
        }

        var device = devices.FirstOrDefault(x => x.Endpoint != null && x.Endpoint.Contains("localhost"));

        if (device == null)
        {
            Console.WriteLine("Please run OnvifService on the localhost as Administrator, or use a different camera URL and credentials.");
        }
        else
        {
            using (var client = new SimpleOnvifClient(device.Endpoint, "admin", "password", true))
            {
                var services = await client.GetServicesAsync(true);
                var cameraDateTime = await client.GetSystemDateAndTimeUtcAsync();
                var cameraTimeOffset = cameraDateTime.Subtract(DateTime.UtcNow);
                Console.WriteLine($"Camera time: {cameraDateTime}");
                client.SetCameraUtcNowOffset(cameraTimeOffset); // this is only supported when using WsUsernameToken legacy authentication
                
                var deviceInfo = await client.GetDeviceInformationAsync();
                Console.WriteLine($"Device Manufacturer: {deviceInfo.Manufacturer}");

                // check if media profile is available
                if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
                {
                    var profiles = await client.GetProfilesAsync();
                    var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First().token);
                    Console.WriteLine($"Stream URI: {streamUri.Uri}");
                }

                // check if ptz profile is available
                if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.PTZ) != null)
                {
                    var profiles = await client.GetProfilesAsync();
                    await client.AbsoluteMoveAsync(profiles.Profiles.First().token, 1f, 1f, 1f, 1f, 1f, 1f);

                    var currentPosition = await client.GetStatusAsync(profiles.Profiles.First().token);
                    Console.WriteLine($"Current pan: {currentPosition.Position.PanTilt.x}");
                    Console.WriteLine($"Current tilt: {currentPosition.Position.PanTilt.y}");
                    Console.WriteLine($"Current zoom: {currentPosition.Position.Zoom.x}");
                }

                // check if event profile is available
                if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.EVENTS) != null)
                {
                    // basic events vs pull point subscription
                    bool useBasicEvents = false; // = false;

                    if (useBasicEvents)
                        await BasicEventSubscription(client);
                    else
                        await PullPointEventSubscription(client);
                }
            }
        }
    }

    static async Task PullPointEventSubscription(SimpleOnvifClient client)
    {
        var subscription = await client.PullPointSubscribeAsync(1);
        while (true)
        {
            var messages = await client.PullPointPullMessagesAsync(subscription.SubscriptionReference.Address.Value);

            foreach (var ev in messages.NotificationMessage)
            {
                if (OnvifEvents.IsMotionDetected(ev) != null)
                    Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
                else if (OnvifEvents.IsTamperDetected(ev) != null)
                    Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
            }
        }
    }

    static async Task BasicEventSubscription(SimpleOnvifClient client)
    {
        // we must run as an Administrator for the Basic subscription to work
        string onvifInterfaceIp = FindNetworkInterface(client.OnvifUri);
        SimpleOnvifEventListener eventListener = new SimpleOnvifEventListener(onvifInterfaceIp);
        eventListener.Start((int cameraID, string ev) =>
        {
            if (OnvifEvents.IsMotionDetected(ev) != null)
                Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
            else if (OnvifEvents.IsTamperDetected(ev) != null)
                Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
        });

        SubscribeResponse1 subscriptionResponse = await client.BasicSubscribeAsync(eventListener.GetOnvifEventListenerUri());

        while (true)
        {
            await Task.Delay(1000 * 60);
            var result = await client.BasicSubscriptionRenewAsync(subscriptionResponse.SubscribeResponse.SubscriptionReference.Address.Value);
        }
    }

    private static string FindNetworkInterface(string onvifUri)
    {
        IPAddress[] ipAddresses = null;
        string onvifHost = new Uri(onvifUri).Host;
        try
        {
            ipAddresses = Dns.GetHostAddresses(onvifHost, AddressFamily.InterNetwork);
        }
        catch (SocketException)
        {
            Console.WriteLine($"Cannot resolve host {onvifHost}");
            return null;
        }

        IPAddress onvifDeviceIpAddress = ipAddresses.First();
        IEnumerable<NetworkInterface> networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Where(
                i =>
                //i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                i.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                i.OperationalStatus == OperationalStatus.Up
            );
        NetworkInterface matchingInterface = networkInterfaces.FirstOrDefault(x =>
        {
            UnicastIPAddressInformation addr = x.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
            return addr.Address.GetNetworkAddress(addr.IPv4Mask).IsInSameSubnet(onvifDeviceIpAddress.GetNetworkAddress(addr.IPv4Mask), addr.IPv4Mask);
        });
        return matchingInterface.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString();
    }

    public static bool IsInSameSubnet(this IPAddress address2, IPAddress address, IPAddress subnetMask)
    {
        IPAddress network1 = address.GetNetworkAddress(subnetMask);
        IPAddress network2 = address2.GetNetworkAddress(subnetMask);
        return network1.Equals(network2);
    }

    public static IPAddress GetNetworkAddress(this IPAddress address, IPAddress subnetMask)
    {
        byte[] ipAdressBytes = address.GetAddressBytes();
        byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

        if (ipAdressBytes.Length != subnetMaskBytes.Length)
            throw new ArgumentException("IP address and subnet mask lengths do not match.");

        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
        for (int i = 0; i < broadcastAddress.Length; i++)
        {
            broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));
        }
        return new IPAddress(broadcastAddress);
    }
}