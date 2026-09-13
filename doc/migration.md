# Migrating from 0.9.x to 0.10.0

0.10.0 replaces WCF and CoreWCF with `HttpClient` on the client and ASP.NET Core endpoint routing
on the server. The service bindings are no longer produced by svcutil but by `WsdlGenerator`, a
WSDL and XML Schema compiler in this repository. Nothing about the SOAP on the wire changed - a
0.9.x client and a 0.10.0 server understand each other, and either still talks to real devices and
to Onvif Device Manager.

What that costs you depends on which half you use:

- **Client**: usually a handful of lines. `SimpleOnvifClient` keeps every constructor and method
  signature it had.
- **Server**: every operation you override changes its return type, and service registration is
  rewritten. Mechanical, but it touches every implementation class.

---

## 1. Packages

The 50 per-service packages are gone. Everything is in three:

| 0.9.x | 0.10.0 |
| --- | --- |
| `SharpOnvifClient` + `SharpOnvifClient.Media` + `SharpOnvifClient.PTZ` + … | `SharpOnvifClient` |
| `SharpOnvifServer` + `SharpOnvifServer.Media` + `SharpOnvifServer.PTZ` + … | `SharpOnvifServer` |
| `SharpOnvifCommon` | `SharpOnvifCommon` |

Delete the per-service `PackageReference` lines; keep the one for the side you use. All 25 services
come with it, each still under its own namespace (`SharpOnvifClient.Media`,
`SharpOnvifServer.PTZ`, …), so your `using` directives for those are unchanged.

The assemblies got smaller despite carrying every service, because the Onvif schema is no longer
duplicated once per service: `SharpOnvifClient.dll` went from 4.57 MB to about 1.7 MB,
`SharpOnvifServer.dll` from 4.90 MB to about 2.0 MB. There are also no NuGet dependencies left at all -
CoreWCF, `System.ServiceModel.*`, `System.Runtime.Caching` and `System.Reflection.DispatchProxy` are
all gone. Target frameworks gained one: `SharpOnvifClient` and `SharpOnvifCommon` build for
netstandard2.0 as well as net481, net8.0 and net10.0, so they can be referenced from Xamarin,
Unity, older .NET Core and anything else that consumes .NET Standard. The server stays on net8.0
and net10.0, because it is ASP.NET Core.

## 2. The Onvif data model moved

The types from `onvif.xsd` - `Profile`, `VideoResolution`, `PTZVector`, `PTZStatus` and the rest -
used to be generated into every service that referenced them, so there was a
`SharpOnvifClient.Media.Profile` and a separate, incompatible `SharpOnvifServer.Media.Profile`. They
now exist once, in `SharpOnvifCommon.Onvif`, shared by both sides.

Add the import to files that use them:

```cs
using SharpOnvifCommon.Onvif;
```

If you wrote those names fully qualified, drop the old prefix:

```cs
- SharpOnvifClient.Media.Profile profile = …;
+ SharpOnvifCommon.Onvif.Profile profile = …;
```

**Ten types were renamed** so that the Onvif namespaces can be imported alongside the framework
ones without aliases. The XML on the wire is untouched; only the C# name changed.

| Schema type | 0.9.x | 0.10.0 | Lives in |
| --- | --- | --- | --- |
| `tt:DateTime` | `DateTime` | `OnvifDateTime` | `SharpOnvifCommon.Onvif` |
| `tt:IPAddress` | `IPAddress` | `OnvifIPAddress` | `SharpOnvifCommon.Onvif` |
| `tt:NetworkInterface` | `NetworkInterface` | `OnvifNetworkInterface` | `SharpOnvifCommon.Onvif` |
| `tt:Object` | `Object` | `OnvifObject` | `SharpOnvifCommon.Onvif` |
| `tt:Scope` | `Scope` | `OnvifScope` | `SharpOnvifCommon.Onvif` |
| `tt:TimeZone` | `TimeZone` | `OnvifTimeZone` | `SharpOnvifCommon.Onvif` |
| `tt:Version` | `Version` | `OnvifVersion` | `SharpOnvifCommon.Onvif` |
| `pt:Attribute` | `Attribute` | `OnvifAttribute` | `SharpOnvifCommon.Onvif` |
| `aev:Action` | `Action` | `OnvifAction` | `…ActionEngine` |
| `tcr:Credential` | `Credential` | `OnvifCredential` | `…Credential` |

The last two are declared by a single service's own WSDL rather than by a shared schema, so they
stay in that service's namespace - `SharpOnvifClient.ActionEngine` and `SharpOnvifServer.Credential`
and so on - and not in `SharpOnvifCommon.Onvif` with the rest.

Where a service declares its own type with a name the shared schema also uses - `Capabilities` is
the common one - qualify it: `new SharpOnvifServer.PTZ.Capabilities()`.

## 3. svcutil's disambiguating suffixes are gone

svcutil appended `1` to a name it had already used. Those suffixes were an artefact of the tool,
not of Onvif, and the generator does not produce them:

| 0.9.x | 0.10.0 |
| --- | --- |
| `SubscribeResponse1` | `SubscribeResponse` |
| `UnsubscribeResponse1` | `UnsubscribeResponse` |
| `RenewResponse1` | `RenewResponse` |

A wrapper element that carried nothing but the response is also gone, so one hop disappears from
these expressions:

```cs
- subscriptionResponse.SubscribeResponse.SubscriptionReference.Address.Value
+ subscriptionResponse.SubscriptionReference.Address.Value
```

## 4. Client

### SimpleOnvifClient

Source-compatible. Every constructor, `SetCameraUtcNowOffset`, the device, media, PTZ and event
calls all keep their signatures. The only edits are the response renames in section 3, which affect
`BasicSubscribeAsync`, `BasicSubscriptionRenewAsync`, `PullPointUnsubscribeAsync` and
`BasicSubscriptionUnsubscribeAsync`, and the `using` in section 2.

`SimpleOnvifEventListener` keeps its shape, though the address it hands out does not - see below.
`OnvifDiscoveryClient` is no longer a static class; see "OnvifDiscoveryClient is an instance".

### Service clients

Creating one no longer means building a WCF binding and wrapping the client in an authentication
proxy:

```cs
// 0.9.x
DigestAuthenticationSchemeOptions authentication = new DigestAuthenticationSchemeOptions();
var credentials = new System.Net.NetworkCredential(userName, password);
System.ServiceModel.Description.IEndpointBehavior legacyAuth = new WsUsernameTokenBehavior(credentials);
var disableExpect100Continue = new DisableExpect100ContinueBehavior();

using (var deviceClient = new SharpOnvifClient.DeviceMgmt.DeviceClient(
    OnvifBindingFactory.CreateBinding(uri),
    new System.ServiceModel.EndpointAddress(uri)))
{
    DisableExpect100ContinueBehaviorExtensions.SetDisableExpect100Continue(deviceClient, disableExpect100Continue);
    var proxyClient = OnvifAuthenticationExtensions.SetOnvifAuthentication(
        deviceClient, credentials, authentication, legacyAuth);

    var info = await proxyClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());
}
```

```cs
// 0.10.0
using (var deviceClient = new SharpOnvifClient.DeviceMgmt.DeviceClient(uri, userName, password))
{
    var info = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());
}
```

Both Onvif digest schemes are offered by default. For full control, pass
`SharpOnvifCommon.Soap.OnvifClientSettings` instead of the credentials - it carries `Credentials`,
`Authentication`, `Timeout` and `DisableExpect100Continue`. `DigestAuthenticationSchemeOptions` is
still in `SharpOnvifClient.Security` and now derives from `OnvifAuthenticationSettings`.

Every operation also has an overload taking the request's members directly, so the request object is
usually unnecessary: `await deviceClient.GetServicesAsync(includeCapability: false)`.

These types have no replacement, because they described WCF and there is no WCF left:
`OnvifBindingFactory`, `SharpOnvifClient.Behaviors.*` (`DisableExpect100ContinueBehavior`,
`OnvifMessageFormatterBehavior`), `SharpOnvifClient.Formatter.*`, `WsUsernameTokenBehavior`,
`HttpDigestBehavior`, `OnvifAuthenticationExtensions.SetOnvifAuthentication`. What they configured
is now either the default or a property on `OnvifClientSettings`.

### Faults

A device that answers with a SOAP fault used to raise `System.ServiceModel.FaultException`. It now
raises `SharpOnvifCommon.Xml.OnvifFaultException`, which carries the Onvif subcode directly:

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

### A device that does not answer raises an Onvif exception

A client call used to let the HTTP stack's own exceptions through: a dropped connection as an
`HttpIOException` inside an `HttpRequestException`, a timeout as a `TaskCanceledException`. Neither
says anything about Onvif, and an application that did not catch all of them crashed when the
device was stopped. Both are now `SharpOnvifCommon.Soap.OnvifTransportException`, with the original
exception as `InnerException` and `TimedOut` telling the two apart.

If you catch `HttpRequestException` around an Onvif call, catch `OnvifTransportException` instead.
A cancellation you requested is unchanged - it still arrives as an `OperationCanceledException`.

### The Basic subscription callback address changed shape

`SimpleOnvifEventListener.GetOnvifEventListenerUri` used to return
`http://host:port/<cameraID>/`, which anything that found the port could post to. It now carries an
unguessable token - `http://host:port/<token>/<cameraID>/` - and a notification that does not
present it is refused before your callback sees it.

Nothing in your code changes as long as you hand the camera whatever
`GetOnvifEventListenerUri` returns, which is what the sample does. If you built the address
yourself, or persisted one across runs, it will no longer be accepted: ask the listener for it.
Setting `PathToken` to null restores the old bare address.

## 5. Server

### Registration

CoreWCF's service model goes away entirely:

```cs
// 0.9.x
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
…
app.UseOnvif()
   .UseOnvifEvents("/onvif/Events/PullPointSubscription");

((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<DeviceImpl>();
    serviceBuilder.AddServiceEndpoint<DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(
        OnvifBindingFactory.CreateBinding(), "/onvif/device_service");

    serviceBuilder.AddService<MediaImpl>();
    serviceBuilder.AddServiceEndpoint<MediaImpl, SharpOnvifServer.Media.Media>(
        OnvifBindingFactory.CreateBinding(), "/onvif/media_service");
});
```

```cs
// 0.10.0
using SharpOnvifServer.Dispatch;
…
app.MapOnvifService<DeviceImpl>("/onvif/device_service");
app.MapOnvifService<MediaImpl>("/onvif/device_service");
```

`AddServiceModelServices`, `AddServiceModelMetadata`, `UseServiceModel`, `AddServiceEndpoint`,
`OnvifBindingFactory` and the `ServiceMetadataBehavior` configuration all go. The `[ServiceContract]`
interface that was the second type argument to `AddServiceEndpoint` is not needed: the service is
identified by its implementation class alone.

`app.UseOnvif()` and `app.UseOnvifEvents(address)` still exist but do nothing and are marked
obsolete, so existing startup code keeps compiling. Delete the calls.

Two things this buys you, both of which used to need workarounds:

- **Several services can share one address**, which is what real devices do. CoreWCF could not put
  two services on one endpoint, which is why 0.9.x had to publish media and PTZ on
  `/onvif/media_service` and `/onvif/ptz_service`. Map them all on `/onvif/device_service` now.
- **Subscription addresses route themselves.** Onvif hands a client a reference like
  `/onvif/Events/PullPointSubscription/aV9xN2sMv1Qb0Zt8/`; `MapOnvifService` matches the trailing segment and
  makes it available as `HttpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID]`. `UseOnvifEvents`
  used to rewrite the path to arrange this, which cannot work under endpoint routing - routing runs
  ahead of application middleware, so by the time the rewrite ran the endpoint had already been
  chosen.

### Implementations

Your class still derives from the same `XxxBase` and still overrides the operations it implements.
Three things change in it.

**1. Drop the CoreWCF import and the message-parameter attributes.**

```cs
- using CoreWCF;
+ using SharpOnvifServer.Dispatch;
…
- [return: MessageParameter(Name = "Capabilities")]
  public override … GetServiceCapabilities()
```

**2. Every operation returns its response type.** This is the one change that touches every
override. The bases are generated in message-contract style throughout, so an operation that
returned a bare member or `void` now returns the operation's response message:

```cs
- public override Capabilities GetServiceCapabilities()
+ public override GetServiceCapabilitiesResponse GetServiceCapabilities()
  {
-     return new Capabilities() { EFlip = true, … };
+     return new GetServiceCapabilitiesResponse(new SharpOnvifServer.PTZ.Capabilities() { EFlip = true, … });
  }

- public override void AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
+ public override AbsoluteMoveResponse AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
  {
      …
+     return new AbsoluteMoveResponse();
  }
```

Each response type has a constructor taking its members, so wrapping a value you already build is
one call. The compiler finds every one of these for you: the signature no longer matches the base,
so the `override` fails to compile.

Each operation now appears three times on the base, each layer defaulting to the next, so you can
override whichever suits: an async form taking the request, a synchronous form taking the request,
and a synchronous form taking the request's members as arguments. The last is what 0.9.x had.
Anything you do not override is reported to the client as `ter:ActionNotSupported`.

`XxxBase` is also `abstract` now. If you instantiated one directly rather than deriving from it,
derive from it.

**3. `OperationContext` becomes `OnvifOperationContext`.**

```cs
- Uri endpointUri = OperationContext.Current.IncomingMessageProperties.Via;
+ Uri endpointUri = OnvifOperationContext.RequestUri;
```

`OnvifOperationContext.Current` is the `HttpContext` if you need more than the address.

### Event subscriptions are identified by a token, not a number

`IEventSubscriptionManager<T>` used `int` subscription IDs, handed out by a counter and placed in
the address a client is told to come back to. Since a subscription is addressed by ID alone, any
client that could reach the endpoint could reach every other client's subscription by counting -
reading its events, or cancelling it:

```
0.9.x   /onvif/Events/PullPointSubscription/3/
0.10.0  /onvif/Events/PullPointSubscription/aV9xN2sMv1Qb0Zt8/
```

IDs are now unguessable strings, and the three interface methods take `string`:

```cs
- int  AddSubscription(T subscription);
- T    GetSubscription(int subscriptionID);
- void RemoveSubscription(int subscriptionID);
+ string AddSubscription(T subscription);
+ T      GetSubscription(string subscriptionID);
+ void   RemoveSubscription(string subscriptionID);
```

`HttpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID]` holds a `string` to match, so an
implementation that cast it changes with the interface:

```cs
- int subscriptionID = (int)httpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID];
+ string subscriptionID = httpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] as string;
```

Nothing else about the flow changes: the ID still goes in the address, and `MapOnvifService` still
routes the trailing segment to the service.

### HTTP Digest helpers

`SharpOnvifCommon.Security.HttpDigestAuthentication` changed in two ways that a server which calls
it directly will notice. Most servers do not - the authentication handler calls it for you.

```cs
- int  result = HttpDigestAuthentication.ValidateServerNonce(algorithm, type, nonce, nc, now, …);
+ int  result = await HttpDigestAuthentication.ValidateServerNonceAsync(algorithm, type, nonce, nc, now, …);
```

Spending a nonce is asynchronous because the store that remembers spent ones can live outside the
process - see `INonceReplayStore` below.

The `NoncePrivateKey` field is gone. It made the key that signs every nonce readable and writable
by anything in the process, which is enough to mint nonces the server will accept as its own. Use
`RegenerateNoncePrivateKey()`, or `SetNoncePrivateKey(byte[])` where several instances have to
agree on one.

### Unchanged

`IUserRepository`, `AddOnvifDigestAuthentication`, `DigestAuthenticationSchemeOptions`,
`AddOnvifDiscovery`, `OnvifDiscoveryOptions`, `SharpOnvifServer.Events.IEventSource` and the
`IServer.GetHttpEndpoint` helpers all keep their shapes: nothing you already set on them has moved
or changed meaning.

Two of them have gained optional properties you may want -
`DigestAuthenticationSchemeOptions.HttpDigestNonceReplayStore` for running more than one instance,
and `OnvifDiscoveryOptions.EndpointReference`, `MetadataVersion` and `AnnounceOnStartAndStop` for
the announcements described below. Leave them alone and the behaviour is what it was.

`IEventSubscriptionManager<T>` and `DefaultEventSubscriptionManager<T>` do not keep their shapes -
see above.

## 6. Behaviour that changed without the signature changing

These compile as they did and behave differently, because they were wrong:

- **`OnvifHelpers.FromTimeout`** read a duration ending in `S` as minutes, so `PT60S` - what this
  library writes for a 60 second timeout - came back as an hour. It now reads the whole
  `xs:duration` grammar, so `PT1M30S` and `PT1H` work too instead of throwing. Any subscription
  lifetime computed from a relative termination time was sixty times too long and is now right.
- **`OnvifHelpers.StringToDateTime`** returned a local-time `DateTime` for a value carrying a zone,
  and parsed in the current culture. It returns UTC and parses invariantly.
  `DateTimeToString` converts to UTC rather than labelling a local time with `Z`.
- **`OnvifEvents.IsMotionDetected`** and its tamper and sound counterparts read the state from the
  named data item of the notification - `IsMotion`, `IsTamper`, `IsSoundDetected` - rather than
  from any true value anywhere in the message. A camera reporting `IsMotion=false` beside an
  enabled rule used to report motion. If you relied on the old reading, you were reading a false
  alarm.
- A notification missing its topic or its message used to throw from those helpers. They return
  null, which is also what they return for a notification about something else.
- **A pull point subscription now expires when it says it will.** It follows from the duration
  fix above: a device that granted `PT1S` used to hold the subscription for a minute, so a client
  asking for something too short worked by accident. Ask for a usable lifetime - the sample now
  asks for 60 seconds - and renew it. A device implemented on this library decides what it grants
  and reports it back in `TerminationTime`, which is what a client should believe rather than what
  it asked for.
- **The server now bounds what it will read.** A request larger than
  `OnvifEndpoint.MaxRequestBytes` (2 MB) is answered with a fault rather than read, and a document
  nested deeper than `OnvifXmlReader.MaxDepth` (256) is refused - reading contracts recurses, and a
  stack overflow cannot be caught. Both are far above anything Onvif describes, and both are
  settable if your device really does send more.

### OnvifDiscoveryClient is an instance

It was a static class. It is a normal one now, so that the logger and the `Failed` event belong to
it rather than to the process - an application watching more than one thing can tell which part of
it is reporting.

```cs
- var devices = await OnvifDiscoveryClient.DiscoverAsync();
+ var discovery = new OnvifDiscoveryClient();
+ var devices = await discovery.DiscoverAsync();
```

`DiscoverAsync`, `WaitForDeviceAsync`, `WaitForDeviceProbeInterval` and `Failed` are instance
members; the constants - `ONVIF_DISCOVERY_PORT`, the multicast addresses, `ONVIF_MULTICAST_TIMEOUT`
- are still static. Holding an instance costs nothing: the sockets are opened per call.

### Discovery finds things it did not find before

Four faults, all of them a multicast operation that has to name an interface and did not. Nothing
in your code changes; what changes is that discovery now works where it silently did not.

- **A device is discoverable from the machine it runs on.** Every socket was bound to an
  interface's own address, and such a socket is given multicast that arrives from the wire but not
  multicast looped back from the same machine. A client beside a device never saw its Probe or its
  Hello. They are bound to `Any` and joined per interface now, which is what the join was always
  deciding anyway.
- **IPv6 discovery works.** The device's join used interface index zero, which is not an
  interface - `ff02::c` is link-local scope, so there is no default to fall back on - and every
  IPv6 interface failed to start with "Can't assign requested address". The client's Probe had the
  matching fault on the sending side and failed with "No route to host". Both name the interface
  now. If you concluded that IPv6 discovery did not work, try it again.
- **Probing skips the loopback interface.** A Probe cannot be multicast out of it. Nothing is lost:
  a device on this machine is listening on the real interfaces too, which is how it is found.

Expect more multicast traffic from a device as a result - see the announcements below.

### A hosted device now shuts down

`DiscoveryService` waited on a listener that was sitting in a socket read which closing the socket
does not interrupt, so `StopAsync` never returned. The host could not stop, had to be killed, and a
killed host leaves its sockets open and its multicast group joined. If you were killing the process
to stop it, or wondering why ports stayed busy between runs, that was why. Shutdown is bounded now:
listeners are given five seconds and the sockets are closed regardless.

### Diagnostics that used to go nowhere

Failures inside the client were written with `Debug.WriteLine`, which a release build removes. They
now go to an `IOnvifLogger` the object was given - `SimpleOnvifClient.Logger`,
`SimpleOnvifEventListener.Logger`, `OnvifDiscoveryClient.Logger`, `OnvifDiscoveryListener.Logger`,
or `OnvifClientSettings.Logger` for a service client built directly. Given none, an object reports
nowhere, so the default behaviour is as quiet as before and there is now a way to hear it. The
logger belongs to the object rather than the process, so two of anything can report to different
places.

Discovery also raises `Failed`, on the discovery client and on the listener, for failures on a
single interface, which it carries on past.

## 7. New in 0.10.0

Worth knowing about once you are building again:

- `tt:VideoEncoding` carries `H265`, `AV1`, `H266` and `AV2`, and `tt:AudioEncoding` carries `OPUS`,
  none of which onvif.xsd enumerates. 0.9.x had H265 and AV1 hand-edited into the generated file;
  they are configured for the generator now, so regenerating keeps them.
- HTTP Digest gained mutual authentication (`Authentication-Info` / `rspauth`), nonce-count replay
  protection, and an `INonceReplayStore` for deployments running more than one instance behind one
  address. See the README.
- **A device announces itself.** It sends a WS-Discovery Hello when it starts and a Bye when it
  stops, so a client learns of it without probing. Every announcement names the same
  `OnvifDiscoveryOptions.EndpointReference`; set it from something that survives a restart - a
  serial number or MAC - or a client sees a new device each time the old one comes back.
  `AnnounceOnStartAndStop` turns it off, and `MetadataVersion` is now yours to set.
- **A client can wait for a device instead of giving up.** `WaitForDeviceAsync` listens for that
  Hello and keeps probing while it waits, for an application that starts before
  its camera does or has to survive one rebooting. `OnvifDiscoveryListener` is the same mechanism
  without the waiting, reporting devices arriving and leaving as they do.
- **`SharpOnvifServer.Events.TopicFilter`** reads the topics a subscriber asked for out of its
  Filter and says whether a notification is one of them, so an implementation can deliver what was
  asked for rather than everything. A notification can now also carry `tt:ElementItem`, through
  `NotificationMessage.SourceElements` and `DataElements` - anything with a shape to it, like a
  rectangle, which a `SimpleItem` cannot express.
- `WsdlGenerator` is not specific to Onvif and will generate a client and a service for any
  document/literal SOAP 1.2 WSDL. See [codegen.md](codegen.md).

## 8. If you copy the sample's configuration

`Onvif.Server` moved off port 5000 to 8090, and 5001 to 8443. On macOS the AirPlay Receiver
listens on 5000 and answers anything else with 403, which to a client is indistinguishable from
the device refusing it - and it takes the port the moment your device releases it, so it appears
exactly when you stop the server. If you kept 5000, that is worth knowing about.
