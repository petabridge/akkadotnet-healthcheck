using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class AkkaHealthCheckSpecs 
    {
        [Fact(DisplayName = "Should load default AkkaHealthCheck plugin and settings")]
        public async Task Should_load_default_AkkaHealthCheck()
        {
            using (var system = ActorSystem.Create("foo"))
            {
                var healthCheck = AkkaHealthCheck.For(system);
                healthCheck.Settings.Misconfigured.Should().BeFalse();
                var status = await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance, TimeSpan.FromSeconds(1));
                status.IsLive.Should().BeTrue();
            }
        }
    }
}
