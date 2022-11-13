using Akka.Cluster;
using Akka.Cluster.Hosting;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .WithAkkaHealthCheck()
    .WithAkkaClusterHealthCheck()
    .WithAkkaPersistenceHealthCheck()
    .AddAkka("actor-system", (configurationBuilder, serviceProvider) =>
    {
        configurationBuilder
            .AddHocon("akka.cluster.min-nr-of-members = 1", HoconAddMode.Prepend)
            .WithClustering()
            .WithHealthCheck()
            .AddStartup((system, registry) =>
            {
                var cluster = Cluster.Get(system);
                cluster.Join(cluster.SelfAddress);
            });
    });

var app = builder.Build();

// Create our health endpoint(s) - look here for documentation: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0
app.MapHealthChecks("/healthz/live/akka", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new [] {"akka", "liveness"})
});

app.MapHealthChecks("/healthz/live/akka/node", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new [] {"akka", "node", "liveness"})
});

app.MapHealthChecks("/healthz/live/akka/persistence", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new [] {"akka", "persistence", "liveness"})
});

app.MapHealthChecks("/healthz/live/akka/cluster", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new [] {"akka", "cluster", "liveness"})
});

app.MapHealthChecks("/healthz/ready/akka", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new []{"akka", "readiness"})
});

app.MapHealthChecks("/healthz/ready/akka/node", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new []{"akka", "node", "readiness"})
});

app.MapHealthChecks("/healthz/ready/akka/cluster", new HealthCheckOptions
{
    // Only include HealthChecks with tag 'readiness' to this endpoint, customize this for your needs...
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new []{"akka", "cluster", "readiness"})
});

app.Run();
