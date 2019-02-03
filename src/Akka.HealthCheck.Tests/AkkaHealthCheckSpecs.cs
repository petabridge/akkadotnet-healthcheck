using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class AkkaHealthCheckSpecs 
    {
        private class CustomProbe : ReceiveActor
        {
            private readonly LivenessStatus _livenessStatus;
            private readonly ReadinessStatus _readinessStatus;

            public CustomProbe() : this(new LivenessStatus(true), new ReadinessStatus(true)) { }

            public CustomProbe(LivenessStatus livenessStatus, ReadinessStatus readinessStatus)
            {
                _livenessStatus = livenessStatus;
                _readinessStatus = readinessStatus;

                Receive<GetCurrentLiveness>(_ => Sender.Tell(_livenessStatus));
                Receive<GetCurrentReadiness>(_ => Sender.Tell(_readinessStatus));
            }
        }

        private class CustomHealthCheckProvider : ProbeProviderBase
        {
            public CustomHealthCheckProvider(ActorSystem system) : base(system)
            {
            }

            public override Props ProbeProps => Props.Create(() => new CustomProbe());
        }

        [Fact(DisplayName = "Should load default AkkaHealthCheck plugin and settings")]
        public async Task Should_load_default_AkkaHealthCheck()
        {
            using (var system = ActorSystem.Create("foo"))
            {
                var healthCheck = AkkaHealthCheck.For(system);
                healthCheck.Settings.Misconfigured.Should().BeFalse();
                var livenessStatus = await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance, TimeSpan.FromSeconds(1));
                livenessStatus.IsLive.Should().BeTrue();
                var readinessStatus = await healthCheck.ReadinessProbe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance, TimeSpan.FromSeconds(1));
                readinessStatus.IsReady.Should().BeTrue();
            }
        }

        [Fact(DisplayName = "Should load AkkaHealthCheck plugin and settings with custom providers")]
        public async Task Should_load_custom_AkkaHealthCheck()
        {
            Config config = @"
                akka.healthcheck.liveness.provider = ""Akka.HealthCheck.Tests.AkkaHealthCheckSpecs+CustomHealthCheckProvider, Akka.HealthCheck.Tests""
                akka.healthcheck.readiness.provider = ""Akka.HealthCheck.Tests.AkkaHealthCheckSpecs+CustomHealthCheckProvider, Akka.HealthCheck.Tests""
            ";

             using (var system = ActorSystem.Create("foo", config))
            {
                var healthCheck = AkkaHealthCheck.For(system);
                healthCheck.Settings.Misconfigured.Should().BeFalse();

                // check that the custom plugins were loaded
                healthCheck.Settings.LivenessProbeProvider.Should().Be(typeof(CustomHealthCheckProvider));
                healthCheck.Settings.ReadinessProbeProvider.Should().Be(typeof(CustomHealthCheckProvider));

                var livenessStatus = await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance, TimeSpan.FromSeconds(1));
                livenessStatus.IsLive.Should().BeTrue();
                var readinessStatus = await healthCheck.ReadinessProbe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance, TimeSpan.FromSeconds(1));
                readinessStatus.IsReady.Should().BeTrue();
            }
        }
    }
}
