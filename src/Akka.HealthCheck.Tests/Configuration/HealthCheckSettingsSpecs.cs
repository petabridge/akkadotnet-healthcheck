// -----------------------------------------------------------------------
// <copyright file="HealthCheckSettingsSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Tests.Configuration
{
    public class HealthCheckSettingsSpecs : TestKit.Xunit.TestKit
    {
        [Fact(DisplayName = "Should be able to load default Akka.HealthCheck HOCON")]
        public void Should_load_default_HealthCheck_HOCON()
        {
            HealthCheckSettings.DefaultConfig().Should().NotBe(Config.Empty);
        }

        [Fact(DisplayName = "Should be able to load default Akka.HealthCheck settings")]
        public void Should_load_default_HealthCheck_Settings()
        {
            var settings = new HealthCheckSettings(HealthCheckSettings.DefaultConfig().GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(DefaultLivenessProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(DefaultReadinessProvider));
        }

        [Fact(DisplayName = "HealthCheckSettings.Misconfigured should be true when Liveness provider is invalid")]
        public void Should_signal_misconfiguration_when_Liveness_provider_is_invalid()
        {
            var hocon = ConfigurationFactory.ParseString(@"
                akka.healthcheck.liveness.provider = ""Akka.Fake.FakeProvider, Akka.Fake""
            ");

            var settings = new HealthCheckSettings(hocon.WithFallback(HealthCheckSettings.DefaultConfig())
                .GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeTrue();
            settings.LivenessProbeProvider.Should().Be(typeof(DefaultLivenessProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(DefaultReadinessProvider));
        }

        [Fact(DisplayName = "HealthCheckSettings.Misconfigured should be true when Readiness provider is invalid")]
        public void Should_signal_misconfiguration_when_Readiness_provider_is_invalid()
        {
            var hocon = ConfigurationFactory.ParseString(@"
                akka.healthcheck.readiness.provider = ""Akka.Fake.FakeProvider, Akka.Fake""
            ");

            var settings = new HealthCheckSettings(hocon.WithFallback(HealthCheckSettings.DefaultConfig())
                .GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeTrue();
            settings.LivenessProbeProvider.Should().Be(typeof(DefaultLivenessProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(DefaultReadinessProvider));
        }
    }
}