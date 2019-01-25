using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    /// <summary>
    /// Specs for the <see cref="DefaultLivenessProbe"/>
    /// </summary>
    public class DefaultLivenessProbeSpecs : LivenessProbeSpecBase
    {
        public DefaultLivenessProbeSpecs(ITestOutputHelper helper) : base(helper)
        {
        }

        protected override Props LivenessProbeProps => Props.Create(() => new DefaultLivenessProbe());
    }
}