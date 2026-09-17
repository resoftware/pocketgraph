# PocketGraph

A Neo4j and Memgraph client for iPhone and iPad, built with .NET MAUI.

**[pocketgraph.app](https://pocketgraph.app)** 

[![Download on the App Store](https://tools.applemediaservices.com/api/badges/download-on-the-app-store/black/en-us?size=250x83&amp;releaseDate=1280278400)](https://apps.apple.com/nl/app/pocketgraph/id1604368926)

## Features

- Connect via the Bolt protocol with SSL/TLS support
- Compatible with Neo4j and Memgraph
- Interactive graph visualization with expand/collapse
- Table view for query results
- Save and manage connections and queries
- Light and dark theme
- iPhone and iPad support (iOS 14.2+)

## Project Structure

```
Xamarin.Neo4j/
├── Xamarin.Neo4j/                    # Shared .NET MAUI project
├── Xamarin.Neo4j.iOS/                # iOS platform project
├── Xamarin.Neo4j.Tests/              # Unit tests
└── Xamarin.Neo4j.IntegrationTests/   # Integration tests (Neo4j + Memgraph via Docker)
```

## Development

### Prerequisites
- .NET 10 SDK
- Xcode (for iOS builds)
- Docker (for integration tests)

### Building

```bash
cd Xamarin.Neo4j
dotnet build Xamarin.Neo4j.sln
```

### Running Tests

```bash
# Unit tests
dotnet test Xamarin.Neo4j/Xamarin.Neo4j/Xamarin.Neo4j.Tests/

# Integration tests (requires Docker)
dotnet test Xamarin.Neo4j/Xamarin.Neo4j/Xamarin.Neo4j.IntegrationTests/
```

### Android DEX shrinking & obfuscation (R8)

Release builds of the Android head run R8 over the Java/DEX side
(`AndroidLinkTool=r8` in `Xamarin.Neo4j.Android.csproj`). Play Console's *DEX code
optimization* report scores obfuscation at ~1% on a stock .NET MAUI build: .NET for
Android runs no DEX shrinker by default, and even with R8 on, the ProGuard config the
SDK generates hardcodes `-dontobfuscate` — a flag nothing later in the config list can
undo. The `_AndroidObfuscateDex` target therefore drops that file from R8's `--pg-conf`
list and substitutes `Xamarin.Neo4j.Android/proguard_xamarin.cfg` (the same file minus
that line), plus a generated file carrying the `-printmapping`/`-keepattributes` tail
the SDK would have appended. Measured on the Release APK: **49.8% of DEX classes
renamed, up from ~0%**.

Only library-internal classes are renamed: the trimmer emits a `-keep` rule for every
Java type the managed bindings reference, aapt2 one for every class named in a layout
or the manifest, and each Android Callable Wrapper gets its own.
`Xamarin.Neo4j.Android/proguard.cfg` covers what those miss — things resolved by name
from native code. Add to it if a Release build dies with `ClassNotFoundException`,
`NoSuchFieldError` or `NoSuchMethodError` where a Debug build does not.

`mapping.txt` is embedded in the AAB
(`BUNDLE-METADATA/com.android.tools.build.obfuscation/proguard.map`), so Play Console
retraces obfuscated Java stacks by itself. Managed (C#) stack traces are unaffected —
R8 only touches DEX.

Local Release builds must be clean: aapt2's keep rules are registered as a `FileWrite`,
so an incremental build that skips the resource link has `IncrementalClean` delete them.
The target caches a copy outside `FileWrites` and errors out if even that is missing —
delete `Xamarin.Neo4j.Android/obj/Release` if it fires.

## Technology Stack

- .NET MAUI
- Neo4j.Driver (Bolt protocol)
- C# / XAML
- MVVM architecture
- Testcontainers for integration testing

## Privacy

PocketGraph does not collect any user data. All connections and queries are stored locally on your device.

## About

PocketGraph is a product of [Re: Software B.V.](https://resoftware.nl).

Neo4j and Cypher are registered trademarks of Neo4j, Inc.

## License

See [LICENSE](LICENSE) for details.

## Support

Contact: support@resoftware.nl
