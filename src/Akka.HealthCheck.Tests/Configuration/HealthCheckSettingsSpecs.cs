// -----------------------------------------------------------------------
// <copyright file="HealthCheckSettingsSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Files;
using Akka.HealthCheck.Transports.Sockets;
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
            settings.LivenessTransport.Should().Be(ProbeTransport.Custom);
            settings.ReadinessTransport.Should().Be(ProbeTransport.Custom);
            settings.LivenessTransportSettings.Should().BeOfType<CustomTransportSettings>();
            settings.ReadinessTransportSettings.Should().BeOfType<CustomTransportSettings>();
            settings.LogConfigOnStart.Should().BeTrue();
        }

        [Fact(DisplayName = "Should be able load non-default log options")]
        public void Should_load_non_default_Log_Settings()
        {
            var hocon = ConfigurationFactory.ParseString(@"
                akka.healthcheck.log-config-on-start = false
            ");

            var settings = new HealthCheckSettings(hocon.WithFallback(HealthCheckSettings.DefaultConfig())
                .GetConfig("akka.healthcheck"));
            settings.LogConfigOnStart.Should().BeFalse();
            //Will add option for normall log's in this same test later on
        }

        [Fact(DisplayName = "HealthCheckSettings should load non-default transport values")]
        public void Should_load_non_default_Transport_values()
        {
            var hocon = ConfigurationFactory.ParseString(@"
                akka.healthcheck.readiness.transport = file
                akka.healthcheck.liveness.transport = tcp
            ");

            var settings = new HealthCheckSettings(hocon.WithFallback(HealthCheckSettings.DefaultConfig())
                .GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(DefaultLivenessProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(DefaultReadinessProvider));
            settings.LivenessTransport.Should().Be(ProbeTransport.TcpSocket);
            settings.ReadinessTransport.Should().Be(ProbeTransport.File);
            settings.LivenessTransportSettings.Should().BeOfType<SocketTransportSettings>();
            settings.LivenessTransportSettings.As<SocketTransportSettings>().Port.Should().Be(11000);
            settings.ReadinessTransportSettings.Should().BeOfType<FileTransportSettings>();
            settings.ReadinessTransportSettings.As<FileTransportSettings>().FilePath.Should().Be("readiness.txt");
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