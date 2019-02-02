using Akka.Actor;

namespace Akka.HealthCheck.Readiness
{
    /// <summary>
    /// Used when <see cref="Akka.HealthCheck.Configuration.HealthCheckSettings.Misconfigured"/> is true.
    /// </summary>
    public sealed class MisconfiguredReadinessProvider : ProbeProviderBase
    {
        public MisconfiguredReadinessProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => DefaultReadinessProbe.MisconfiguredProbeProbs();
    }
}