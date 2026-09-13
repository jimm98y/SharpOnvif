# Migrating from 0.9.x to 0.10.0

0.10.0 replaces WCF and CoreWCF with `HttpClient` and ASP.NET Core. Nothing about the SOAP on the
wire changed, so a 0.9.x client and a 0.10.0 server understand each other, and either still talks
to real devices and to Onvif Device Manager.

Only what breaks is listed here, with what to do about it. The client side is usually a handful of
lines; the server side touches every operation you implement.

---

## 1. Packages

The 50 per-service packages are gone. Delete those `PackageReference` lines and keep one:

| 0.9.x | 0.10.0 |
| --- | --- |
| `SharpOnvifClient` + `SharpOnvifClient.Media` + `SharpOnvifClient.PTZ` + … | `SharpOnvifClient` |
| `SharpOnvifServer` + `SharpOnvifServer.Media` + `SharpOnvifServer.PTZ` + … | `SharpOnvifServer` |

All 25 services ship in the one package, each still under its own namespace
(`SharpOnvifClient.Media`, `SharpOnvifServer.PTZ`, …), so your `using` directives for those are
unchanged.

## 2. The Onvif data model moved

The types from `onvif.xsd` - `Profile`, `VideoResolution`, `PTZVector` and the rest - were
generated into every service that used them. They now exist once, in `SharpOnvifCommon.Onvif`.

```cs
+ using SharpOnvifCommon.Onvif;
```

Fully qualified uses need the new namespace:

```cs
- SharpOnvifClient.Media.Profile profile = …;
+ SharpOnvifCommon.Onvif.Profile profile = …;
```

**Ten types were renamed** so the Onvif namespaces can be imported alongside the framework ones
without aliases. The XML on the wire is unchanged.

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

The last two are declared by one service's own WSDL, so they stay in that service's namespace -
`SharpOnvifClient.ActionEngine`, `SharpOnvifServer.Credential` - not in `SharpOnvifCommon.Onvif`.

Where a service declares a type whose name the shared schema also uses - `Capabilities` is the
common one - qualify it: `new SharpOnvifServer.PTZ.Capabilities()`.

## 3. svcutil's `1` suffixes are gone

| 0.9.x | 0.10.0 |
| --- | --- |
| `SubscribeResponse1` | `SubscribeResponse` |
| `UnsubscribeResponse1` | `UnsubscribeResponse` |
| `RenewResponse1` | `RenewResponse` |

A wrapper element that carried nothing but the response went with them, so one hop disappears:

```cs
- subscriptionResponse.SubscribeResponse.SubscriptionReference.Address.Value
+ subscriptionResponse.SubscriptionReference.Address.Value
```

## 4. Client

### Creating a service client

The WCF binding and the authentication proxy are gone:

```cs
- using (var deviceClient = new DeviceClient(
-     OnvifBindingFactory.CreateBinding(uri), new EndpointAddress(uri)))
- {
-     var proxy = OnvifAuthenticationExtensions.SetOnvifAuthentication(
-         deviceClient, credentials, authentication, legacyAuth);
-     var info = await proxy.GetDeviceInformationAsync(new GetDeviceInformationRequest());
- }
+ using (var deviceClient = new DeviceClient(uri, userName, password))
+ {
+     var info = await deviceClient.GetDeviceInformationAsync(new GetDeviceInformationRequest());
+ }
```

`OnvifBindingFactory`, `SharpOnvifClient.Behaviors.*`, `SharpOnvifClient.Formatter.*`,
`WsUsernameTokenBehavior`, `HttpDigestBehavior` and `SetOnvifAuthentication` have no replacement:
they described WCF, and what they configured is now either the default or a property on
`SharpOnvifCommon.Soap.OnvifClientSettings`.

`SimpleOnvifClient` keeps every constructor and method signature. Its only edits are the response
renames in section 3 and the `using` in section 2.

### Exceptions

| Situation | 0.9.x | 0.10.0 |
| --- | --- | --- |
| The device answered with a SOAP fault | `FaultException` | `SharpOnvifCommon.Xml.OnvifFaultException` |
| The device could not be reached, dropped the connection, or timed out | `HttpRequestException`, `TaskCanceledException` | `SharpOnvifCommon.Soap.OnvifTransportException` |

A cancellation you requested still arrives as `OperationCanceledException`. A pull point spends
nearly all its time waiting on a request, so a loop that polls one has to catch
`OnvifTransportException` and subscribe again - the device has forgotten the subscription.

### `OnvifDiscoveryClient` is an instance

```cs
- var devices = await OnvifDiscoveryClient.DiscoverAsync();
+ var devices = await new OnvifDiscoveryClient().DiscoverAsync();
```

The methods are instance members now; the constants - `ONVIF_DISCOVERY_PORT`, the multicast
addresses, `ONVIF_MULTICAST_TIMEOUT` - are still static.

### The Basic subscription callback address

`SimpleOnvifEventListener.GetOnvifEventListenerUri` used to return `http://host:port/<cameraID>/`,
which anything that found the port could post to. It now carries an unguessable token, and a
notification without it is refused.

Nothing changes if you hand the camera whatever that method returns. If you built the address
yourself or persisted one, ask the listener for it instead. `PathToken = null` restores the old
form.

## 5. Server

### Registration

```cs
- builder.Services.AddServiceModelServices();
- builder.Services.AddServiceModelMetadata();
- …
- app.UseOnvif().UseOnvifEvents("/onvif/Events/PullPointSubscription");
- ((IApplicationBuilder)app).UseServiceModel(serviceBuilder =>
- {
-     serviceBuilder.AddService<DeviceImpl>();
-     serviceBuilder.AddServiceEndpoint<DeviceImpl, SharpOnvifServer.DeviceMgmt.Device>(
-         OnvifBindingFactory.CreateBinding(), "/onvif/device_service");
- });
+ using SharpOnvifServer.Dispatch;
+ app.MapOnvifService<DeviceImpl>("/onvif/device_service");
```

`AddServiceModelServices`, `AddServiceModelMetadata`, `UseServiceModel`, `AddServiceEndpoint`,
`OnvifBindingFactory` and the `ServiceMetadataBehavior` configuration all go. The `[ServiceContract]`
interface that was the second type argument is not needed - the implementation class identifies the
service.

`app.UseOnvif()` and `app.UseOnvifEvents(address)` still exist, do nothing, and are marked obsolete.
Delete the calls.

Several services can now share one address, which is what real devices do, so the separate
`/onvif/media_service` and `/onvif/ptz_service` addresses 0.9.x needed are no longer necessary.

### Implementations

Your class still derives from the same `XxxBase` and overrides the same operations. Three changes.

**1. Drop the CoreWCF import and the message-parameter attributes.**

```cs
- using CoreWCF;
+ using SharpOnvifServer.Dispatch;
…
- [return: MessageParameter(Name = "Capabilities")]
```

**2. Every operation returns its response type.** This touches every override:

```cs
- public override Capabilities GetServiceCapabilities()
-     => new Capabilities { EFlip = true };
+ public override GetServiceCapabilitiesResponse GetServiceCapabilities()
+     => new GetServiceCapabilitiesResponse(new SharpOnvifServer.PTZ.Capabilities { EFlip = true });

- public override void AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
- { … }
+ public override AbsoluteMoveResponse AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
+ { … return new AbsoluteMoveResponse(); }
```

Each response type has a constructor taking its members. The compiler finds every one of these for
you: the signature no longer matches the base, so the `override` fails to compile.

`XxxBase` is `abstract` now. If you instantiated one rather than deriving from it, derive from it.

**3. `OperationContext` becomes `OnvifOperationContext`.**

```cs
- Uri endpointUri = OperationContext.Current.IncomingMessageProperties.Via;
+ Uri endpointUri = OnvifOperationContext.RequestUri;
```

`OnvifOperationContext.Current` is the `HttpContext` if you need more than the address.

### Event subscriptions are identified by a token

A subscription is addressed by ID alone, and the IDs were sequential integers, so any client that
could reach the endpoint could reach every other client's subscription by counting. They are
unguessable strings now:

```cs
- int  AddSubscription(T subscription);
- T    GetSubscription(int subscriptionID);
- void RemoveSubscription(int subscriptionID);
+ string AddSubscription(T subscription);
+ T      GetSubscription(string subscriptionID);
+ void   RemoveSubscription(string subscriptionID);
```

```cs
- int subscriptionID = (int)httpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID];
+ string subscriptionID = httpContext.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] as string;
```

### One `DigestAuthentication` instead of two

The client and the server each declared an enum of that name, with the same members and the same
values, so any code touching both sides needed aliases. There is one now, in
`SharpOnvifCommon.Security`. `DigestAuthenticationSchemeOptions` itself has not moved.

```cs
  using SharpOnvifServer.Security;
+ using SharpOnvifCommon.Security;   // for DigestAuthentication
```

Configuration that sets it numerically - `"Authentication": 3` in appsettings - is unaffected.

### HTTP Digest helpers

Only a server that calls these directly is affected; the authentication handler calls them for you.

```cs
- int result = HttpDigestAuthentication.ValidateServerNonce(algorithm, type, nonce, nc, now, …);
+ int result = await HttpDigestAuthentication.ValidateServerNonceAsync(algorithm, type, nonce, nc, now, …);
```

The `NoncePrivateKey` field is gone - it made the key that signs every nonce writable by anything
in the process. Use `RegenerateNoncePrivateKey()`, or `SetNoncePrivateKey(byte[])` where several
instances must agree on one.

### Unchanged

`IUserRepository`, `AddOnvifDigestAuthentication`, `DigestAuthenticationSchemeOptions`,
`AddOnvifDiscovery`, `OnvifDiscoveryOptions`, `SharpOnvifServer.Events.IEventSource` and the
`IServer.GetHttpEndpoint` helpers keep their shapes: nothing you already set on them has moved or
changed meaning.

## 6. Behaviour that changed without a signature changing

These compile as they did and behave differently, because they were wrong.

- **`OnvifHelpers.FromTimeout`** read a duration ending in `S` as minutes, so `PT60S` came back as
  an hour. Every subscription lifetime computed from a relative termination time was sixty times
  too long. **If you asked for a very short lifetime and it worked, it worked by accident** - ask
  for a usable one and renew it, and believe the `TerminationTime` the device reports rather than
  what you requested. `PT1M30S` and `PT1H` now parse instead of throwing.
- **`OnvifHelpers.StringToDateTime`** returned local time for a value carrying a zone, and parsed
  in the current culture. It returns UTC and parses invariantly. `DateTimeToString` converts to UTC
  rather than labelling a local time with `Z`.
- **`OnvifEvents.IsMotionDetected`**, and its tamper and sound counterparts, read the state from the
  notification's named data item - `IsMotion`, `IsTamper`, `IsSoundDetected` - rather than from any
  true value anywhere in the message. A camera reporting `IsMotion=false` beside an enabled rule
  used to report motion. **If you relied on the old reading, you were reading a false alarm.**
  These also return null instead of throwing for a notification missing its topic or message.
- **The server bounds what it reads.** A request over `OnvifEndpoint.MaxRequestBytes` (2 MB) is
  refused with a fault, and a document nested deeper than `OnvifXmlReader.MaxDepth` (256) is
  refused. Both are far above anything Onvif describes, and both are settable.
