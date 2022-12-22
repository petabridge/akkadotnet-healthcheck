// -----------------------------------------------------------------------
// <copyright file="AkkaHostingExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Hosting;

namespace Akka.HealthCheck.Hosting
{
    public static class AkkaHostingExtensions
    {
        public static AkkaConfigurationBuilder WithHealthCheck(
            this AkkaConfigurationBuilder builder,
            Action<AkkaHealthCheckOptions>? configure = null)
        {
            var configuration = new AkkaHealthCheckOptions();
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