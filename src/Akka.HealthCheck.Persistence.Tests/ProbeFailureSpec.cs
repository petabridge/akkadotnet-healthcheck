﻿// -----------------------------------------------------------------------
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
        
        private readonly string _id = Guid.NewGuid().ToString("N");
        private int _count;
        
        public ProbeFailureSpec(ITestOutputHelper output) : base("akka.loglevel = DEBUG", nameof(ProbeFailureSpec), output)
        {
        }

        [Fact(DisplayName = "First probe should report that probe is still warming up")]
        public void SuccessfulFirstProbeTest()
        {
            var status = PerformProbe(); 
            status.IsLive.Should().BeFalse();
            status.JournalRecovered.Should().BeNull();
            status.JournalPersisted.Should().BeTrue();
            status.SnapshotRecovered.Should().BeNull();
            status.SnapshotSaved.Should().BeTrue();
            status.StatusMessage.Should().StartWith("Warming up probe.");
            status.Failures.Should().BeNull();
        }
        
        [Fact(DisplayName = "Status should reflect successful probe")]
        public void SuccessfulProbeTest()
        {
            AssertFirstProbe();
            var status = PerformProbe();
            status.IsLive.Should().BeTrue();
            status.JournalRecovered.Should().BeTrue();
            status.JournalPersisted.Should().BeTrue();
            status.SnapshotRecovered.Should().BeTrue();
            status.SnapshotSaved.Should().BeTrue();
            status.Failures.Should().BeNull();
        }
        
        [Fact(DisplayName = "Journal persist failed on first probe, snapshot should still be reported as saved")]
        public async Task JournalPersistFailOnFirstTest()
        {
            await WithJournalWrite(write => write.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeNull();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeNull();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestJournalFailureException>();
                status.StatusMessage.Should().NotStartWith("Warming up probe.");
            });
        }
        
        [Fact(DisplayName = "Journal persist rejected on first probe, snapshot should still be reported as saved")]
        public async Task JournalPersistRejectedOnFirstTest()
        {
            await WithJournalWrite(write => write.Reject(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeNull();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeNull();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestJournalRejectionException>();
                status.StatusMessage.Should().NotStartWith("Warming up probe.");
            });
        }
        
        [Fact(DisplayName = "Snapshot failed to save on first probe, journal should still be reported as persisted")]
        public async Task SnapshotSaveFailOnFirstTest()
        {
            await WithSnapshotSave(save => save.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeNull();
                status.JournalPersisted.Should().BeTrue();
                status.SnapshotRecovered.Should().BeNull();
                status.SnapshotSaved.Should().BeFalse();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestSnapshotStoreFailureException>();
                status.StatusMessage.Should().NotStartWith("Warming up probe.");
            });
        }
        
        [Fact(DisplayName = "Journal recovery failed, snapshot should still be reported as recovered")]
        public async Task JournalRecoverFailTest()
        {
            AssertFirstProbe();
            await WithJournalRecovery(recover => recover.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeFalse();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeFalse();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestJournalFailureException>();
            });
        }

        [Fact(DisplayName = "Snapshot recovery failed, journal should not be recovered")]
        public async Task SnapshotRecoverFailTest()
        {
            AssertFirstProbe();
            await WithSnapshotLoad(load => load.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeFalse();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeFalse();
                status.SnapshotSaved.Should().BeFalse();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestSnapshotStoreFailureException>();
            });
        }
        
        [Fact(DisplayName = "Journal failed to persist, snapshot should still be reported as saved")]
        public async Task JournalPersistFailTest()
        {
            AssertFirstProbe();
            await WithJournalWrite(write => write.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeTrue();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestJournalFailureException>();
            });
        }

        [Fact(DisplayName = "Journal persist rejected, snapshot should still be reported as saved")]
        public async Task JournalPersistRejectedTest()
        {
            AssertFirstProbe();
            await WithJournalWrite(write => write.Reject(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeTrue();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestJournalRejectionException>();
            });
        }

        [Fact(DisplayName = "Snapshot failed to save, journal should still be reported as saved")]
        public async Task SnapshotSaveFailTest()
        {
            AssertFirstProbe();
            await WithSnapshotSave(write => write.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeTrue();
                status.JournalPersisted.Should().BeTrue();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeFalse();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestSnapshotStoreFailureException>();
            });
        }

        [Fact(DisplayName = "Snapshot delete failed, everything should be true with exception")]
        public async Task SnapshotDeleteFailTest()
        {
            AssertFirstProbe();
            await WithSnapshotDelete(delete => delete.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeTrue();
                status.JournalPersisted.Should().BeTrue();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestSnapshotStoreFailureException>();
            });
        }
        
        // Could not test journal delete failed test, TestJournal does not expose journal delete behavior
        /*
        [Fact(DisplayName = "Journal delete failed, everything should be true with exception")]
        public async Task JournalDeleteFailTest()
        {
            AssertFirstProbe();
            await WithJournalDelete(delete => delete.Fail(), () =>
            {
                var status = PerformProbe();
                status.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeTrue();
                status.JournalPersisted.Should().BeTrue();
                status.SnapshotRecovered.Should().BeTrue();
                status.SnapshotSaved.Should().BeTrue();
                var e = status.Failures!.Flatten().InnerExceptions[0];
                e.Should().BeOfType<TestSnapshotStoreFailureException>();
            });
        }
        */

        [Fact(DisplayName = "AkkaPersistenceLivenessProbe should retry probe if SaveSnapshot failed")]
        public async Task SaveSnapshotFailTest()
        {
            await WithSnapshotSave(
                save => save.SetInterceptorAsync(new SnapshotInterceptors.DelayOnce(1.Seconds(), new SnapshotInterceptors.Failure(1))),
                async () =>
                {
                    var probeActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(true, 250.Milliseconds(), 500.Seconds())));
                    probeActor.Tell(new SubscribeToLiveness(TestActor));
                
                    probeActor.Tell(GetCurrentLiveness.Instance);
                    var status = ExpectMsg<LivenessStatus>();
                    status.IsLive.Should().BeFalse();
                    status.StatusMessage.Should().StartWith("Warming up probe.");

                    var failStatus = await FishForMessageAsync<PersistenceLivenessStatus>(
                        msg => !msg.StatusMessage.StartsWith("Warming up probe."), 
                        6.Seconds());

                    failStatus.IsLive.Should().BeFalse();
                    failStatus.JournalPersisted.Should().BeTrue();
                    failStatus.JournalRecovered.Should().BeNull();
                    failStatus.SnapshotSaved.Should().BeFalse();
                    failStatus.SnapshotRecovered.Should().BeNull();
                
                    failStatus = await ExpectMsgAsync<PersistenceLivenessStatus>(6.Seconds());
                
                    failStatus.IsLive.Should().BeFalse();
                    failStatus.JournalPersisted.Should().BeTrue();
                    failStatus.JournalRecovered.Should().BeTrue();
                    failStatus.SnapshotSaved.Should().BeTrue();
                    failStatus.SnapshotRecovered.Should().BeNull();
                
                    var successStatus = await ExpectMsgAsync<PersistenceLivenessStatus>(6.Seconds());
                    successStatus.IsLive.Should().BeTrue();
                });
        }
        
        private PersistenceLivenessStatus PerformProbe()
        {
            var first = _count == 0;
            _count++;
            var liveProbe = ActorOf(() => new SuicideProbe(TestActor, first, _id, true));
            Watch(liveProbe);
            liveProbe.Tell("hit");
            var status = ExpectMsg<PersistenceLivenessStatus>();
            ExpectTerminated(liveProbe);
            Unwatch(liveProbe);
            
            return status;
        }
        
        private void AssertFirstProbe()
        {
            if (_count != 0)
                throw new Exception("Must be called as the first probe!");
            
            var status = PerformProbe(); 
            status.JournalRecovered.Should().BeNull();
            status.JournalPersisted.Should().BeTrue();
            status.SnapshotRecovered.Should().BeNull();
            status.SnapshotSaved.Should().BeTrue();
            status.StatusMessage.Should().StartWith("Warming up probe.");
            status.Failures.Should().BeNull();
        }
    }
}