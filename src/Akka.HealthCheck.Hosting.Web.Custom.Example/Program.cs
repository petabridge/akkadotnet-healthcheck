using Akka.HealthCheck.Hosting;
using Akka.HealthCheck.Hosting.Web.Custom.Example;
using Akka.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services
    .AddHealthChecks()
    .AddCheck<CustomHealthCheckReadinessProbe>("akka-custom-readiness", HealthStatus.Unhealthy, new [] { "akka", "ready", "custom" });

webBuilder.Services
    .AddAkka("actor-system", (builder, serviceProvider) =>
    {
        builder
            .WithHealthCheck(opt =>
            {
                opt.AddReadinessProvider<CustomReadinessProbeProvider>("custom");
            });
    });

var app = webBuilder.Build();

app.MapHealthChecks("/healthz/akka/ready/custom", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.IsSupersetOf(new [] { "akka", "ready", "custom" })
});

await app.RunAsync();
