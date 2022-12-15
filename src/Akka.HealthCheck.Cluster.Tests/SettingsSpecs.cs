// -----------------------------------------------------------------------
// <copyright file="SettingsSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using Xunit;
using FluentAssertions;

namespace Akka.HealthCheck.Cluster.Tests
{
    public class SettingsSpecs
    {
        [Fact(DisplayName = "HealthCheck should load settings properly")]
        public void HealthCheck_Should_load_settings_test()
        {
            var config = ((Config) @"
akka.healthcheck{
    liveness {
        providers {
            default = ""Akka.HealthCheck.Cluster.ClusterLivenessProbeProvider, Akka.HealthCheck.Cluster""
        }
    }
    readiness {
        providers {
            default = ""Akka.HealthCheck.Cluster.ClusterReadinessProbeProvider, Akka.HealthCheck.Cluster""
        }
    }
}").WithFallback(HealthCheckSettings.DefaultConfig());
            var settings = new HealthCheckSettings(config.GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(ClusterLivenessProbeProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(ClusterReadinessProbeProvider));
        }
        
        [Fact(DisplayName = "HealthCheck should load settings properly (compat)")]
        public void HealthCheck_Should_load_settings_test_compat()
        {
            var config = ((Config) @"
akka.healthcheck{
    liveness {
        provider = ""Akka.HealthCheck.Cluster.ClusterLivenessProbeProvider, Akka.HealthCheck.Cluster""
    }
    readiness {
        provider = ""Akka.HealthCheck.Cluster.ClusterReadinessProbeProvider, Akka.HealthCheck.Cluster""
    }
}").WithFallback(HealthCheckSettings.DefaultConfig());
            var settings = new HealthCheckSettings(config.GetConfig("akka.healthcheck"));
            settings.Misconfigured.Should().BeFalse();
            settings.LivenessProbeProvider.Should().Be(typeof(ClusterLivenessProbeProvider));
            settings.ReadinessProbeProvider.Should().Be(typeof(ClusterReadinessProbeProvider));
        }
    }
}