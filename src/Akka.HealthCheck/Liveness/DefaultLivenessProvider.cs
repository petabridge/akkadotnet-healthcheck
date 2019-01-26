using Akka.Actor;
using Akka.HealthCheck;

namespace Akka.HealthCheck.Liveness
{
    /// <inheritdoc />
    /// <summary>
    /// The default <see cref="T:Akka.HealthCheck.IProbeProvider" /> implementation for liveness checks.
    /// </summary>
    public sealed class DefaultLivenessProvider : ProbeProviderBase
    {
        public DefaultLivenessProvider(ActorSystem system) : base(system)
        {
           
        }

        public override Props ProbeProps => Props.Create(() => new DefaultLivenessProbe());
    }
}