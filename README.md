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
