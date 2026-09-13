// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using SharpOnvifClient;
using SharpOnvifClient.Events;
using SharpOnvifClient.Security;
using SharpOnvifCommon.Security;
using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await MainAsync(args);
    }

    static CancellationToken Stopping = CancellationToken.None;

    static ILog Logger = null;

    static OnvifDiscoveryClient Discovery = null;

    static async Task MainAsync(string[] args)
    {
        // Ctrl-C stops waiting, rather than killing the process where it stands.
        using var stopping = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopping.Cancel(); };
        Stopping = stopping.Token;

        Logger = new DefaultOnvifLogger
        {
            IsLoggingEnabled = true,
            IsInfoEnabled = false,
            IsDebugEnabled = false,
            IsTraceEnabled = false,
        };

        Discovery = new OnvifDiscoveryClient(Logger);
        Discovery.Failed += (_, e) => Console.WriteLine($"  ! {e}");

        static bool IsOnThisMachine(OnvifDiscoveryResult candidate) =>
            candidate.Addresses != null &&
            candidate.Addresses.Any(address => address.Contains("127.0.0.1") || address.Contains("[::1]"));

        var devices = await Discovery.DiscoverAsync(null, 1000);

        foreach (var onvifDevice in devices)
        {
            Console.WriteLine($"Found device: Manufacturer = {onvifDevice.Manufacturer}, Model = {onvifDevice.Hardware}");
        }

        var device = devices.FirstOrDefault(IsOnThisMachine);

        if (device == null)
        {
            Console.WriteLine("No Onvif device on this machine yet - waiting for one to announce itself.");
            Console.WriteLine("Start Onvif.Server, or press Ctrl-C to give up.");

            try
            {
                device = await Discovery.WaitForDeviceAsync(IsOnThisMachine, stopping.Token);
                Console.WriteLine($"Device appeared: Manufacturer = {device.Manufacturer}, Model = {device.Hardware}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Gave up waiting.");
                return;
            }
        }

        DigestAuthentication authentication = DigestAuthentication.HttpDigest | DigestAuthentication.WsUsernameToken;
        using (var client = new SimpleOnvifClient(device.Addresses.First(x => x.Contains("127.0.0.1") || x.Contains("[::1]")),
            "admin", 
            "password", 
            new DigestAuthenticationSchemeOptions(authentication),
            true))
        {
            var services = await client.GetServicesAsync(true);
            var cameraDateTime = await client.GetSystemDateAndTimeUtcAsync();
            var cameraTimeOffset = cameraDateTime.Subtract(DateTime.UtcNow);
            Console.WriteLine($"Camera time: {cameraDateTime}");
            if (authentication.HasFlag(DigestAuthentication.WsUsernameToken))
            {
                client.SetCameraUtcNowOffset(cameraTimeOffset); // this is only supported when using WsUsernameToken legacy authentication
            }
                    
            var deviceInfo = await client.GetDeviceInformationAsync();
            Console.WriteLine($"Device Manufacturer: {deviceInfo.Manufacturer}");

            // check if media profile is available
            if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
            {
                var profiles = await client.GetProfilesAsync();
                var streamUri = await client.GetStreamUriAsync(profiles.Profiles.First().token);
                Console.WriteLine($"Stream URI: {streamUri.Uri}");
            }
            else if(services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA2) != null)
            {
                var profiles = await client.GetProfiles2Async();
                var streamUri = await client.GetStreamUri2Async(profiles.Profiles.First().token, "RTSP");
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

                try
                {
                    if (useBasicEvents)
                        await BasicEventSubscription(client);
                    else
                        await PullPointEventSubscription(client);
                }
                catch (Exception ex) 
                { 
                    Console.WriteLine(ex.Message); 
                }
            }
        }
    }

    static async Task PullPointEventSubscription(SimpleOnvifClient client)
    {
        while (true)
        {
            CreatePullPointSubscriptionResponse subscription;
            try
            {
                subscription = await client.PullPointSubscribeAsync(60);
            }
            catch (Exception ex) when (ex is SoapTransportException || ex is SoapFaultException)
            {
                Console.WriteLine($"Cannot subscribe: {ex.Message}");
                if (!await WaitForDevice(client.OnvifUri, Stopping)) return;
                continue;
            }

            string address = subscription.SubscriptionReference.Address.Value;
            Console.WriteLine($"Subscribed: {address}");

            try
            {
                while (true)
                {
                    var messages = await client.PullPointPullMessagesAsync(address);

                    foreach (var ev in messages.NotificationMessage ?? Array.Empty<NotificationMessageHolderType>())
                    {
                        if (OnvifEvents.IsMotionDetected(ev) != null)
                            Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
                        else if (OnvifEvents.IsTamperDetected(ev) != null)
                            Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
                    }
                }
            }
            catch (SoapTransportException ex)
            {
                Console.WriteLine($"Lost the device: {ex.Message}");
                if (!await WaitForDevice(client.OnvifUri, Stopping)) return;
            }
            catch (SoapFaultException ex)
            {
                Console.WriteLine($"The device refused the pull, subscribing again: {ex.Message}");
            }
        }
    }

    static async Task<bool> WaitForDevice(string onvifUri, CancellationToken cancellationToken)
    {
        Console.WriteLine("Waiting for the device to announce itself...");

        try
        {
            var device = await Discovery.WaitForDeviceAsync(
                candidate => candidate.Addresses.Any(address => SameDevice(address, onvifUri)),
                cancellationToken);

            Console.WriteLine($"The device is back: {device.Addresses.FirstOrDefault()}");
            return true;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Gave up waiting.");
            return false;
        }
    }

    static bool SameDevice(string announced, string onvifUri)
    {
        return Uri.TryCreate(announced, UriKind.Absolute, out var a)
            && Uri.TryCreate(onvifUri, UriKind.Absolute, out var b)
            && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase);
    }

    static async Task BasicEventSubscription(SimpleOnvifClient client)
    {
        // we must run as an Administrator for the Basic subscription to work
        string onvifInterfaceIp = FindNetworkInterface(client.OnvifUri);
        SimpleOnvifEventListener eventListener = new SimpleOnvifEventListener(onvifInterfaceIp) { Logger = Logger };
        eventListener.Start((int cameraID, string ev) =>
        {
            if (OnvifEvents.IsMotionDetected(ev) != null)
                Console.WriteLine($"Motion detected: {OnvifEvents.IsMotionDetected(ev)}");
            else if (OnvifEvents.IsTamperDetected(ev) != null)
                Console.WriteLine($"Tamper detected: {OnvifEvents.IsTamperDetected(ev)}");
        });

        SubscribeResponse subscriptionResponse = await client.BasicSubscribeAsync(eventListener.GetOnvifEventListenerUri());

        while (true)
        {
            await Task.Delay(1000 * 60);

            try
            {
                await client.BasicSubscriptionRenewAsync(subscriptionResponse.SubscriptionReference.Address.Value);
            }
            catch (Exception ex) when (ex is SoapTransportException || ex is SoapFaultException)
            {
                Console.WriteLine($"Renewing failed, subscribing again: {ex.Message}");
                subscriptionResponse = await client.BasicSubscribeAsync(eventListener.GetOnvifEventListenerUri());
            }
        }
    }

    private static string FindNetworkInterface(string onvifUri)
    {
        IPAddress[] ipAddresses = null;
        string onvifHost = new Uri(onvifUri).Host;
        try
        {
            ipAddresses = Dns.GetHostAddresses(onvifHost);
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
            UnicastIPAddressInformation addr = x.GetIPProperties().UnicastAddresses.FirstOrDefault(xx => xx.Address.AddressFamily == AddressFamily.InterNetwork);
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