#### 1.0.0-beta1 January 5 2023 ####
This release is a beta release of the new `Akka.Hosting` API and the ASP.NET integration API. We would love to hear your input on these new APIs.

* [Bumped Akka version to 1.4.47](https://github.com/akkadotnet/akka.net/releases/tag/1.4.47)
* [Add multi probe provider support](https://github.com/petabridge/akkadotnet-healthcheck/pull/151)
* [Add `Akka.Hosting` support and ASP.NET IHealthCheck integration](https://github.com/petabridge/akkadotnet-healthcheck/pull/148)
* [Expanded persistence health check reporting](https://github.com/petabridge/akkadotnet-healthcheck/pull/154)
* [Bump Akka.Hosting from 0.5.1 to 1.0.0](https://github.com/petabridge/akkadotnet-healthcheck/pull/163)
* [Improve ASP.NET health check route configuration callback](https://github.com/petabridge/akkadotnet-healthcheck/pull/165)
* [Fix probe status reporting to account for all provider statuses](https://github.com/petabridge/akkadotnet-healthcheck/pull/171)
* [Add documentation](https://github.com/petabridge/akkadotnet-healthcheck/pull/173)

**Notable Changes From Previous Versions**

> **NOTE**
> 
> All these information can be read in the documentation [here](https://github.com/petabridge/akkadotnet-healthcheck/blob/dev/README.md)

**1. Improved Persistence Status Report**

Persistence health check now returns a `PersistenceLivenessStatus` with a more comprehensive status report that also includes whether snapshots and journal events were successfully persisted, and any exceptions thrown during the probe execution.

**2. Multi-provider Support**

Both liveness and readiness endpoint can now host multiple health check probe providers. Note that both liveness and readiness endpoint will return unhealthy if any of these hosted probes reported that they are unhealthy.

The HOCON configuration for `Akka.HealthCheck` has been changed to accomodate this. Instead of settings a single provider, you can now pass in multiple providers at once:

```hocon
akka.healthcheck {
  liveness {
    providers {
      default = "Akka.HealthCheck.Liveness.DefaultLivenessProvider, Akka.HealthCheck"
      cluster = "Akka.HealthCheck.Cluster.ClusterLivenessProbeProvider, Akka.HealthCheck.Cluster"
    }
  }
  readiness {
    providers {
      default = "Akka.HealthCheck.Readiness.DefaultReadinessProvider, Akka.HealthCheck"
      custom = "MyAssembly.CustomReadinessProvider, MyAssembly"
    }
  }
```

**3. `Akka.Hosting` integration**

To configure multi providers via `Akka.Hosting`, you can install the new `Akka.HealthCheck.Hosting` NuGet package and use the convenience method `AddProviders()` and provide the combination of providers you would like to run like so:

```csharp
// Add Akka.HealthCheck
builder.WithHealthCheck(options =>
{
    // Here we're adding all of the built-in providers
    options.AddProviders(HealthCheckType.All);
});
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

Depending on your code style, You can also use the more verbose methods to add providers:
```csharp
// Add Akka.HealthCheck
builder.WithHealthCheck(options =>
{
    // Here we're adding all of the built-in providers one provider at a time
    options
        .ClearAllProviders()
        .AddDefaultReadinessProvider()
        .AddClusterReadinessProvider()
        .AddDefaultLivenessProvider()
        .AddClusterLivenessProvider()
        .AddPersistenceLivenessProvider();
});
```

Custom `IProbeProvider` can be added using these methods:
```csharp
// Add Akka.HealthCheck
builder.WithHealthCheck(options =>
{
    // Adding custom user IProbeProvider providers
    options
        .AddReadinessProvider<MyReadinessProvider>("custom-readiness")
        .AddLivenessProvider<MyLivenessProvider>("custom-liveness");
});
```

**4. ASP.NET `IHealthCheck` Integration**

`Akka.HealthCheck` can be integrated directly by installing the `Akka.HealthCheck.Hosting.Web` NuGet package. You can read the documentation [here](https://github.com/petabridge/akkadotnet-healthcheck/blob/dev/README.md#aspnet-integration)

#### 0.3.4 December 22 2022 ####

This release is a patch release for a bug in the persistence liveness probe.
* [Patch persistence id bug inside the persistence liveness probe](https://github.com/petabridge/akkadotnet-healthcheck/pull/154)

#### 0.3.3 November 2 2022 ####
* [Bumped Akka version to 1.4.45](https://github.com/akkadotnet/akka.net/releases/tag/1.4.45)
* [Enabled dual platform targeting, binaries are now targeting netstandard2.0 and net60 platforms](https://github.com/petabridge/akkadotnet-healthcheck/pull/140)

#### 0.3.2 June 24 2021 ####
* [Switch `Akka.HealthCheck.Transports.Sockets.SocketStatusTransport` network protocol from IPV6 to IPV4](https://github.com/petabridge/akkadotnet-healthcheck/pull/95)
* [Bumped Akka version to 1.4.21](https://github.com/akkadotnet/akka.net/releases/tag/1.4.21)

#### 0.3.1 March 25 2021 ####
**Bumped Akka version**
Bumped Akka version to 1.4.18