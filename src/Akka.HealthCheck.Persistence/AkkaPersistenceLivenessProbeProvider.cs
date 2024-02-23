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
        private readonly TimeSpan _timeout;
        
        public AkkaPersistenceLivenessProbeProvider(ActorSystem system) : base(system)
        {
            var config = system.Settings.Config.GetConfig("akka.healthcheck.liveness.persistence");
            _interval = config.GetTimeSpan("probe-interval", TimeSpan.FromSeconds(10));
            _timeout = config.GetTimeSpan("timeout", TimeSpan.FromSeconds(3));
        }

        public override Props ProbeProps => 
            AkkaPersistenceLivenessProbe.PersistentHealthCheckProps(Settings.LogInfoEvents, _interval, _timeout);
    }
}