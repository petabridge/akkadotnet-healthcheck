// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;

namespace Akka.HealthCheck.Persistence
{
    /// <summary>
    ///     <see cref="IProbeProvider" /> liveness implementation intended for use with Akka.Persistence.
    /// </summary>
    public sealed class AkkaPersistenceLivenessProbeProvider : ProbeProviderBase
    {
        private readonly TimeSpan _interval;
        
        public AkkaPersistenceLivenessProbeProvider(ActorSystem system) : base(system)
        {
            _interval = system.Settings.Config.GetTimeSpan(
                path: "akka.healthcheck.liveness.persistence.probe-interval",
                @default: TimeSpan.FromSeconds(10));
        }

        public override Props ProbeProps => 
            AkkaPersistenceLivenessProbe.PersistentHealthCheckProps(Settings.LogInfoEvents, _interval);
    }
}