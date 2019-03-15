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
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeSubscriptionTest : TestKit.Xunit.TestKit
    {

        public AkkaPersistenceLivenessProbeSubscriptionTest(ITestOutputHelper helper)
            : base(TestConfig.GetValidConfigurationString(ThreadLocalRandom.Current.Next()), output: helper)
        {

        }


        [Fact(DisplayName = "AkkaPersistenceLivenessProbe should correctly handle subscription requests")]
        public void AkkaPersistenceLivenessProbe_Should_Handle_Subscriptions_In_Any_State()
        {
            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();
            AwaitAssert(() => ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue(),TimeSpan.FromSeconds(10));

            var probe = CreateTestProbe();
            ProbActor.Tell(new SubscribeToLiveness(probe));
            probe.ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
            
        }


    }

}
