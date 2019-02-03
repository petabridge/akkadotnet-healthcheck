﻿// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheckSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Tests
{
    public class AkkaHealthCheckSpecs
    {
        private class CustomProbe : ReceiveActor
        {
            private readonly LivenessStatus _livenessStatus;
            private readonly ReadinessStatus _readinessStatus;

            public CustomProbe() : this(new LivenessStatus(true), new ReadinessStatus(true))
            {
            }

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

        [Fact(DisplayName = "Should load AkkaHealthCheck plugin and settings with MISCONFIGURED custom providers")]
        public async Task Should_load_custom_AkkaHealthCheck()
        {
            Config config = @"
                akka.healthcheck.liveness.provider = ""Akka.HealthCheck.Tests.AkkaHealthCheckSpecs+CustomHealthCheckProvider+FakeName, Akka.HealthCheck.Tests""
                akka.healthcheck.readiness.provider = ""Akka.HealthCheck.Tests.AkkaHealthCheckSpecs+CustomHealthCheckProvider+FakeName, Akka.HealthCheck.Tests""
            ";

            using (var system = ActorSystem.Create("foo", config))
            {
                var healthCheck = AkkaHealthCheck.For(system);

                // should be misconfigured
                healthCheck.Settings.Misconfigured.Should().BeTrue();

                // check that the custom plugins were NOT loaded
                healthCheck.Settings.LivenessProbeProvider.Should().Be(typeof(DefaultLivenessProvider));
                healthCheck.Settings.ReadinessProbeProvider.Should().Be(typeof(DefaultReadinessProvider));

                // when misconfigured, probes should report that we are neither live nor ready
                var livenessStatus =
                    await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance,
                        TimeSpan.FromSeconds(1));
                livenessStatus.IsLive.Should().BeFalse();

                var readinessStatus =
                    await healthCheck.ReadinessProbe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance,
                        TimeSpan.FromSeconds(1));
                readinessStatus.IsReady.Should().BeFalse();
            }
        }

        [Fact(DisplayName = "Should load default AkkaHealthCheck plugin and settings")]
        public async Task Should_load_default_AkkaHealthCheck()
        {
            using (var system = ActorSystem.Create("foo"))
            {
                var healthCheck = AkkaHealthCheck.For(system);
                healthCheck.Settings.Misconfigured.Should().BeFalse();
                var livenessStatus =
                    await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance,
                        TimeSpan.FromSeconds(1));
                livenessStatus.IsLive.Should().BeTrue();
                var readinessStatus =
                    await healthCheck.ReadinessProbe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance,
                        TimeSpan.FromSeconds(1));
                readinessStatus.IsReady.Should().BeTrue();
            }
        }

        [Fact(DisplayName = "Should load AkkaHealthCheck plugin and settings with custom providers")]
        public async Task Should_load_misconfigured_AkkaHealthCheck()
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

                var livenessStatus =
                    await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance,
                        TimeSpan.FromSeconds(1));
                livenessStatus.IsLive.Should().BeTrue();
                var readinessStatus =
                    await healthCheck.ReadinessProbe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance,
                        TimeSpan.FromSeconds(1));
                readinessStatus.IsReady.Should().BeTrue();
            }
        }
    }
}