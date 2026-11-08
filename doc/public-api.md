# Public API

What the libraries of SharpOnvif expose is written down, and a removal from it does not build until
it is written down too, an addition warns until it is. The libraries are SharpOnvifCommon,
SharpOnvifClient and SharpOnvifServer, and two checks run on every `dotnet build` of each:

- **The API files.** `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` next to each project list
  its public types and members, the generated contracts, clients and services among them. They are
  the same on every target framework of the library. Shipped is the API as released; Unshipped is
  what changed since. A public symbol missing from them is a warning, RS0016, until it is listed - an
  addition breaks no one - and a listed one that is gone fails the build with RS0017.
- **Package validation.** Each package being built is compared with the last release
  (`PackageValidationBaselineVersion` in each project, 0.11.4 now), and every binary or source
  breaking change fails the build with a CP error: a member added to an interface or an abstract
  class among them, which breaks whoever implements it. It also checks that the target frameworks
  agree: what netstandard2.0 offers, net481, net8.0 and net10.0 have to offer too.

## Adding to the API

List what the RS0016 warnings name: accept their fix in the IDE ("Add to public API"), or from the
command line:

```
dotnet format analyzers src/SharpOnvifClient/SharpOnvifClient.csproj --diagnostics RS0016 --severity warn
```

The new lines land in that project's `PublicAPI.Unshipped.txt`. Regenerating the bindings
(doc/codegen.md) changes the API of the contracts, clients and services it rewrites, and is listed
the same way: read the diff of the API files along with the diff of the sources.

## Removing or changing the API

RS0017 names what disappeared. Moving its line from Shipped to Unshipped with a `*REMOVED*` prefix
records the removal; a member whose signature changed is a removal of the old line and an addition
of the new one.

If package validation then reports a CP error, the change breaks code built against another
framework or the last release. When that is intended, record it:

```
dotnet build src/SharpOnvifClient/SharpOnvifClient.csproj -c Release -p:ApiCompatGenerateSuppressionFile=true
```

That writes `CompatibilitySuppressions.xml` next to the project. Say in the pull request what broke.

Validation runs as part of the build, because the build packs. `dotnet pack` on its own does not
rebuild and validates whatever binaries are already in `bin`.

## Releasing

1. Move every line of each library's `PublicAPI.Unshipped.txt` into the `PublicAPI.Shipped.txt`
   beside it, dropping the `*REMOVED*` lines together with the shipped lines they remove.
2. Once the release is on nuget.org, set `PackageValidationBaselineVersion` in each library's
   project to it, and delete the `CompatibilitySuppressions.xml` files: what they excused is in the
   new baseline.
