# WSDL code generation

`src/WsdlGenerator` compiles WSDL and XML Schema into C# clients and services. It is not specific
to ONVIF: it generates from any document/literal SOAP 1.2 WSDL, and everything particular to ONVIF
is in `onvif.codegen.json` at the root of this repository.

Generated sources are committed, so a normal build runs no generator and needs no network access.

## Regenerate this repository's bindings

```
dotnet run --project src/WsdlGenerator
```

Reads `onvif.codegen.json` and rewrites the generated sources in place. Review the diff afterwards.

To pick up upstream specification changes, run `wsdl/fetch.sh` first. It re-downloads every
document in `wsdl/sources.txt`, mirroring the remote URL layout under `wsdl/` so that relative
`schemaLocation` and `location` references resolve offline.

## Generate from another WSDL

```
dotnet run --project src/WsdlGenerator -- \
    --wsdl ./bank.wsdl \
    --namespace Example.Banking \
    --out ./Generated \
    --client
```

Produces `Generated/Bank/{DataContracts,Client}.cs` in `Example.Banking.Bank`, the types its
schemas share in `Example.Banking.Schema`, and the runtime in `Example.Banking.Runtime`.

The service name comes from the file name. Write `--wsdl Accounts=./bank.wsdl` to choose one, and
repeat `--wsdl` for several services, which then share their common schemas.

Describe anything larger in a configuration file and pass `--config`.

## Command-line options

| Option | Description |
| --- | --- |
| `--config <file>` | Read the whole run from a file. Paths in it are relative to it. |
| `--wsdl <uri>` | A WSDL, as a file path or an `http(s)` URL. Repeatable. `<name>=<uri>` names the service. |
| `--namespace <ns>` | Root namespace. A service is generated into `<ns>.<Service>`. |
| `--out <dir>` | Output directory. A service is generated into `<dir>/<Service>`. |
| `--shared-namespace <ns>` | Namespace for types the services share. Default: `<ns>.Schema`. |
| `--shared-out <dir>` | Directory for those types. Default: `<dir>/Schema`. |
| `--runtime-namespace <ns>` | Namespace of the runtime. Default: `<ns>.Runtime`. |
| `--runtime-out <dir>` | Directory for the runtime. Default: `<dir>/Runtime`. |
| `--no-runtime` | Do not write the runtime. `--runtime-namespace` names an existing one. |
| `--settings <type>` | Type a client constructs when given no settings. Implements `IClientSettings`. |
| `--dispatch <ns>` | Namespace a generated service is routed by. Required with `--server`. |
| `--client` / `--server` | Generate one side only. Both by default. |
| `--mirror <dir>` | Resolve every document from a local mirror instead of disk and the network. |
| `--enum-value <t>=<v>` | Add a value to a schema enumeration. The type is written `{namespace}LocalName`. Repeatable. |
| `--help`, `-h` | Show usage. |

`--wsdl` and `--config` are alternatives. Given neither, the generator reads `onvif.codegen.json`
from the repository root.

## Configuration file

A run of any size is described in JSON. Paths are relative to the file.

```json
{
  "mirror": "wsdl",
  "typeNamePrefix": "Onvif",
  "shared":  { "namespace": "SharpOnvifCommon.Onvif", "out": "src/SharpOnvifCommon/Generated" },
  "runtime": { "namespace": "SharpOnvifCommon",       "out": "src/SharpOnvifCommon/Generated/Runtime" },
  "settings": "SharpOnvifCommon.Soap.OnvifClientSettings",
  "dispatch": "SharpOnvifServer.Dispatch",
  "targets": [
    { "namespace": "SharpOnvifClient", "out": "src/SharpOnvifClient/Generated", "client": true },
    { "namespace": "SharpOnvifServer", "out": "src/SharpOnvifServer/Generated", "server": true }
  ],
  "services": [
    { "name": "DeviceMgmt", "wsdl": "https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl" }
  ],
  "enumerationValues": [
    { "type": "{http://www.onvif.org/ver10/schema}VideoEncoding", "value": "H265",
      "documentation": "H.265 / HEVC. Sent by devices; not listed by onvif.xsd." }
  ]
}
```

| Property | Description |
| --- | --- |
| `mirror` | Directory to resolve every document from. Omit to read from disk and the network. |
| `typeNamePrefix` | Prefix for a type whose schema name collides with a framework type. Omit to leave such names alone. |
| `shared` | `namespace` and `out` for the types the services share. Required. |
| `runtime` | `namespace` and `out` for the runtime. Omit `out` to compile against one that already exists. Required. |
| `settings` | Type a client constructs when given no settings. Omit and a client only ever takes settings it is handed. |
| `dispatch` | Namespace a generated service is routed by. Required for any target with `"server": true`. |
| `targets` | Where the per-service code goes: `namespace`, `out`, and `client` or `server`. Required. |
| `services` | The WSDLs: `name` and `wsdl` each. Names must be unique. Required. |
| `enumerationValues` | Values to add to a schema enumeration: `type` as `{namespace}LocalName`, `value`, and optional `documentation`. |
| `$comment` | Ignored, for a note at the top of the file. Any other unknown key is an error naming the key. |

### Remarks

A published schema can trail the implementations that follow it. `enumerationValues` adds a value
the schema does not list, and the conversions generated beside the enumeration stay in step with
it, which editing the output would not. Values are appended, so the number behind an existing name
does not move.

## Output

| Path | Contents |
| --- | --- |
| `<shared.out>/DataContracts.cs` | Types the services share, generated once. |
| `<target.out>/<Service>/DataContracts.cs` | Types that service's own WSDL declares. |
| `<target.out>/<Service>/Client.cs` | One interface and one client per portType. |
| `<target.out>/<Service>/Service.cs` | One base class per portType, and its dispatcher. |
| `<runtime.out>/**` | The runtime, below. |

## The runtime

Generated code is compiled against a runtime, and that runtime is generated with it. It is written
rather than referenced so that generated code depends on no library: the output of `--client`
compiles in a project with no references at all, which `TestGeneratedCodeCompilesAlone` verifies by
compiling it that way.

| File | Contents |
| --- | --- |
| `ILog.cs` | Where a client reports what it could not do. |
| `Soap/IClientSettings.cs` | What a client was built with. |
| `Soap/IClientAuthentication.cs` | How a client proves who it is. |
| `Soap/IMessageCodec.cs` | What an envelope is. |
| `Xml/IXmlWriter.cs`, `Xml/IXmlReader.cs` | How a contract writes and reads itself, and the lexical conversions. |
| `Soap/SoapClientBase.cs` | The base class a generated client derives from. |
| `Xml/XmlContract.cs` | The base class every generated contract derives from. |
| `Xml/SoapFault.cs`, `Soap/SoapTransportException.cs`, `Xml/XmlNamespaceDeclaration.cs` | What a caller catches and inspects. |

Nothing that implements those interfaces is generated. ONVIF's implementations are hand-written in
`SharpOnvifCommon`: `OnvifXmlReader`, `OnvifXmlWriter`, `SoapEnvelope`, `SoapMessageCodec`,
`OnvifClientSettings`, `OnvifAuthenticationSettings` and the loggers.

### Remarks

Generated code **compiles** against nothing and does not **run** against nothing. Generating for a
service of your own means bringing an implementation of the runtime interfaces or writing one;
`SharpOnvifCommon`'s `Xml/` is about 1,300 lines of it.

`IClientSettings` is the small end, and `TestGeneratedCodeCompilesAlone` compiles this example:

```cs
internal sealed class BankSettings : IClientSettings
{
    public IMessageCodec Codec { get { return new BankCodec(); } }   // yours: the envelope
    public NetworkCredential Credentials { get { return null; } }
    public ILog Logger { get { return null; } }
    public IClientAuthentication Authentication { get { return null; } }   // sends no credentials
    public TimeSpan UtcNowOffset { get { return TimeSpan.Zero; } }
    public TimeSpan Timeout { get { return TimeSpan.FromSeconds(30); } }
    public bool DisableExpect100Continue { get { return true; } }
    public long MaxResponseContentBytes { get { return 16L * 1024 * 1024; } }
    public HttpMessageHandler Transport { get { return null; } }
    public HttpClient HttpClient { get { return null; } }
    public IEnumerable<XmlNamespaceDeclaration> EnvelopePrologue { get { return null; } }
}
```

A null `Authentication` sends no credentials and a null `Logger` reports nowhere. A null `Codec` is
refused: a client has no idea what an envelope looks like.

An implementation of `IXmlReader` or `IXmlWriter` must be in the same assembly as the emitted
runtime, because it drives hooks on `XmlContract` that are internal to it. Generated contracts need
not be: they derive from `XmlContract` and can live anywhere, which is how `SharpOnvifClient` and
`SharpOnvifServer` carry their own while the runtime sits in `SharpOnvifCommon`. Point
`--runtime-out` at the project that will implement it.

A generated service also names the dispatch it is routed by, `--dispatch`. That is the only thing a
generated service names that is not generated: routing an action to a method over ASP.NET Core is a
library rather than anything a schema describes.

## Operation shapes

Each operation takes a generated request contract and returns a generated response contract.

```cs
Task<GetServicesResponse> GetServicesAsync(GetServicesRequest request, CancellationToken ct = default);
```

An overload takes the request's members as arguments, for the common case where building the
request adds nothing. The two never collide: they differ in arity or in parameter type.

```cs
Task<GetServicesResponse> GetServicesAsync(bool IncludeCapability, CancellationToken ct = default);
```

Each operation appears three times on a generated service base, each layer defaulting to the next.
Override whichever suits the implementation.

```cs
public virtual Task<GetServicesResponse> GetServicesAsync(GetServicesRequest request, CancellationToken ct);
public virtual GetServicesResponse GetServices(GetServicesRequest request);
public virtual GetServicesResponse GetServices(bool IncludeCapability);
```

The dispatcher calls the first. An operation nothing overrides throws `NotImplementedException`,
which the endpoint reports as the `ter:ActionNotSupported` fault.

## Type names

A generated type whose name matches one already in scope for a consumer would make both ambiguous.
With implicit usings `System` is always in scope, so a contract named `DateTime` would be ambiguous
in any file that also imports the generated namespace.

`typeNamePrefix` prefixes those names. The schema name is untouched, so nothing moves on the wire:
`OnvifDateTime` still serializes as `DateTime`. Eight ONVIF names need it today - `Action`,
`Attribute`, `DateTime`, `IPAddress`, `NetworkInterface`, `Object`, `Scope` and `TimeZone` - and
`CsharpNaming.FrameworkTypeNames` lists a wider set, so a future revision introducing a `Stream` or
a `Task` does not reintroduce the problem.

`SharpOnvifClient.Tests`'s `TestNamespaceCoexistence` imports every relevant framework and ONVIF
namespace at once with no aliases. It exists to be compiled: a name that collided would break the
build.

## Supported XML Schema

The generator models the slice of XML Schema that service specifications use. Anything outside it
raises a `SchemaException` naming the file and line rather than emitting wrong code.

**Supported**: `sequence`, `choice`, `any`, `anyAttribute`, `complexContent` extension,
`simpleContent` extension, `simpleType` restriction, list and union, enumerations, mixed content,
abstract types, `attributeGroup`, `import` and `include`.

**Not supported**: substitution groups, `xs:group`, `xs:all`, `complexContent` restriction,
`redefine`, `notation`.

Only document/literal SOAP 1.2 bindings are accepted.

Only types an operation can put on the wire are generated. A type a schema declares but that no
operation reaches is not.
