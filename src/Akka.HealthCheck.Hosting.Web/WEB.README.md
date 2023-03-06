# Akka.HealthCheck.Hosting.Web

This package integrates `Akka.HealthCheck`, `Akka.Hosting`, and `Microsoft.AspNetCore.Diagnostics.HealthChecks`, allowing users to access `Akka.HealthCheck` via HTTP REST API.

## Available Akka.NET Probes

The package provides 5 `IHealthCheck` probes that can be registered with the health check middleware, each uniquely tagged so they can be individually filtered during mapping:
1. `AkkaReadinessProbe` - The default readiness probe. The probe reports the time the `ActorSystem` was started. 
 
   **Tags:** [ "akka", "ready", "node" ]

2. `AkkaLivenessProbe` - The default liveness probe. The probe reports the time the `ActorSystem` was started. 
  
   **Tags:** [ "akka", "live", "node" ]
   
3. `AkkaClusterReadinessProbe` - Readiness probe for clustering. 
   * Reports healthy when: 
      * The `ActorSystem` joined a cluster. 
      * The `ActorSystem` is connected to a cluster 
   * Reports unhealthy when:
      * The `ActorSystem` just started has not joined a cluster.
      * All other nodes in the cluster is unreachable.

   **Tags:** [ "akka", "ready", "cluster" ]

4. `AkkaClusterLivenessProbe` - Liveness probe for clustering.
    * Reports healthy when:
        * The `ActorSystem` joined a cluster.
        * The `ActorSystem` is connected to a cluster
    * Reports unhealthy when:
        * The `ActorSystem` just started and has not joined a cluster.
        * The `ActorSystem` left the cluster.

   **Tags:** [ "akka", "live", "cluster" ]

5. `AkkaPersistenceLivenessProbe` - Liveness probe for persistence. It probes the persistence storage every second to check that persistence is working properly.
    * Reports healthy when persistence successfully recover both journal and snapshot data from storage.
    * Reports unhealthy when:
        * Persistence just started and has not recovered.
        * Either journal or snapshot failed recovery inside the periodic check.

   **Tags:** [ "akka", "live", "persistence" ]

## Installation

There are 3 steps that needs to be done to integrate `Akka.HealthCheck` with diagnostic health check

- Register health check probes to the health check middleware service.
- Add `Akka.HealthCheck` to the `ActorSystem`.
- Map the health check probe routes.

### Register `Akka.HealthCheck` Services With HealthCheck 

The convenience `IServiceCollection` extension method `WithAkkaHealthCheck(HealthCheckType)` can be used to register the standard probes in any combination.

```csharp
var webBuilder = WebApplication.CreateBuilder(args);
webBuilder.Services
    .WithAkkaHealthCheck(HealthCheckType.All);
```

As alternative, individual probes can be registered using these methods:
- `WithAkkaLivenessProbe()`
- `WithAkkaReadinessProbe()`
- `WithAkkaClusterLivenessProbe()`
- `WithAkkaClusterReadinessProbe()`
- `WithAkkaPersistenceLivenessProbe()`

### Add `Akka.HealthCheck` To The `ActorSystem`

The convenience `AkkaConfigurationBuilder` extension method `WithWebHealthCheck(IServiceProvider)` automatically scans for any registered probes inside the health check middleware and adds the respective Akka.NET health check probes to the `ActorSystem`

```csharp
var webBuilder = WebApplication.CreateBuilder(args);
webBuilder.Services
    .WithAkkaHealthCheck(HealthCheckType.All)
    .AddAkka("actor-system", (builder, serviceProvider) =>
    {
        // Automatically detects which health checks were registered 
        // inside the health check middleware and starts them
        builder.WithWebHealthCheck(serviceProvider);
    });
```

### Map The Health Check Probe Routes

The convenience `IEndpointRouteBuilder` extension method `MapAkkaHealthCheckRoutes` automatically scans for any registered probes inside the health check middleware and maps all the probes to a HTTP route. The HTTP route is the concatenation of the probe tags. By default:

- `AkkaReadinessProbe` is mapped to "/{prefix}/akka/ready/node"
- `AkkaLivenessProbe` is mapped to "/{prefix}/akka/live/node"
- `AkkaClusterReadinessProbe` is mapped to "/{prefix}/akka/ready/cluster"
- `AkkaClusterLivenessProbe` is mapped to "/{prefix}/akka/live/cluster"
- `AkkaPersistenceLivenessProbe` is mapped to "/{prefix}/akka/live/persistence"
- All liveness probes can be queried all at once at "/{prefix}/akka/live"
- All readiness probes can be queried all at once at "/{prefix}/akka/ready"
- All Akka.NET probes can be queried all at once at ""/{prefix}/akka"

```csharp
var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services
    // Register all of the health check service with IServiceCollection
    .WithAkkaHealthCheck(HealthCheckType.All) 
    .AddAkka("actor-system", (builder, serviceProvider) =>
    {
        builder
            // Automatically detects which health checks were registered 
            // inside the health check middleware and starts them
            .WithWebHealthCheck(serviceProvider);
    });

var app = webBuilder.Build();

// Automatically detects which health checks were registered inside 
// the health check middleware and maps their routes
app.MapAkkaHealthCheckRoutes();

await app.RunAsync();
```
## HTTP Response

By default, the health check middleware outputs a simple string response of either "healthy" or "unhealthy" regardless of the number of probes being queried. To more verbose response can be gained by using `Helper.JsonResponseWriter` as the route endpoint response writer.

```csharp
app.MapAkkaHealthCheckRoutes(
    optionConfigure: opt =>
    {
        // Use a custom response writer to output a json of all reported statuses
        opt.ResponseWriter = Helper.JsonResponseWriter;
    });
```

Example output when all probes are enabled:
```json
{
  "status": "Healthy",
  "results": {
    "akka-liveness": {
      "status": "Healthy",
      "description": "Akka.NET node is alive",
      "data": {
        "message": "Live: 12/16/2022 9:54:28 PM +00:00"
      }
    },
    "akka-readiness": {
      "status": "Healthy",
      "description": "Akka.NET node is ready",
      "data": {
        "message": "Live: 12/16/2022 9:54:28 PM +00:00"
      }
    },
    "akka-cluster-liveness": {
      "status": "Healthy",
      "description": "Akka.NET cluster is alive",
      "data": {
        "message": ""
      }
    },
    "akka-cluster-readiness": {
      "status": "Healthy",
      "description": "Akka.NET cluster is ready",
      "data": {
        "message": ""
      }
    },
    "akka-persistence-liveness": {
      "status": "Healthy",
      "description": "Akka.NET persistence is alive",
      "data": {
        "message": "RecoveryStatus(JournalRecovered=True, SnapshotRecovered=True)"
      }
    }
  }
}
```

## Manually Setup Custom Akka.NET `IProbeProvider` With Health Check Middleware

To manually setup a custom `IProbeProvider`, check the [custom probe example project](https://github.com/petabridge/akkadotnet-healthcheck/tree/dev/src/Akka.HealthCheck.Hosting.Web.Custom.Example).

Documentation on how to set up ASP.NET Core health check can be read [here](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)