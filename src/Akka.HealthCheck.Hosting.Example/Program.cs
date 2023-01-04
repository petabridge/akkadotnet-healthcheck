// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Cluster.Hosting;
using Akka.HealthCheck.Hosting;
using Akka.Hosting;
using Akka.Persistence.Hosting;
using Microsoft.Extensions.Hosting;

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
                options.Liveness.Transport = HealthCheckTransport.Tcp;
                options.Liveness.TcpPort = 15000;
                options.Readiness.Transport = HealthCheckTransport.Tcp;
                options.Readiness.TcpPort = 15001;
            });
        });
    })
    .Build();

await host.RunAsync();