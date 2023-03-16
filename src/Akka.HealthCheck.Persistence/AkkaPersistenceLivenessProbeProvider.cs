// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Persistence
{
    /// <summary>
    ///     <see cref="IProbeProvider" /> liveness implementation intended for use with Akka.Persistence.
    /// </summary>
    public sealed class AkkaPersistenceLivenessProbeProvider : ProbeProviderBase
    {
        public AkkaPersistenceLivenessProbeProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => AkkaPersistenceLivenessProbe.PersistentHealthCheckProps(Settings.LogInfoEvents);
    }
}