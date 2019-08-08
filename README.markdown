[![ci.appveyor.com](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/MongoDB.ApplicationInsights?branch=master&svg=true)](https://ci.appveyor.com/api/projects/status/github/blushingpenguin/MongoDB.ApplicationInsights?branch=master&svg=true)
[![codecov.io](https://codecov.io/gh/blushingpenguin/MongoDB.ApplicationInsights/coverage.svg?branch=master)](https://codecov.io/gh/blushingpenguin/MongoDB.ApplicationInsights?branch=master)

# MongoDB.ApplicationInsights

Adds application insights tracking into MongoDB in an easy to use way. There is also a second libary, MongoDB.ApplicationInsights.DependencyInjection which adds convenience functions for configuring the .NET core dependency injection system.

A simple example usage with the dependency injection helpers:

```csharp
using Microsoft.ApplicationInsights;
using MongoDB.ApplicationInsights.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetry();
        services.AddMongoClient("mongodb://localhost:27017/");
    }
}

public class MyClass
{
    private readonly IMongoClient _client;

    public MyClass(IMongoClient client)
    {
        _client = client;
    }
}
```

And without:

```csharp
using Microsoft.ApplicationInsights;
using MongoDB.ApplicationInsights;

public class Program
{
    public static void Main(string[] args)
    {
        var config = new TelemetryConfiguration
        {
            InstrumentationKey = "your-appinsights-key"
        };
        var telemetryClient = new TelemetryClient(config);
        var factory = new MongoClientFactory(telemetryClient);
        var client = factory.GetClient("mongodb://localhost:27017/");
    }
}
```

## MongoApplicationInsightsSettings

The telemetry can be configured by passing an instance of `MongoApplicationInsightsSettings` to any of the functions that ultimately constructs a `MongoClient` instance.

```csharp
    public class MongoApplicationInsightsSettings
    {
        public HashSet<string> FilteredCommands { get; set; };
        public TimeSpan MaxQueryTime { get; set; };
    }
```

`FilteredCommands` names the mongo commands to ignore. By default these are
`buildInfo`, `getLastError`, `isMaster`, `ping`, `saslStart` and `saslContinue`.

`MaxQueryTime` is a fallback to prevent memory leaks if mongo reports that a command has started, but does not later report whether it has succeeded or failed. It is set to 4 hours by default -- if you have queries that run for longer than this time then you will need to increase the setting to prevent telemetry for those queries from being discarded.

## MongoClientFactory

`MongoClientFactory` takes mongo settings, application insights settings and constructs instances of `MongoClient` with the settings applied.

```csharp
public MongoClientFactory(TelemetryClient telemetryClient = null, MongoApplicationInsightsSettings settings = null)
```

The constructor takes an instance of the telemetry client to use, and the telemetry settings. If no telemetry client is supplied, then telemetry is disabled. If no settings are supplied then a default settings object is constructed.

```csharp
IMongoClient GetClient(MongoClientSettings clientSettings);
IMongoClient GetClient(MongoUrl mongoUrl);
IMongoClient GetClient(string connectionString);
```

These functions construct a `MongoClient` instance in the same way as the corresponding `MongoClient` constructor calls, but with telemetry applied (if a telemetry client was supplied to the constructor).

The dependency injection helper will register a singleton `MongoClientFactory`:

```csharp
public static IServiceCollection AddMongoClientFactory(
    this IServiceCollection services,
    MongoApplicationInsightsSettings settings = null
);
````

`MongoApplicationInsightsSettings` will be registered with the dependency injection container (or a default instance will be registered if it is not provided).

## Dependency injection helpers

```csharp
public static IServiceCollection AddMongoClient(
    this IServiceCollection services,
    string connectionString,
    MongoApplicationInsightsSettings settings = null
);
public static IServiceCollection AddMongoClient(
    this IServiceCollection services,
    MongoUrl url,
    MongoApplicationInsightsSettings settings = null
);
public static IServiceCollection AddMongoClient(
    this IServiceCollection services,
    MongoClientSettings clientSettings,
    MongoApplicationInsightsSettings settings = null
);
```

These three helpers register a singleton `MongoClientFactory` instance using the provided settings (if any), and then a singleton `IMongoClient` which will construct a client using the factory and the provided mongo client settings.  There is a short example of using these at the start of this document.

If no TelemetryClient instance is registered in the dependency injection container then telemetry will be disabled.
