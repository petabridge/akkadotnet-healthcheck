# Akka.HealthCheck
A configurable library for exposing Akka nodes inside common healthcheck systems and environments, such as [Kubernetes Liveness and Readiness probes](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/) and [ASP.NET health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).

# Table of contents

- [Core Plugin](#core-plugin)
   - [Built-in Akka.NET Probes](#built-in-akkanet-probes)
   - [Configuring Using `Akka.Hosting`](#configuring-using-akkahosting)
   - [Configuring Using HOCON](#configuring-using-hocon)
- [ASP.NET Integration](#aspnet-integration)
   - [Available Akka.NET Probes](#available-akkanet-probes)
   - [Installation](#installation)
      - [Register `Akka.HealthCheck` Services With HealthCheck](#register-akkahealthcheck-services-with-healthcheck)
      - [Add `Akka.HealthCheck` To The `ActorSystem`](#add-akkahealthcheck-to-the-actorsystem)
      - [Map The Health Check Probe Routes](#map-the-health-check-probe-routes)
   - [HTTP Response](#http-response)
   - [Manually Setup Custom Akka.NET `IProbeProvider` With Health Check Middleware](#manually-setup-custom-akkanet-iprobeprovider-with-health-check-middleware)
- [Building this solution](#building-this-solution)
   - [Conventions](#conventions)
   - [DocFx for Documentation](#docfx-for-documentation)
      - [Previewing Documentation](#previewing-documentation)
   - [Release Notes, Version Numbers, Etc](#release-notes-version-numbers-etc)
   - [Code Signing via SignService](#code-signing-via-signservice)

# Core Plugin
[Back To Top](#akkahealthcheck)

The core plugin is designed to run as standalone health check endpoint for your actor system. Three types of transports are supported:
1. __Custom (default)__

   Used to specify that no built-in transport will be used. Typically users will query / subscribe to the Readiness or Liveness probe actors and pipe the changes in liveness / readiness status out to something like a custom HTTP endpoint.

2. __TCP socket__
    
    This transport mode fulfills a Kubernetes-like style health check endpoint, where liveness/readiness are tested by checking if a pod can accept a socket connection request on a certain port. See the [Kubernetes documentation](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/#define-a-tcp-liveness-probe) for usage example.

3. __File__

    Writes the readiness or liveness status out to disk to a specified file location. Used in combination with liveness checks such as "command line execution" checks. See the [Kubernetes documentation](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/#define-a-liveness-command) for usage example.

## Built-in Akka.NET Probes
[Back To Top](#akkahealthcheck)

There are 5 probe providers that can be used with `Akka.HealthCheck`:
1. `DefaultReadinessProvider` - The default readiness probe. The probe reports the time the `ActorSystem` was started.
    * Available inside the `Akka.HealthCheck` NuGet package. 
2. `DefaultLivenessProvider` - The default liveness probe. The probe reports the time the `ActorSystem` was started.
    * Available inside the `Akka.HealthCheck` NuGet package.
3. `ClusterReadinessProbeProvider` - Readiness probe for clustering.
    * Reports healthy when:
        * The `ActorSystem` joined a cluster.
        * The `ActorSystem` is connected to a cluster
    * Reports unhealthy when:
        * The `ActorSystem` just started has not joined a cluster.
        * All other nodes in the cluster is unreachable.
    - Available inside the `Akka.HealthCheck.Cluster` NuGet package.
4. `ClusterLivenessProbeProvider` - Liveness probe for clustering.
    * Reports healthy when:
        * The `ActorSystem` joined a cluster.
        * The `ActorSystem` is connected to a cluster
    * Reports unhealthy when:
        * The `ActorSystem` just started and has not joined a cluster.
        * The `ActorSystem` left the cluster.
    * Available inside the `Akka.HealthCheck.Cluster` NuGet package.
5. `AkkaPersistenceLivenessProbeProvider` - Liveness probe for persistence. It probes the persistence storage every second to check that persistence is working properly.
    * Reports healthy when persistence successfully recover both journal and snapshot data from storage.
    * Reports unhealthy when:
        * Persistence just started and has not recovered.
        * Either journal or snapshot failed recovery inside the periodic check.
    * Available inside the `Akka.HealthCheck.Persistence` NuGet package.

## Configuring Using `Akka.Hosting`
[Back To Top](#akkahealthcheck)

To use the [`Akka.Hosting`](https://github.com/akkadotnet/Akka.Hosting/) extension method, you will need to install the `Akka.HealthCheck.Hosting` package.

```powershell
dotnet add package Akka.HealthCheck.Hosting
```

> **NOTE**
>
> Unlike the core library, `Akka.HealthCheck.Hosting` already includes `Akka.HealthCheck.Cluster` and `Akka.HealthCheck.Persistence` as its dependency; there is no need to install these packages to start using `Akka.HealthCheck` with clustering or persistence.

To add health check to your actor system, use the `.WithHealthCheck()` hosting extension method:

```csharp
using var host = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddAkka("test-system", (builder, provider) =>
        {
            // Add Akka.Cluster support
            builder.WithClustering();
            
            // Add persistence
            builder
                .WithInMemoryJournal()
                .WithInMemorySnapshotStore();

            // Add Akka.HealthCheck
            builder.WithHealthCheck(options =>
            {
                // Here we're adding all of the built-in providers
                options.AddProviders(HealthCheckType.All);
            });
        });
    })
    .Build();

await host.RunAsync();
```

`HealthCheckType` is a bit flag enum that consists of these choices:
```csharp
[Flags]
public enum HealthCheckType
{
    DefaultLiveness = 1,
    DefaultReadiness = 2,
    Default = DefaultLiveness | DefaultReadiness,
    ClusterLiveness = 4,
    ClusterReadiness = 8,
    Cluster = ClusterLiveness | ClusterReadiness,
    PersistenceLiveness = 16,
    Persistence = PersistenceLiveness,
    All = Default | Cluster | Persistence
}
```

## Configuring Using HOCON
[Back To Top](#akkahealthcheck)

> **NOTE**
>
> The cluster and persistence probe providers for `Akka.HealthCheck` are published in separate NuGet packages.
>
> - If you need to use the cluster liveness and readiness probe, you need to install the `Akka.HealthCheck.Cluster` NuGet package
> - If you need to use the persistence liveness probe, you need to install the `Akka.HealthCheck.Persistence` NuGet package

`Akka.HealthCheck` can be added manually through HOCON configuration:

```csharp
var hocon = @"
akka.healthcheck {
  log-config-on-start = on
  log-info = on
  liveness {
    providers {
      default = "Akka.HealthCheck.Liveness.DefaultLivenessProvider, Akka.HealthCheck"
      cluster = "Akka.HealthCheck.Cluster.ClusterLivenessProbeProvider, Akka.HealthCheck.Cluster"
    }
            
    transport = tcp
    tcp.port = 8080
  }
  
  readiness {
    providers {
      default = "Akka.HealthCheck.Readiness.DefaultReadinessProvider, Akka.HealthCheck"
      custom = "MyAssembly.CustomReadinessProvider, MyAssembly"
    }
    transport = file
    file.path = ""snapshot.txt""
  }    
}";
var config = ConfigurationFactory.ParseString(hocon);
var actorSystem = ActorSystem.Create("Probe", config);
var healthCheck = AkkaHealthCheck.For(actorSystem);
actorSystem.WhenTerminated.Wait();
```

The full reference HOCON configuration are:
```HOCON
############################################
# Akka.HealthCheck Reference Config File   #
############################################

akka.healthcheck{
  # Log the complete configuration at INFO level when the actor system is started.
  # This is useful when you are uncertain of what configuration is used.
  log-config-on-start = on
  
  # Log Liveness and Readiness probe event messages
  # Such as Liveness/Readiness subscriptions, and status request
  log-info = on

  liveness {
    # List of liveness probe providers. 
    # Custom end-user provider can be created by implementing the IProbeProvider interface.
    providers {
      # The default IProbeProvider implementation used for executing
      # liveness checks inside Akka.HealthCheck 
      default = "Akka.HealthCheck.Liveness.DefaultLivenessProvider, Akka.HealthCheck"
      
      # Clustering liveness check provider.
      # To use, install the Akka.HealthCheck.Cluster NuGet package and uncomment this line.
      #
      #cluster = "Akka.HealthCheck.Cluster.ClusterLivenessProbeProvider, Akka.HealthCheck.Cluster"
      
      # Persistence liveness check provider.
      # To use, install the Akka.HealthCheck.Persistence NuGet package and uncomment this line.
      #
      #persistence = "Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbeProvider, Akka.HealthCheck.Persistence"
    }

    # Defines the signaling mechanism used to communicate with K8s, AWS, Azure,
    # or whatever the hosting environment is for the Akka.NET application. The
    # accepted values are 'file', 'tcp', and 'custom'.
    #
    # In the event of a custom transport (which is the default), Akka.HealthCheck
    # won't try to automatically report any probe data to any medium. It's up
    # to the end-user to query that data directly from the AkkaHealthCheck
    # ActorSystem extension.
    transport = custom
   
    # If the `transport` used is `file`, this is where we specify the path of the file
    # that we will write status data to. It's strongly recommended that you use an 
    # absolute path for best results.
    file.path = "liveness.txt"
   
    # If the `transport` used is `tcp`, this is where we specify the port # of the inbound
    # socket that we're going open in order to accept external healthcheck connections.
    tcp.port = 11000
  }

   readiness{
      providers {
         # The default IProbeProvider implementation used for executing
         # readiness checks inside Akka.Healtcheck. Can be overridden by
         # end-users via a custom IProbeProvider implementation. 
         default = "Akka.HealthCheck.Readiness.DefaultReadinessProvider, Akka.HealthCheck"
         
         # Clustering readiness check provider.
         # To use, install the Akka.HealthCheck.Cluster NuGet package and uncomment this line.
         #
         #cluster = "Akka.HealthCheck.Cluster.ClusterReadinessProbeProvider, Akka.HealthCheck.Cluster"
      }
      
      # Defines the signaling mechanism used to communicate with K8s, AWS, Azure,
      # or whatever the hosting environment is for the Akka.NET application. The
      # accepted values are 'file', 'tcp', and 'custom'.
      #
      # In the event of a custom transport (which is the default), Akka.HealthCheck
      # won't try to automatically report any probe data to any medium. It's up
      # to the end-user to query that data directly from the AkkaHealthCheck
      # ActorSystem extension.
      transport = custom
      
      # If the `transport` used is `file`, this is where we specify the path of the file
      # that we will write status data to. It's strongly recommended that you use an 
      # absolute path for best results.
      file.path = "readiness.txt"
      
      # If the `transport` used is `tcp`, this is where we specify the port # of the inbound
      # socket that we're going open in order to accept external healthcheck connections.
      tcp.port = 11001
   }
}
```

# ASP.NET Integration
[Back To Top](#akkahealthcheck)

`Akka.HealthCheck` and `Akka.Hosting` can be directly integrated with `Microsoft.AspNetCore.Diagnostics.HealthChecks`, allowing users to access `Akka.HealthCheck` via HTTP REST API.

## Available Akka.NET Probes
[Back To Top](#akkahealthcheck)

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
[Back To Top](#akkahealthcheck)

To integrate `Akka.HealthCheck` with ASP.NET, you will need to install the `Akka.HealthCheck.Hosting.Web` package.

```powershell
dotnet add package Akka.HealthCheck.Hosting.Web
```

There are 3 steps that needs to be done to integrate `Akka.HealthCheck` with diagnostic health check

- Register health check probes to the health check middleware service.
- Add `Akka.HealthCheck` to the `ActorSystem`.
- Map the health check probe routes.

### Register `Akka.HealthCheck` Services With HealthCheck
[Back To Top](#akkahealthcheck)

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
[Back To Top](#akkahealthcheck)

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
[Back To Top](#akkahealthcheck)

The convenience `IEndpointRouteBuilder` extension method `MapAkkaHealthCheckRoutes` automatically scans for any registered probes inside the health check middleware and maps all the probes to a HTTP route. The HTTP route is the concatenation of the probe tags. By default:

- `AkkaReadinessProbe` is mapped to "/healthz/akka/ready/node"
- `AkkaLivenessProbe` is mapped to "/healthz/akka/live/node"
- `AkkaClusterReadinessProbe` is mapped to "/healthz/akka/ready/cluster"
- `AkkaClusterLivenessProbe` is mapped to "/healthz/akka/live/cluster"
- `AkkaPersistenceLivenessProbe` is mapped to "/healthz/akka/live/persistence"
- All liveness probes can be queried all at once at "/healthz/akka/live"
- All readiness probes can be queried all at once at "/healthz/akka/ready"
- All Akka.NET probes can be queried all at once at "/healthz/akka"

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
[Back To Top](#akkahealthcheck)

By default, the health check middleware outputs a simple string response of either "healthy" or "unhealthy" regardless of the number of probes being queried. To more verbose response can be gained by using `Helper.JsonResponseWriter` as the route endpoint response writer.

```csharp
app.MapAkkaHealthCheckRoutes(
    optionConfigure: (_, opt) =>
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
    "akka-live-node": {
      "status": "Healthy",
      "description": "Akka.NET node is alive",
      "data": {
        "message": "Live: 12/16/2022 9:54:28 PM +00:00"
      }
    },
    "akka-ready-node": {
      "status": "Healthy",
      "description": "Akka.NET node is ready",
      "data": {
        "message": "Live: 12/16/2022 9:54:28 PM +00:00"
      }
    },
    "akka-live-cluster": {
      "status": "Healthy",
      "description": "Akka.NET cluster is alive",
      "data": {
        "message": ""
      }
    },
    "akka-ready-cluster": {
      "status": "Healthy",
      "description": "Akka.NET cluster is ready",
      "data": {
        "message": ""
      }
    },
    "akka-live-persistence": {
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
[Back To Top](#akkahealthcheck)

To manually setup a custom `IProbeProvider`, check the [custom probe example project](https://github.com/petabridge/akkadotnet-healthcheck/tree/dev/src/Akka.HealthCheck.Hosting.Web.Custom.Example).

Documentation on how to set up ASP.NET Core health check can be read [here](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

# Building this solution
[Back To Top](#akkahealthcheck)

To run the build script associated with this solution, execute the following:

**Windows**
```
c:\> build.cmd all
```

**Linux / OS X**
```
c:\> build.sh all
```

If you need any information on the supported commands, please execute the `build.[cmd|sh] help` command.

This build script is powered by [FAKE](https://fake.build/); please see their API documentation should you need to make any changes to the [`build.fsx`](build.fsx) file.

## Conventions
[Back To Top](#akkahealthcheck)

The attached build script will automatically do the following based on the conventions of the project names added to this project:

* Any project name ending with `.Tests` will automatically be treated as a [XUnit2](https://xunit.github.io/) project and will be included during the test stages of this build script;
* Any project name ending with `.Tests` will automatically be treated as a [NBench](https://github.com/petabridge/NBench) project and will be included during the test stages of this build script; and
* Any project meeting neither of these conventions will be treated as a NuGet packaging target and its `.nupkg` file will automatically be placed in the `bin\nuget` folder upon running the `build.[cmd|sh] all` command.

## DocFx for Documentation
[Back To Top](#akkahealthcheck)

This solution also supports [DocFx](http://dotnet.github.io/docfx/) for generating both API documentation and articles to describe the behavior, output, and usages of your project. 

All of the relevant articles you wish to write should be added to the `/docs/articles/` folder and any API documentation you might need will also appear there.

All of the documentation will be statically generated and the output will be placed in the `/docs/_site/` folder. 

### Previewing Documentation
[Back To Top](#akkahealthcheck)

To preview the documentation for this project, execute the following command at the root of this folder:

```
C:\> serve-docs.cmd
```

This will use the built-in `docfx.console` binary that is installed as part of the NuGet restore process from executing any of the usual `build.cmd` or `build.sh` steps to preview the fully-rendered documentation. For best results, do this immediately after calling `build.cmd buildRelease`.

## Release Notes, Version Numbers, Etc
[Back To Top](#akkahealthcheck)

This project will automatically populate its release notes in all of its modules via the entries written inside [`RELEASE_NOTES.md`](RELEASE_NOTES.md) and will automatically update the versions of all assemblies and NuGet packages via the metadata included inside [`Directory.Build.props`](src/Directory.Build.props).

All new projects added into the solution will automatically picks up all the settings set inside `Directory.Build.props`.

## Code Signing via SignService
[Back To Top](#akkahealthcheck)

This project uses [SignService](https://github.com/onovotny/SignService) to code-sign NuGet packages prior to publication. The `build.cmd` and `build.sh` scripts will automatically download the `SignClient` needed to execute code signing locally on the build agent, but it's still your responsibility to set up the SignService server per the instructions at the linked repository.

Once you've gone through the ropes of setting up a code-signing server, you'll need to set a few configuration options in your project in order to use the `SignClient`:

* Add your Active Directory settings to [`appsettings.json`](appsettings.json) and
* Pass in your signature information to the `signingName`, `signingDescription`, and `signingUrl` values inside `build.fsx`.

Whenever you're ready to run code-signing on the NuGet packages published by `build.fsx`, execute the following command:

```
C:\> build.cmd nuget SignClientSecret={your secret} SignClientUser={your username}
```

This will invoke the `SignClient` and actually execute code signing against your `.nupkg` files prior to NuGet publication.

If one of these two values isn't provided, the code signing stage will skip itself and simply produce unsigned NuGet code packages.
