using Akka.Cluster.Hosting;
using Akka.HealthCheck.Cluster;
using Akka.HealthCheck.IHealthCheckExample;
using Akka.HealthCheck.IHealthCheckExample.HealthChecks;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Persistence;
using Akka.HealthCheck.Readiness;
using Akka.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAkka("actor-system", (configurationBuilder, serviceProvider) =>
{
    configurationBuilder
    .WithClustering()
    .WithActors((actorSystem, registry) =>
    {
        // We need to start the Probe actors which we need in our HealthChecks
        actorSystem.CreateAndRegisterHealthProbe<DefaultLivenessProbe>(registry, new DefaultLivenessProvider(actorSystem));
        actorSystem.CreateAndRegisterHealthProbe<ClusterLivenessProbe>(registry, new ClusterLivenessProbeProvider(actorSystem));
        actorSystem.CreateAndRegisterHealthProbe<AkkaPersistenceLivenessProbe>(registry, new AkkaPersistenceLivenessProbeProvider(actorSystem));
        actorSystem.CreateAndRegisterHealthProbe<DefaultReadinessProbe>(registry, new DefaultReadinessProvider(actorSystem));
        actorSystem.CreateAndRegisterHealthProbe<ClusterReadinessProbe>(registry, new ClusterReadinessProbeProvider(actorSystem));
    });
});

// Add health checks - look here for documentation: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0
builder.Services.AddHealthChecks()
    .AddCheck<AkkaLivenessActorHealthCheck<ClusterLivenessProbe>>("akka-liveness-cluster", HealthStatus.Unhealthy, new[] { "liveness" })
    .AddCheck<AkkaLivenessActorHealthCheck<DefaultLivenessProbe>>("akka-liveness-default", HealthStatus.Unhealthy, new[] { "liveness" })
    .AddCheck<AkkaLivenessActorHealthCheck<AkkaPersistenceLivenessProbe>>("akka-liveness-persistance", HealthStatus.Unhealthy, new[] { "liveness" })
    .AddCheck<AkkaReadinessActorHealthCheck<ClusterReadinessProbe>>("akka-readiness-cluster", HealthStatus.Unhealthy, new[] { "readiness" })
    .AddCheck<AkkaReadinessActorHealthCheck<DefaultReadinessProbe>>("akka-readiness-default", HealthStatus.Unhealthy, new[] { "readiness" });

var app = builder.Build();

// Create our health endpoint(s) - look here for documentation: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    // Only include HealthChecks with tag 'liveness' to this endpoint, customize this for your needs...
    Predicate = healthCheck => healthCheck.Tags.Contains("liveness")
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    // Only include HealthChecks with tag 'readiness' to this endpoint, customize this for your needs...
    Predicate = healthCheck => healthCheck.Tags.Contains("readiness")
});

app.Run();
