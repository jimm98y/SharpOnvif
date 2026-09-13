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
duplicated once per service: `SharpOnvifClient.dll` went from 4.57 MB to 1.69 MB,
`SharpOnvifServer.dll` from 4.90 MB to 2.02 MB. There are also no NuGet dependencies left at all -
CoreWCF, `System.ServiceModel.*`, `System.Runtime.Caching` and `System.Reflection.DispatchProxy` are
all gone. Target frameworks are unchanged: net481, net8.0 and net10.0 for the client and common,
net8.0 and net10.0 for the server.

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

| Onvif schema type | 0.9.x | 0.10.0 |
| --- | --- | --- |
| `tt:Action` | `Action` | `OnvifAction` |
| `tt:Attribute` | `Attribute` | `OnvifAttribute` |
| `tt:Credential` | `Credential` | `OnvifCredential` |
| `tt:DateTime` | `DateTime` | `OnvifDateTime` |
| `tt:IPAddress` | `IPAddress` | `OnvifIPAddress` |
| `tt:NetworkInterface` | `NetworkInterface` | `OnvifNetworkInterface` |
| `tt:Object` | `Object` | `OnvifObject` |
| `tt:Scope` | `Scope` | `OnvifScope` |
| `tt:TimeZone` | `TimeZone` | `OnvifTimeZone` |
| `tt:Version` | `Version` | `OnvifVersion` |

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

`OnvifDiscoveryClient.DiscoverAsync` and `SimpleOnvifEventListener` are unchanged.

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
  `/onvif/Events/PullPointSubscription/3/`; `MapOnvifService` matches the trailing segment and makes
  it available as `HttpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID]`. `UseOnvifEvents` used to
  rewrite the path to arrange this, which cannot work under endpoint routing - routing runs ahead of
  application middleware, so by the time the rewrite ran the endpoint had already been chosen.

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
the address a client is told to come back to (`/onvif/Events/PullPointSubscription/3/`). Since a
subscription is addressed by ID alone, any client that could reach the endpoint could reach every
other client's subscription by counting - reading its events, or cancelling it.

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

### Unchanged

`IUserRepository`, `AddOnvifDigestAuthentication`, `DigestAuthenticationSchemeOptions`,
`AddOnvifDiscovery`, `OnvifDiscoveryOptions`, the `SharpOnvifServer.Events` interfaces
(`IEventSource`, `IEventSubscriptionManager<T>`, `DefaultEventSubscriptionManager<T>`) and the
`IServer.GetHttpEndpoint` helpers all keep their shapes.

## 6. New in 0.10.0

Worth knowing about once you are building again:

- `tt:VideoEncoding` carries `H265`, `AV1`, `H266` and `AV2`, and `tt:AudioEncoding` carries `OPUS`,
  none of which onvif.xsd enumerates. 0.9.x had H265 and AV1 hand-edited into the generated file;
  they are configured for the generator now, so regenerating keeps them.
- HTTP Digest gained mutual authentication (`Authentication-Info` / `rspauth`), nonce-count replay
  protection, and an `INonceReplayStore` for deployments running more than one instance behind one
  address. See the README.
- `WsdlGenerator` is not specific to Onvif and will generate a client and a service for any
  document/literal SOAP 1.2 WSDL. See [codegen.md](codegen.md).
