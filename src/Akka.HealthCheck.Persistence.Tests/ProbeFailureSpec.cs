// -----------------------------------------------------------------------
// <copyright file="ProbeFailureSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Persistence.TestKit;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class ProbeFailureSpec: PersistenceTestKit
    {
        public ProbeFailureSpec(ITestOutputHelper output) : base("akka.loglevel = DEBUG", nameof(ProbeFailureSpec), output)
        {
        }

        [Fact(DisplayName = "Status should reflect successful probe")]
        public void SuccessfulProbeTest()
        {
            PerformWarmup();
            var status = PerformProbe();
            status.IsLive.Should().BeTrue();
            status.Failure.Should().BeNull();
        }
        
        [Fact(DisplayName = "Journal recovery failed, probe should fail")]
        public async Task JournalRecoverFailTest()
        {
            await WithJournalRecovery(recover => recover.Fail(), () =>
            {
                PerformWarmup();
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                var e = status.Failure;
                e.Should().NotBeNull().And.BeOfType<TestJournalFailureException>();
            });
        }

        [Fact(DisplayName = "Snapshot recovery failed, probe should fail")]
        public async Task SnapshotRecoverFailTest()
        {
            await WithSnapshotLoad(load => load.Fail(), () =>
            {
                PerformWarmup(true);
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                var e = status.Failure!;
                e.Should().NotBeNull().And.BeOfType<TestSnapshotStoreFailureException>();
            });
        }

        private void PerformWarmup(bool expectFailed = false)
        {
            var warmupProbe = ActorOf(SuicideWarmupProbe.Props(TestActor, AkkaPersistenceLivenessProbe.PersistenceId, true));
            Watch(warmupProbe);
            if(expectFailed)
                ExpectMsg<WarmupFailed>();
            else
                ExpectMsg<WarmupComplete>();
            ExpectTerminated(warmupProbe);
            Unwatch(warmupProbe);
        }

        private PersistenceLivenessStatus PerformProbe()
        {
            var liveProbe = ActorOf(SuicideProbe.Props(TestActor, AkkaPersistenceLivenessProbe.PersistenceId, true));
            Watch(liveProbe);
            var status = ExpectMsg<PersistenceLivenessStatus>();
            ExpectTerminated(liveProbe);
            Unwatch(liveProbe);
            
            return status;
        }

        [Fact(DisplayName = "Failures should progressively change from healthy to degraded to unhealthy")]
        public async Task FailStatusProgressionTest()
        {
            const int failureThreshold = 3;
            var probe = Sys.ActorOf(AkkaPersistenceLivenessProbe.PersistentHealthCheckProps(true, 500.Milliseconds(), 3.Seconds(), failureThreshold));
            probe.Tell(new SubscribeToLiveness(TestActor), TestActor);

            // wait until probe returns a healthy status
            FishForMessage<PersistenceLivenessStatus>(s => s.Status is AkkaHealthStatus.Healthy);
            
            await WithSnapshotLoad(load => load.Fail(), async () =>
            {
                PersistenceLivenessStatus status;

                // wait until probe detects first failure
                // needed to avoid race condition with decoupled suicide actor
                FishForMessage<PersistenceLivenessStatus>(s => s.Status is AkkaHealthStatus.Degraded);
                
                // Below failure threshold, probe should report degraded
                // first failure test already handled above
                for (var i = 0; i < failureThreshold - 1; i++)
                {
                    status = await ExpectMsgAsync<PersistenceLivenessStatus>();
                    status.Status.Should().Be(AkkaHealthStatus.Degraded);
                }
                
                // exceeding failure threshold, probe should report unhealthy
                status = await ExpectMsgAsync<PersistenceLivenessStatus>();
                status.Status.Should().Be(AkkaHealthStatus.Unhealthy);
            });
        }
    }
}