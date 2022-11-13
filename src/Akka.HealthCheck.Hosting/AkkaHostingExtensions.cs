// -----------------------------------------------------------------------
// <copyright file="AkkaHostingExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.HealthCheck.Hosting.Services;
using Akka.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting
{
    public static class AkkaHostingExtensions
    {
        public static IServiceCollection WithAkkaHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaLivenessService>("akka-liveness", HealthStatus.Unhealthy, new[] { "akka", "node", "liveness" })
                .AddCheck<AkkaReadinessService>("akka-readiness", HealthStatus.Unhealthy, new[] { "akka", "node", "readiness" });

            return services;
        }
        
        public static IServiceCollection WithAkkaPersistenceHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaPersistenceLivenessService>("akka-persistence-liveness", HealthStatus.Unhealthy, new[] { "akka", "persistence", "liveness" });

            return services;
        }
        
        public static IServiceCollection WithAkkaClusterHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaClusterLivenessService>("akka-cluster-liveness", HealthStatus.Unhealthy, new[] { "akka", "cluster", "liveness" })
                .AddCheck<AkkaClusterReadinessService>("akka-cluster-readiness", HealthStatus.Unhealthy, new[] { "akka", "cluster", "readiness" });

            return services;
        }
        
        public static AkkaConfigurationBuilder WithHealthCheck(
            this AkkaConfigurationBuilder builder,
            Action<AkkaHealthCheckConfig>? configure = null)
        {
            var configuration = new AkkaHealthCheckConfig();
            configure?.Invoke(configuration);

            var config = configuration.ToConfig();
            if (config is { })
            {
                builder.AddHocon(config, HoconAddMode.Prepend);
            }
            
            builder.AddStartup((system, registry) =>
            {
                AkkaHealthCheck.For(system);
            });
            
            return builder;
        }
    }
}