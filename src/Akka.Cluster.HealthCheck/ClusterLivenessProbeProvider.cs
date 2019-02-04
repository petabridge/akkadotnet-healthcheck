using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.HealthCheck;

namespace Akka.Cluster.HealthCheck
{
    /// <summary>
    /// <see cref="IProbeProvider"/> implementation intended for use with Akka.Cluster.
    /// </summary>
    public sealed class ClusterLivenessProbeProvider : ProbeProviderBase
    {
        public ClusterLivenessProbeProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps { get; }
    }


}
