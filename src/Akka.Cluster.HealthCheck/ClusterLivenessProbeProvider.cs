// -----------------------------------------------------------------------
// <copyright file="ClusterLivenessProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.HealthCheck;

namespace Akka.Cluster.HealthCheck
{
    /// <summary>
    ///     <see cref="IProbeProvider" /> liveness implementation intended for use with Akka.Cluster.
    /// </summary>
    public sealed class ClusterLivenessProbeProvider : ProbeProviderBase
    {
        public ClusterLivenessProbeProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => Props.Create(() => new ClusterLivenessProbe());
    }
}