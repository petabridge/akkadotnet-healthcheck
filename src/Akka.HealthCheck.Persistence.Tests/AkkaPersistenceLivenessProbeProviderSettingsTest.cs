// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeProviderSettingsTest.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeProviderSettingsTest
    {
        [Fact(DisplayName = " ActorSystem should correcly load AkkaPersistenceLivenessProbeProvider from HOCON configuration")]
        public void AkkaPersistenceLivenessProbeProviderSettingsTest_Should_Load()
        {
            var config = ConfigurationFactory.ParseString(@"
                    akka.healthcheck{
                        liveness {
                            providers {
                                default = ""Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbeProvider, Akka.HealthCheck.Persistence""
                            }
                        }
                    }").WithFallback(HealthCheckSettings.DefaultConfig());

            var settings = new HealthCheckSettings(config.GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(AkkaPersistenceLivenessProbeProvider));
        }
        
        [Fact(DisplayName = " ActorSystem should correcly load AkkaPersistenceLivenessProbeProvider from HOCON configuration (compat)")]
        public void AkkaPersistenceLivenessProbeProviderSettingsTest_Should_Load_compat()
        {
            var config = ConfigurationFactory.ParseString(@"
                    akka.healthcheck{
                        liveness {
                            provider = ""Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbeProvider, Akka.HealthCheck.Persistence""
                        }
                    }").WithFallback(HealthCheckSettings.DefaultConfig());

            var settings = new HealthCheckSettings(config.GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(AkkaPersistenceLivenessProbeProvider));
        }
    }
}