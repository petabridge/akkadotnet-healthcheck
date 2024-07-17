// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeSubscriptionTest.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Util;
using Akka.Util.Internal;
using FluentAssertions;
using System;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeSubscriptionTest : Akka.TestKit.Xunit2.TestKit
    {

        public AkkaPersistenceLivenessProbeSubscriptionTest(ITestOutputHelper helper)
            : base(TestConfig.GetValidConfigurationString(ThreadLocalRandom.Current.Next()), output: helper)
        {

        }
    

        [Fact(DisplayName = "AkkaPersistenceLivenessProbe should correctly handle subscription requests")]
        public void AkkaPersistenceLivenessProbe_Should_Handle_Subscriptions_In_Any_State()
        {
            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(true, 250.Milliseconds(), 3.Seconds(), 0)));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            var firstResult = ExpectMsg<LivenessStatus>();
            firstResult.Status.Should().Be(AkkaHealthStatus.Degraded, "Initial status should be degraded, not unhealthy");
            firstResult.IsLive.Should().BeTrue("Degraded should still report as live");
            AwaitAssert(() => ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue(),TimeSpan.FromSeconds(10));

            var probe = CreateTestProbe();
            ProbActor.Tell(new SubscribeToLiveness(probe));
            probe.ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
            
        }


    }

}
