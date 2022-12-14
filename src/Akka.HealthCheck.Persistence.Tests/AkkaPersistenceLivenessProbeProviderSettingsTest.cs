// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeProviderSettingsTest.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Akka.Actor;
using Akka.Configuration;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeProviderSettingsTest : TestKit.Xunit.TestKit
    {

        public AkkaPersistenceLivenessProbeProviderSettingsTest(ITestOutputHelper helper)
            : base(HoconString, output: helper)
        {
        }
        public static string HoconString = @"
                    akka.healthcheck{
                        liveness {
                            providers {
                                default = ""Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbeProvider, Akka.HealthCheck.Persistence""
                            }
                        }
                    }";


        [Fact(DisplayName = " ActorSystem should correcly load AkkaPersistenceLivenessProbeProvider from HOCON configuration")]
        public void AkkaPersistenceLivenessProbeProviderSettingsTest_Should_Load()
        {
            var healthCheck = AkkaHealthCheck.For(Sys);
            healthCheck.Settings.Misconfigured.Should().BeFalse();
            healthCheck.Settings.LivenessProbeProvider.Should().Be(typeof(AkkaPersistenceLivenessProbeProvider));
        }
    }
}