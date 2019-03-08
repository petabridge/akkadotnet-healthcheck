// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeSubscriptionTest.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using FluentAssertions;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeSubscriptionTest : TestKit.Xunit.TestKit
    {

        public AkkaPersistenceLivenessProbeSubscriptionTest(ITestOutputHelper helper)
            : base(output: helper)
        {

        }
        [Fact(DisplayName = "AkkaPersistenceLivenessProbe_Should_Handle_Subscriptions_In_Any_State")]
        public void AkkaPersistenceLivenessProbe_Should_Handle_Subscriptions_In_Any_State()
        {
            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();
            AwaitAssert(() => ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue());




        }


    }

}
