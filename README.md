# SharpOnvif
A C# implementation of the Onvif interface - client as well as the server. All profiles are supported.

> **Upgrading from 0.9.x?** 0.10.0 replaces WCF and CoreWCF with `HttpClient` and ASP.NET Core, and
> merges the 50 per-service packages into two. Nothing changed on the wire. See
> [doc/migration.md](doc/migration.md).

## SharpOnvifServer
Onvif server provides NET8 and NET10 bindings generated from the Onvif WSDLs by
`WsdlGenerator`, hosted on ASP.NET Core. It makes it easy to implement only parts of the
Onvif specification needed for your project.

[![NuGet version](https://img.shields.io/nuget/v/SharpOnvifServer.svg?style=flat-square)](https://www.nuget.org/packages/SharpOnvifServer)

Start with a normal ASP.NET Core application:
```cs
var builder = WebApplication.CreateBuilder();
```
Add Digest authentication for Onvif:
```cs
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddOnvifDigestAuthentication();
```
Implement `IUserRepository` to provide user verification and configure your user:
```cs
public class UserRepository : IUserRepository
{
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "password";

    public Task<UserInfo> GetUser(string userName)
    {
        if (string.Compare(userName, UserName, false) == 0)
        {
            return Task.FromResult(new UserInfo() { UserName = userName, Password = Password });
        }

        return Task.FromResult((UserInfo)null);
    }

    public Task<UserInfo> GetUserAsync(string userName)
    {
        return Task.FromResult(GetUser(userName));
    }

    // used only when userhash=TRUE - see the examples for an implementation
    public UserInfo GetUserByHash(string algorithm, string userName, string realm)
    {
        throw new NotImplementedException();
    }

    public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm)
    {
        throw new NotImplementedException();
    }
}
```
Optionally, add Onvif discovery to make your service discoverable on the network:
```cs
builder.Services.AddOnvifDiscovery();
```
The device answers a Probe, and announces itself with a WS-Discovery Hello when it starts and a Bye
when it stops, so a client learns about it without having to probe. Every announcement names the
same endpoint reference, which is how a client pairs the Bye with the device that said Hello. That
address lasts as long as the process unless you give it one that outlives a restart:
```cs
builder.Services.AddOnvifDiscovery(new OnvifDiscoveryOptions
{
    // From something the device keeps - its serial number or MAC - or a client sees a new
    // device every time this one is restarted.
    EndpointReference = "urn:uuid:" + deviceUuid,
});
```
Simple `DeviceImpl` just extends `SharpOnvifServer.DeviceMgmt.DeviceBase` and overrides a method you want to implement - for instance `GetDeviceInformation`:
```cs
public class DeviceImpl : DeviceBase
{
    public override GetDeviceInformationResponse GetDeviceInformation(GetDeviceInformationRequest request)
    {
        return new GetDeviceInformationResponse()
        {
            FirmwareVersion = "1.0",
            HardwareId = "1.0",
            Manufacturer = "Manufacturer",
            Model = "1",
            SerialNumber = "1"
        };
    }
}
```
Each operation appears three times on the generated base, each layer defaulting to the next, so you can override whichever suits: an async form taking the request, a synchronous form taking the request, and a synchronous form taking the request's members as arguments. Anything you do not override is reported to the client as the `ter:ActionNotSupported` fault.

Add it as a singleton:
```cs
builder.Services.AddSingleton<DeviceImpl>();
```
Add authentication:
```cs
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```
Finally map the service onto a URL and run:
```cs
app.MapOnvifService<DeviceImpl>("/onvif/device_service");
app.Run();
```
Several services can share one URL, which is what real devices do - requests are routed by their SOAP action:
```cs
app.MapOnvifService<DeviceImpl>("/onvif/device_service");
app.MapOnvifService<MediaImpl>("/onvif/device_service");
app.MapOnvifService<PTZImpl>("/onvif/device_service");
```
The operation is found from the `action` parameter of the Content-Type header, a `wsa:Action` SOAP
header, or the body element, whichever the client sends - Onvif Device Manager uses the second form
for event subscriptions.

An address with a trailing segment reaches the same service, which is how Onvif addresses a
subscription manager: a request to `/onvif/Events/PullPointSubscription/aV9xN2sMv1Qb0Zt8/` reaches the
service mapped at `/onvif/Events/PullPointSubscription`, with the segment available to the
implementation as `HttpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID]` - a string, because a
subscription ID is an unguessable token rather than a counter.
Your Onvif service should now be discoverable on the network and you should be able to use Onvif Device Manager or similar tool to call your endpoint.
See `Onvif.Server` sample project for a complete example.
## SharpOnvifClient
Onvif client provides .NET Standard 2.0, .NET Framework 4.8.1, NET8.0 and NET10.0 bindings generated from the Onvif WSDLs by `WsdlGenerator`, over `HttpClient`. `SimpleOnvifClient` wraps common API calls to get basic information from the camera and includes both Pull Point as well as Basic event subscriptions. 

[![NuGet version](https://img.shields.io/nuget/v/SharpOnvifClient.svg?style=flat-square)](https://www.nuget.org/packages/SharpOnvifClient)

To discover Onvif devices on your network, use:
```cs
var discovery = new OnvifDiscoveryClient();
var onvifDevices = await discovery.DiscoverAsync();
```

A Probe only finds what is on the network at the moment it is sent, which is no help to an
application that starts before its camera does. To wait for one to appear instead:
```cs
var device = await discovery.WaitForDeviceAsync(cancellationToken: stopping.Token);
```
It listens for the WS-Discovery Hello a device sends when it joins, and keeps probing as well -
an announcement is UDP multicast, so it can be lost, and one sent before you started listening is
already gone. Pass a predicate to wait for a particular device. There is no timeout, because
"wait until the camera is switched on" has no natural one; cancel the token to stop.

Both listen on every interface that carries multicast, IPv4 and IPv6 alike.

`OnvifDiscoveryListener` is the same mechanism without the waiting, for an application that wants
to keep track of devices coming and going:
```cs
var listener = new OnvifDiscoveryListener();
listener.DeviceAnnounced += (s, e) => Console.WriteLine($"{e.Device.Name} arrived");
listener.DeviceLeft += (s, e) => Console.WriteLine($"{e.Device.Name} left");
listener.Start();
```

To create the `SimpleOnvifClient`, use:
```cs
var client = new SimpleOnvifClient(onvifDevices[0].Addresses[0], "admin", "password");
```

Call `GetDeviceInformationAsync` to retrieve information about the device:
```cs
var deviceInfo = await client.GetDeviceInformationAsync();
```

Call `GetServicesAsync` to retrieve a list of all services supported by the device:
```cs
var services = await client.GetServicesAsync();
```

Some operations require the device to support a service. For instance, to retrieve the stream URI the device must support the media service. To check whether the Onvif service is supported by the device, call:
```cs
if (services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA) != null)
{
    // operation only available when the service is supported
}
```
Full list of services that can be supported by the device is available in `SharpOnvifCommon.OnvifServices`.

### Delivering only what a subscriber asked for
A client subscribing to events says which it wants, and a device that sends it everything else as
well is not conformant. `TopicFilter` reads the filter out of the subscribe request and answers
whether a notification is one of them:
```cs
TopicFilter topics = TopicFilter.FromFilter(request.Filter);
...
if (topics.Matches(notification))
{
    // queue it for this subscriber
}
```
It understands a concrete topic, a set of them separated by `|`, `*` for one level, and a trailing
`//.` for a topic and everything beneath it. An expression written in a dialect it cannot evaluate
matches everything, so a subscriber is never silently sent nothing.

A notification carries name/value pairs as `tt:SimpleItem`. Anything with a shape to it - a
rectangle, an analytics payload - goes in `SourceElements` or `DataElements` and is written as
`tt:ElementItem`:
```cs
var message = new NotificationMessage
{
    Topic = "RuleEngine/CellMotionDetector/Motion",
    Data = { { "IsMotion", "true" } },
    DataElements = { { "Shape", rectangleElement } },
};
```

### Pull Point event subscription
Pull point event subscription does not require any special networking configuration and it should work in most networks. 
To create a new Pull Point subscription, call:
```cs
var subscription = await client.PullPointSubscribeAsync();
```
To retrieve the current notifications from the Pull Point subscription, call:
```cs
var notifications = await client.PullPointPullMessagesAsync(subscription);
foreach (var notification in notifications)
{
    // handle the notification message
    bool? isMotion = SharpOnvifClient.OnvifEvents.IsMotionDetected(notification);
}
```

### Basic event subscription
Basic event subscription utilizes a callback from the camera when an event occurs. This requires the camera to be able to reach your machine through a firewall/NAT. To listen for incoming notifications, you must run `SimpleOnvifEventListener`:
```cs
// ID 1 will identify this camera in the callback
const int CAMERA1 = 1;

var eventListener = new SimpleOnvifEventListener();
eventListener.Start((int cameraID, string ev) =>
{
    bool? isTamper = SharpOnvifClient.OnvifEvents.IsTamperDetected(notification);
    if(cameraID == CAMERA1)
    {
        // handle the notification message for CAMERA1
    }
});

var subscriptionResponse = await client.BasicSubscribeAsync(eventListener.GetOnvifEventListenerUri(CAMERA1));
```

#### Securing the callback
The listener is an inbound endpoint on your machine, and Onvif gives a camera no way to
authenticate itself to it - a notification arrives as a plain POST, so anything that can reach the
port can deliver one, and an application that acts on a notification acts on whatever it is told.

The address `GetOnvifEventListenerUri` hands out therefore carries an unguessable token, and a
request that does not present it is refused before your callback sees it. The camera is asked for
nothing - it posts where it was told - so this works with every device and is on by default. It
stops everything that has not seen the address, which is everything scanning the network; it does
not stop something that has.

Two things are worth adding when you know them:
```cs
// Only this camera may deliver.
eventListener.AllowedSources.Add(IPAddress.Parse("192.168.1.10"));

// Listen on one interface rather than all of them.
var eventListener = new SimpleOnvifEventListener("192.168.1.5", 9999);
```
`RefusedCount` reports how many deliveries were turned away, which is worth logging - a number
that climbs means something other than your camera is posting to the port.

None of this makes the channel private: the notification crosses the network in the clear, and a
camera cannot in general be told to use https. Treat a notification as a hint that something
happened, and read anything that matters from the device over the authenticated connection you
already have. If you cannot use a long path, `PathToken` can be set to null for the bare address
the listener used to hand out.
### Using the generated clients
Every Onvif service is in the `SharpOnvifClient` package, each under its own namespace
(`SharpOnvifClient.DeviceMgmt`, `SharpOnvifClient.Media`, `SharpOnvifClient.PTZ`, and so on), with
the shared Onvif data model in `SharpOnvifCommon.Onvif`. Create the client with the endpoint
address and, if the device requires them, credentials:
```cs
using (var deviceClient = new SharpOnvifClient.DeviceMgmt.DeviceClient(
    "http://192.168.1.10/onvif/device_service", "admin", "password"))
{
    var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());
}
```
Both Onvif digest schemes are offered by default. For full control over authentication and transport, pass `OnvifClientSettings`:
```cs
var settings = new SharpOnvifCommon.Soap.OnvifClientSettings
{
    Credentials = new System.Net.NetworkCredential("admin", "password"),
    Authentication = new SharpOnvifCommon.Security.OnvifAuthenticationSettings(
        SharpOnvifCommon.Security.DigestAuthentication.HttpDigest),
    Timeout = TimeSpan.FromSeconds(30),
};

using (var deviceClient = new SharpOnvifClient.DeviceMgmt.DeviceClient(uri, settings))
{
    var deviceInfo = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());
}
```
Every operation also has an overload that takes the request's members directly, so you rarely need to build the request yourself:
```cs
var services = await deviceClient.GetServicesAsync(includeCapability: false);
```
A device that cannot be reached, drops the connection, or does not answer in time raises
`SharpOnvifCommon.Soap.OnvifTransportException`. A device is a thing that reboots and loses power,
and a pull point spends nearly all its time waiting on a request that any of those cuts short, so
a loop that polls one has to expect it:
```cs
while (true)
{
    var subscription = await client.PullPointSubscribeAsync(60);
    try
    {
        while (true)
        {
            var messages = await client.PullPointPullMessagesAsync(
                subscription.SubscriptionReference.Address.Value);
            // handle the notifications
        }
    }
    catch (OnvifTransportException)
    {
        // the device went away; it has forgotten the subscription, so make a new one
    }
}
```
`TimedOut` tells a device that ran late from one that was not there at all, and `InnerException`
carries what the HTTP stack actually said. A cancellation you asked for is not this - that still
arrives as an `OperationCanceledException`.

A device that answers with a SOAP fault raises `SharpOnvifCommon.Xml.OnvifFaultException`, which carries the Onvif error subcode:
```cs
try
{
    await deviceClient.GetHostnameAsync();
}
catch (OnvifFaultException fault) when (fault.Fault?.Subcode == "ActionNotSupported")
{
    // the device does not implement this operation
}
```
See `Onvif.Client` sample project for a complete example.
## Digest authentication
Onvif supports two types of Digest authentication. Legacy [WS-UsernameToken](https://docs.oasis-open.org/wss/v1.1/wss-v1.1-spec-pr-UsernameTokenProfile-01.htm) authentication carried inside the SOAP headers and HTTP Digest authentication as defined in [RFC 7616](https://www.rfc-editor.org/rfc/rfc7616). Both types of authentication are now supported on both the client and the server.

### Running more than one instance
HTTP Digest keeps two pieces of state on the server, and both are per process by default: the
private key a server nonce is minted and validated with, and the record of which nonces have been
spent, which is what refuses a replayed request. A device, or a single server instance, needs
nothing here.

Behind a load balancer it is not enough. A nonce is validated by recomputing it, so an instance can
only validate nonces minted with the key it holds, and an instance keeping the spent-nonce record
in its own memory accepts a captured request its neighbour has already refused. Give every instance
the same key, from a secret store, and a replay store all of them can read:

```cs
HttpDigestAuthentication.SetNoncePrivateKey(keyFromYourSecretStore);

builder.Services.AddOnvifDigestAuthentication(options =>
{
    options.HttpDigestNonceReplayStore = new MyDistributedNonceReplayStore();
});
```
`INonceReplayStore` has one method - it spends a nonce at a nonce count and says whether that count
had been seen before. `MemoryNonceReplayStore`, the default, holds the record in this process.

### Logging
The client reports what it could not do through an `IOnvifLogger` it is given. The logger belongs
to the object, not to the process, so an application watching several cameras can tell which one
is complaining - or send one of them nowhere:
```cs
var logger = new DefaultOnvifLogger { IsLoggingEnabled = true, IsDebugEnabled = false };

var client = new SimpleOnvifClient(uri, "admin", "password") { Logger = logger };
var listener = new SimpleOnvifEventListener(host) { Logger = logger };
var discovery = new OnvifDiscoveryClient(logger);
```
`OnvifClientSettings.Logger` does the same for a service client built directly. Given none, an
object reports nowhere. Implement `IOnvifLogger` to send it into your own logging, or use
`NullOnvifLogger.Instance` to be explicit about silence. There is no dependency on any logging
package, which is what keeps these assemblies free of dependencies altogether. The server does not
use this - it is given an `ILogger` by the host and logs to that.

Discovery additionally raises `Failed`, on `OnvifDiscoveryClient` and on `OnvifDiscoveryListener`,
because it works on every interface at once and carries on when one of them fails. Worth
subscribing to: a Probe that never left the machine looks exactly like a network with no cameras
on it.
```cs
discovery.Failed += (_, e) => Console.WriteLine(e);
// Onvif discovery could not Probe on 192.168.1.5: No route to host
```
Probing skips the loopback interface: a Probe cannot be multicast out of it, and a device on this
machine is listening on the real interfaces as well, so nothing is lost by not asking there.

### No dependencies
`SharpOnvifClient`, `SharpOnvifServer` and `SharpOnvifCommon` reference no NuGet packages on any of
their target frameworks. The one that remained, `System.Runtime.Caching`, was used for a single
thing - an entry that stops existing at a given moment - which `ExpiringCache` now does in about a
hundred lines.

## Testing
Only the DeviceMgmt, Media and Events were tested with Hikvision cameras. 
Server implementation was tested using Onvif Device Manager.

## Generated bindings
The service bindings are generated by `src/WsdlGenerator`, a WSDL and XML Schema compiler in
this repository, from the specification documents mirrored in `wsdl/`. Generated sources are
committed, so a normal build needs no network access and no external tooling. See
[doc/codegen.md](doc/codegen.md) for how to regenerate them and what the generator does.

The generator is not tied to Onvif. It reads WSDL and XML Schema, so it will generate a client and
a service for any document/literal SOAP 1.2 WSDL:

```
dotnet run --project src/WsdlGenerator -- --wsdl ./bank.wsdl --namespace Example.Banking --out ./Generated
```

All 25 Onvif services ship in `SharpOnvifClient` and `SharpOnvifServer`, one namespace per
service. The Onvif data model itself - `Profile`, `VideoResolution`, `PTZVector` and the rest of
`onvif.xsd` - lives once in `SharpOnvifCommon.Onvif` and is shared by both, so a value read by the
client is the same CLR type a server implementation returns.

Where the Onvif schema names a type after something the framework already has, the generated name
is prefixed to keep both usable side by side without aliases - `tt:DateTime` becomes
`OnvifDateTime`, `tt:IPAddress` becomes `OnvifIPAddress`. What goes on the wire is unchanged.

Two enumerations are generated wider than the schema. onvif.xsd still enumerates `tt:VideoEncoding`
as only JPEG, MPEG4 and H264, so the generated enum also carries `H265`, `AV1`, `H266` and `AV2`;
`tt:AudioEncoding` likewise gains `OPUS` beside G711, G726 and AAC. The extra values are
configured for the generator in `ServiceCatalog`, not edited into its output, so regenerating keeps
them and the conversions to and from their XML form stay in step. Any schema enumeration can be
widened the same way from the command line:

```
dotnet run --project src/WsdlGenerator -- --wsdl ./bank.wsdl --namespace Example.Banking \
    --out ./Generated --enum-value "{http://www.onvif.org/ver10/schema}VideoEncoding=AV1"
```

There are two solutions, `src/SharpOnvif.Client.sln` and `src/SharpOnvif.Server.sln`, either of
which builds the shared `SharpOnvifCommon` and the generator alongside its own side. The server
solution also builds the client, because the end-to-end tests answer the server with the real
client rather than a hand-built request.

Tests are split the same way: `SharpOnvifCommon.Tests` covers what needs neither side - the digest
implementation, the cache, the generator - and is in both solutions; `SharpOnvifClient.Tests` and
`SharpOnvifServer.Tests` cover their own. Running both solutions runs every test, with the common
ones twice.

## Credits
Special thanks to Piotr Stapp for figuring out the SOAP security headers in NET8: https://stapp.space/using-soap-security-in-dotnet-core/.