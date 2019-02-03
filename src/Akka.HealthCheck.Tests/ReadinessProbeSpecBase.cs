// -----------------------------------------------------------------------
// <copyright file="ReadinessProbeSpecBase.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Configuration;
using Akka.HealthCheck.Readiness;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    /// <summary>
    ///     Base class for testing the ability for various probe implementations
    /// </summary>
    public abstract class ReadinessProbeSpecBase : TestKit.Xunit.TestKit
    {
        protected ReadinessProbeSpecBase(ITestOutputHelper helper) : this(helper, Config.Empty)
        {
        }

        protected ReadinessProbeSpecBase(ITestOutputHelper helper, Config config) : base(config, output: helper)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            ReadinessProbe = Sys.ActorOf(ReadinessProbeProps);
        }

        protected abstract Props ReadinessProbeProps { get; }

        protected IActorRef ReadinessProbe { get; }

        /// <summary>
        ///     Verify that deathwatch is being used correctly
        /// </summary>
        [Fact(DisplayName = "Should not crash if readiness probe subscriber dies")]
        public void Should_not_crash_if_ReadinessSubscriber_dies()
        {
            var tempActor = Sys.ActorOf(act => act.ReceiveAny((_, ctx) => TestActor.Forward(_)));
            Watch(tempActor);
            ReadinessProbe.Tell(new SubscribeToReadiness(tempActor));
            ExpectMsg<ReadinessStatus>().IsReady.Should().BeTrue();
            EventFilter.Error().Expect(0, () =>
            {
                Sys.Stop(tempActor);
                ExpectTerminated(tempActor);
            });
        }

        [Fact(DisplayName = "Should be able to receive the current ReadinessStatus from the probe")]
        public void Should_receive_current_LivenessStatus_from_probe()
        {
            ReadinessProbe.Tell(GetCurrentReadiness.Instance);
            ExpectMsg<ReadinessStatus>().IsReady.Should().BeTrue();
        }

        [Fact(DisplayName = "Should be able to subscribe to the current ReadinessStatus from the probe")]
        public void Should_subscribe_to_ReadinessStatus_from_probe()
        {
            ReadinessProbe.Tell(new SubscribeToReadiness(TestActor));
            ExpectMsg<ReadinessStatus>().IsReady.Should().BeTrue();
        }
    }
}