using Akka.Cluster.Hosting;
using Akka.Hosting;
using Akka.Remote.Hosting;
using Akka.Serialization;
using LogLevel = Akka.Event.LogLevel;

namespace Akka.HealthCheck.Hosting.Web.Example;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var webBuilder = WebApplication.CreateBuilder(args);

        webBuilder.Logging
            .AddConsole();
        
        webBuilder.Services
            // Register all of the health check service with IServiceCollection
            .WithAkkaHealthCheck(HealthCheckType.All)
            .AddHealthChecks();
        
        webBuilder.Services.AddAkka("actor-system", (builder, serviceProvider) =>
            {
                builder
                    .AddHocon("akka.cluster.min-nr-of-members = 1", HoconAddMode.Prepend)
                    .WithRemoting()
                    .WithClustering()
                    .AddStartup((system, _) =>
                    {
                        var cluster = Akka.Cluster.Cluster.Get(system);
                        cluster.Join(cluster.SelfAddress);
                    });
                
                // Automatically detects which health checks were registered inside the health check middleware and starts them
                builder.WithWebHealthCheck(serviceProvider);
            });

        var app = webBuilder.Build();

        // Automatically detects which health checks were registered inside the health check middleware and maps their routes
        app.MapAkkaHealthCheckRoutes(
            prependPath:"/health",
            optionConfigure: (tags, opt) =>
            {
                // Use a custom response writer to output a json of all reported statuses
                opt.ResponseWriter = Helper.JsonResponseWriter;
            });

        await app.RunAsync();
    }
}