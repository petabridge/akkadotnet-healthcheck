// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.HealthCheck.Hosting.Example.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services
            .AddLogging()
            .AddHostedService<ILivenessProbe>(_ => 
                new SocketHealthHostedService("liveness", 15000))
            .AddHostedService<IReadinessProbe>(_ => 
                new SocketHealthHostedService("readiness", 15001));
    })
    .Build();

await host.RunAsync();