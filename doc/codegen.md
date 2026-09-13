# SharpOnvif code generation

SharpOnvif no longer uses WCF, CoreWCF, or `svcutil`. The Onvif service bindings are produced by
`src/WsdlGenerator`, a purpose-built WSDL/XSD compiler in this repository.

Output goes to four places:

```
src/SharpOnvifCommon/Generated/Runtime/**/*.cs               namespace SharpOnvifCommon(.Soap|.Xml)
src/SharpOnvifCommon/Generated/DataContracts.cs              namespace SharpOnvifCommon.Onvif
src/SharpOnvifClient/Generated/<Service>/DataContracts.cs    namespace SharpOnvifClient.<Service>
src/SharpOnvifClient/Generated/<Service>/Client.cs
src/SharpOnvifServer/Generated/<Service>/DataContracts.cs    namespace SharpOnvifServer.<Service>
src/SharpOnvifServer/Generated/<Service>/Service.cs
```

The Onvif data model lives in `SharpOnvifCommon.Onvif` and is generated once. Each service gets
only the types its own WSDL declares, plus its operations.

## The runtime

The generated code is compiled against a runtime, and that runtime is generated too, from source
embedded in the generator under `src/WsdlGenerator/Runtime/`, into the namespace and directory a
run chooses. It is written rather than referenced so that a generated client depends on nothing
but itself: generate from somebody else's WSDL and the output compiles in a project with no
references at all.

What is in it is deliberately narrow - the base class a client derives from, the plumbing the
generated contracts drive directly, and the interfaces the base class talks through:

```
ILog                        where a client says what it could not do
Soap/IClientSettings        what a client was built with
Soap/IClientAuthentication  how a client proves who it is
Soap/IMessageCodec          what an envelope is
Xml/IXmlWriter              how a contract writes itself
Xml/IXmlReader              how a contract reads itself
Soap/SoapClientBase        the base class, which knows only those
Xml/OnvifContract           the base every generated contract derives from
Xml/SoapFault, Xml/XmlNamespaceDeclaration, Soap/SoapTransportException
```

Nothing that implements any of it is generated. HTTP Digest, the WS-Security UsernameToken, the
nonce replay store, the loggers, the settings that carry them, and the whole XML layer - reader,
writer, envelope, fault parsing, lexical conversions - are ordinary hand-written source in
`SharpOnvifCommon`, under `Security/`, `Soap/`, `Logging/` and `Xml/`.

Nothing service-specific is emitted either: a run writes the same runtime whatever it generates
from. The lexical conversions sit on the reader and writer interfaces rather than in a class of
their own, because a contract always has one of those to hand while it is reading or writing, and
anywhere else would mean naming a type the generated code is not supposed to know.

One consequence is worth being plain about: generated code **compiles** against nothing, but it
does not **run** against nothing. A client with no `IXmlWriter` behind it cannot write a message.
Generating for a service of your own means bringing a runtime implementation - `SharpOnvifCommon`
is one, at about thirteen hundred lines of `Xml/` - or writing one.

The embedded source is ordinary C# and stays compilable in an editor: it is written in a namespace
called `__RUNTIME__`, which the generator replaces.

One name reaches the generated clients from the run: `--settings <type>`, something implementing
`IClientSettings` that a client can construct when it is handed none. This repository names
`SharpOnvifCommon.Soap.OnvifClientSettings`, which is what keeps `new DeviceClient(uri, user,
password)` working. Name nothing and a client only ever takes settings it is given, because there
is nothing it could have built them from.

Change the runtime by editing
`src/WsdlGenerator/Runtime/` and regenerating - editing the emitted copy loses the change on the
next run. The generator writes files but never deletes them, so a file that stops being emitted
has to be removed by hand; `TestCodeGenerator` compares the committed runtime against what a run
would write, and fails when one is left behind.

## Running the generator

```
dotnet run --project src/WsdlGenerator
```

With no arguments it reads the offline schema mirror in `wsdl/` and rewrites this repository's
generated sources in place.
Generated files are committed, so a normal build never runs the generator and never needs
network access. Review the diff whenever you regenerate.

To pick up upstream specification changes, run `wsdl/fetch.sh` first. It re-downloads every
document listed in `wsdl/sources.txt`, mirroring the remote URL layout under `wsdl/` so that
relative `schemaLocation` and `location` references resolve offline.

## Using it for other services

Nothing in the compiler is specific to Onvif - it reads WSDL and XML Schema - so it will generate
from any document/literal SOAP 1.2 service:

```
dotnet run --project src/WsdlGenerator -- \
    --wsdl ./bank.wsdl \
    --namespace Example.Banking \
    --out ./Generated
```

That produces `Generated/Bank/{DataContracts,Client,Service}.cs` in `Example.Banking.Bank`, the
types its schemas share in `Example.Banking.Schema`, and the runtime under it in
`Example.Banking.Runtime`. The service name comes from the file
name; write `--wsdl Accounts=./bank.wsdl` to choose one. Repeat `--wsdl` for several services, and
they share their common schemas the same way the Onvif services do.

| option | |
| --- | --- |
| `--wsdl <uri>` | A WSDL, as a file path or an http(s) URL. Repeatable. `<name>=<uri>` names the service. |
| `--namespace <ns>` | Root namespace; a service lands in `<ns>.<Service>`. |
| `--out <dir>` | Output directory; a service lands in `<dir>/<Service>`. |
| `--shared-namespace <ns>` | Namespace for shared types. Defaults to `<ns>.Schema`. |
| `--shared-out <dir>` | Directory for shared types. Defaults to `<dir>/Schema`. |
| `--runtime-namespace <ns>` | Namespace of the runtime. Defaults to `<ns>.Runtime`. |
| `--runtime-out <dir>` | Directory for the runtime. Defaults to `<dir>/Runtime`. |
| `--no-runtime` | Do not write the runtime; compile against the one `--runtime-namespace` names. |
| `--settings <type>` | What a client builds its settings from when handed none. |
| `--client` / `--server` | Generate one side only. Both by default. |
| `--mirror <dir>` | Resolve every document from a local mirror rather than from disk and the network. |

Relative `xs:import` and `wsdl:import` references resolve against the document that made them, so
a WSDL on disk can pull in schemas beside it and one fetched over http can pull in its siblings.
Pass `--mirror` to keep a run offline and reproducible, which is how this repository generates its
own bindings.

A generated client depends on nothing but the runtime written beside it. A generated service
additionally depends on `SharpOnvifServer`, which carries the ASP.NET Core dispatch it is routed
by; that is not Onvif-specific either, but unlike the runtime it is a library rather than
something the generator writes. Generate with `--client` for output that references nothing at
all.

Run the generator twice into one solution - a second service set, say - with `--no-runtime` on the
second run and `--runtime-namespace` naming the first one's, so the two share a runtime instead of
each emitting a copy.

`SharpOnvifCommon.Tests`'s `TestCodeGenerator` runs the generator over a small banking WSDL to keep this
path working.

## Schema subset

ONVIF uses a narrow slice of XML Schema, and the generator models exactly that slice. Anything
outside it raises a `SchemaException` naming the file and line rather than silently emitting
wrong code, so a future specification revision fails loudly instead of quietly.

Supported: `sequence`, `choice`, `any`, `anyAttribute`, `complexContent` extension,
`simpleContent` extension, `simpleType` restriction/list/union, enumerations, mixed content,
abstract types, `attributeGroup`, `import`/`include`.

Deliberately unsupported, because nothing in the mirror uses them: substitution groups,
`xs:group`, `xs:all`, `complexContent` restriction, `redefine`, `notation`.

Every SOAP binding in ONVIF is document/literal over SOAP 1.2, and the generator rejects
anything else.

## Operation shapes

Every operation is generated in message-contract style: it takes a generated request contract and
returns a generated response contract.

```cs
Task<GetServicesResponse> GetServicesAsync(GetServicesRequest request, CancellationToken ct = default);
```

A convenience overload takes the request's members as arguments instead, for the common case where
building the request adds nothing:

```cs
Task<GetServicesResponse> GetServicesAsync(bool IncludeCapability, CancellationToken ct = default);
```

Both return the same contract. The overloads never collide, because they differ in arity or in
parameter type, and an operation with no inputs still gets the no-argument form.

The server side mirrors this. Each operation appears three times on the generated base class, each
layer defaulting to the next, so an implementation overrides whichever suits it:

```cs
public virtual Task<GetServicesResponse> GetServicesAsync(GetServicesRequest request, CancellationToken ct);
public virtual GetServicesResponse GetServices(GetServicesRequest request);
public virtual GetServicesResponse GetServices(bool IncludeCapability);
```

The dispatcher calls the first. Override the async form for an operation that needs to await, the
middle one to work from the request contract, or the last to take the members directly. An
operation nothing overrides throws `NotImplementedException`, which the endpoint reports as the
`ter:ActionNotSupported` fault the Onvif specification defines.

### Why this differs from the svcutil bindings

`svcutil` rendered some operations in message-contract style and others with the SOAP body
unwrapped into ordinary arguments and a bare return value:

```cs
Task<SystemDateTime> GetSystemDateAndTimeAsync();          // what svcutil generated here
```

Which style an operation got was not consistent. The checked-in bindings were generated by
several `svcutil` and `dotnet-svcutil` invocations over time, and services disagreed with each
other: every AppMgmt operation was message contract while the other 24 services mixed the two, and
the client and server bindings for the same service sometimes disagreed as well. There was no
single rule to reproduce.

Generating one style throughout removes that inconsistency. It does mean an operation svcutil had
unwrapped now returns its response contract, so a call like the one above becomes:

```cs
var response = await client.GetSystemDateAndTimeAsync();
SystemDateTime time = response.SystemDateAndTime;
```

`SimpleOnvifClient` keeps its own convenience shapes and is unaffected.

## Shared and service types

A type belongs to a service when that service's own WSDL declares it: the request and response
contracts, and whatever else sits in the WSDL's target namespace. Everything else comes from a
schema the services share - `onvif.xsd` above all, plus `common.xsd`, the PACS and metadata
schemas, and the OASIS WS-Notification and W3C schemas the event service pulls in.

Shared types are generated once, into `SharpOnvifCommon.Onvif`, and referenced from both sides.
That matters for more than size: `SharpOnvifCommon.Onvif.Profile` is one CLR type, so a value the
client reads can be handed to a server implementation unchanged.

The split is safe because the dependency only runs one way. No type in a shared schema refers to
one declared by a service, so the common assembly needs no reference back - the generator checks
this implicitly by failing to resolve such a reference, and no mirrored schema violates it.

Generating the shared schema in full, rather than only the types some operation reaches, keeps the
common assembly a complete rendering of the Onvif data model. It also removed a per-service
`GenerateEntireSchema` flag that existed solely so the Analytics and DeviceIO assemblies could
publish all of `onvif.xsd` the way svcutil had.

Deduplication took the generated output from 4,188 types to 747 shared plus 1,480 per service, of
which 1,254 are the request and response wrappers for the 627 operations.

## Names that would collide with the framework

A generated type whose name matches one a consumer already has in scope would make both ambiguous
wherever the two namespaces are imported together. With implicit usings, `System` is always in
scope, so a contract called `DateTime` would be ambiguous in any file that also imports
`SharpOnvifCommon.Onvif`.

The generator prefixes those names with `Onvif`. The schema name is untouched, so nothing moves on
the wire: `OnvifDateTime` still serialises as `DateTime`.

Eight names need it today - `Action`, `Attribute`, `DateTime`, `IPAddress`, `NetworkInterface`,
`Object`, `Scope` and `TimeZone`. `CsharpNaming.FrameworkTypeNames` lists a wider set than that,
drawn from the net10.0 reference assemblies for the namespaces a consumer typically imports, so a
future specification revision introducing a `Stream` or a `Task` does not reintroduce the problem.

`SharpOnvifClient.Tests`'s `TestNamespaceCoexistence` imports every relevant framework and Onvif namespace
at once with no aliases. It exists to be compiled: a name that collided would break the build.

## Generated type surface

Only types an operation can put on the wire are generated. Measured against the previously
committed bindings, 22 of the 25 services produce exactly the same set of types, and the
generator additionally emits types that the current schemas added since the old code was
generated.

Two services are exceptions and carry `GenerateEntireSchema` in `ServiceCatalog`: svcutil did not
prune Analytics or DeviceIO, and emitted the whole imported schema closure into them (611 types
for Analytics's 14 operations). The flag keeps those surfaces intact.

A residual 36 types out of roughly 3,800 are named differently from before, all of them cases
where svcutil resolved a name collision with a numeric suffix:

- **Events** keeps the WS-Notification wrapper element types under the operation-derived names
  (`SubscribeRequest` rather than the pair `Subscribe` / `SubscribeRequest`).
- **DeviceIO** and **Analytics** lose a handful of `...1`-suffixed duplicates.
