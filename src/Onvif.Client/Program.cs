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
using SharpOnvifCommon;
using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;
using SharpOnvifClient.Events;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await MainAsync(args);
    }

    /// <summary>Cancelled by Ctrl-C, so that waiting for a device can be given up on.</summary>
    static CancellationToken Stopping = CancellationToken.None;

    /// <summary>The logger this sample gives to everything it makes.</summary>
    static ILog Logger = null;

    /// <summary>One discovery client, reporting through the same logger as the rest.</summary>
    static OnvifDiscoveryClient Discovery = null;

    static async Task MainAsync(string[] args)
    {
        // Ctrl-C stops waiting, rather than killing the process where it stands.
        using var stopping = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stopping.Cancel(); };
        Stopping = stopping.Token;

        // Everything the library could not do, said out loud. Discovery works on every interface
        // at once and carries on when one of them fails, so without this a Probe that never left
        // the machine looks exactly like a network with no cameras on it.
        //
        // The logger is given to each object rather than set once for the process, so an
        // application with several cameras can report each of them separately.
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
            // A Probe only finds what is on the network at the moment it is sent, and the device
            // may not be switched on yet. A device announces itself with a WS-Discovery Hello when
            // it joins, so rather than giving up, wait to be told - WaitForDeviceAsync listens for
            // that and keeps probing, because an announcement is UDP and can be missed.
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

        {
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
    }

    static async Task PullPointEventSubscription(SimpleOnvifClient client)
    {
        // A pull point spends nearly all of its time waiting on a request, so a device that
        // reboots, loses power or is simply stopped cuts that request short. That is normal, and a
        // subscription does not survive it: the device has forgotten it, so a new one is made.
        while (true)
        {
            // The device decides what it grants; asking for a second gets a subscription that is
            // gone before the first pull returns.
            CreatePullPointSubscriptionResponse subscription;
            try
            {
                subscription = await client.PullPointSubscribeAsync(60);
            }
            catch (Exception ex) when (ex is OnvifTransportException || ex is OnvifFaultException)
            {
                // Could not get a subscription at all. Either nothing answered, or something
                // answered that is not our device - when a device releases its port, whatever
                // takes it over answers too, and on a Mac that is AirPlay replying 403.
                //
                // Retrying on a timer would be a busy loop against a machine that may be switched
                // off for hours, so wait to be told it is back. The wait keeps probing as well, so
                // a device that returns quietly is still found.
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
            catch (OnvifTransportException ex)
            {
                Console.WriteLine($"Lost the device: {ex.Message}");
                if (!await WaitForDevice(client.OnvifUri, Stopping)) return;
            }
            catch (OnvifFaultException ex)
            {
                // The device answered and said no - the subscription expired while nobody was
                // pulling, say. It is still there, so a new subscription is the answer, and if
                // that fails too the loop above waits rather than asking again straight away.
                Console.WriteLine($"The device refused the pull, subscribing again: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Waits until the device is back on the network, by listening for the Hello it sends when it
    /// joins. Returns false when the application was asked to stop instead.
    /// </summary>
    /// <remarks>
    /// A device that has gone may be gone for hours. Retrying on a timer spends that time asking a
    /// machine that is switched off; waiting for its announcement costs nothing and reconnects the
    /// moment it comes back. WaitForDeviceAsync keeps probing as well, so a device that returns
    /// without being heard is still found.
    /// </remarks>
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

    /// <summary>Whether an announced address belongs to the device this client is talking to.</summary>
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
            catch (Exception ex) when (ex is OnvifTransportException || ex is OnvifFaultException)
            {
                // The device went away, or forgot the subscription while we were not looking.
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