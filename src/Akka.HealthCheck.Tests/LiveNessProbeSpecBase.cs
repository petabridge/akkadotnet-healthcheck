// -----------------------------------------------------------------------
// <copyright file="LiveNessProbeSpecBase.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    /// <summary>
    ///     Base class for testing the ability for various probe implementations
    /// </summary>
    public abstract class LivenessProbeSpecBase : TestKit.Xunit.TestKit
    {
        protected LivenessProbeSpecBase(ITestOutputHelper helper) : this(helper, Config.Empty)
        {
        }

        protected LivenessProbeSpecBase(ITestOutputHelper helper, Config config) : base(config, output: helper)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            LivenessProbe = Sys.ActorOf(LivenessProbeProps);
        }

        protected abstract Props LivenessProbeProps { get; }

        protected IActorRef LivenessProbe { get; }

        /// <summary>
        ///     Verify that deathwatch is being used correctly
        /// </summary>
        [Fact(DisplayName = "Should not crash if liveness probe subscriber dies")]
        public void Should_not_crash_if_LivenessSubscriber_dies()
        {
            var tempActor = Sys.ActorOf(act => act.ReceiveAny((_, ctx) => TestActor.Forward(_)));
            Watch(tempActor);
            LivenessProbe.Tell(new SubscribeToLiveness(tempActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
            EventFilter.Error().Expect(0, () =>
            {
                Sys.Stop(tempActor);
                ExpectTerminated(tempActor);
            });
        }

        [Fact(DisplayName = "Should be able to receive the current LivenessStatus from the probe")]
        public void Should_receive_current_LivenessStatus_from_probe()
        {
            LivenessProbe.Tell(GetCurrentLiveness.Instance);
            ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
        }

        [Fact(DisplayName = "Should be able to subscribe to the current LivenessStatus from the probe")]
        public void Should_subscribe_to_LivenessStatus_from_probe()
        {
            LivenessProbe.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
        }
    }
}