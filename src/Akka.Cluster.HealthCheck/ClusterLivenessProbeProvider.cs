using System;
using System.Text;
using Akka.Actor;
using Akka.HealthCheck;

namespace Akka.Cluster.HealthCheck
{
    /// <summary>
    /// <see cref="IProbeProvider"/> liveness implementation intended for use with Akka.Cluster.
    /// </summary>
    public sealed class ClusterLivenessProbeProvider : ProbeProviderBase
    {
        public ClusterLivenessProbeProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => Props.Create(() => new ClusterLivenessProbe());
    }
}
