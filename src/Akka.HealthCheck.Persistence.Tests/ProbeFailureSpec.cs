﻿// -----------------------------------------------------------------------
// <copyright file="ProbeFailureSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.TestKit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class ProbeFailureSpec: PersistenceTestKit
    {
        
        private readonly string _id = Guid.NewGuid().ToString("N");
        private int _count;
        
        public ProbeFailureSpec(ITestOutputHelper output) : base(nameof(ProbeFailureSpec), output)
        {
        }

        [Fact(DisplayName = "Successful probe should alternate between not producing and producing report")]
        public void SuccessfulFirstProbeTest()
        {
            foreach (var i in Enumerable.Range(0, 10))
            {
                var isOdd = i % 2 != 0;
                var status = PerformProbe(isOdd);
                if (isOdd)
                {
                    status.Should().NotBeNull();
                    status.IsLive.Should().BeTrue();
                }
                else
                    status.Should().BeNull();
            }
        }
        
        [Fact(DisplayName = "Status should reflect successful probe")]
        public void SuccessfulProbeTest()
        {
            AssertFirstProbe();
            var status = PerformProbe();
            status!.IsLive.Should().BeTrue();
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
                status!.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeFalse();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeFalse();
                status.JournalPersisted.Should().BeFalse();
                status.SnapshotRecovered.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
                status.JournalRecovered.Should().BeFalse();
                status.JournalPersisted.Should().BeTrue();
                status.SnapshotRecovered.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
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
                var status = PerformProbe(false);
                status!.IsLive.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
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
                status!.IsLive.Should().BeFalse();
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

        private PersistenceLivenessStatus? PerformProbe(bool expectResponse = true)
        {
            _count++;
            var liveProbe = ActorOf(() => new SuicideProbe(TestActor, !expectResponse, _id));
            Watch(liveProbe);
            liveProbe.Tell($"hit-{_count}");
            var obj = ExpectMsg<object>(o => o is PersistenceLivenessStatus || (o is Terminated term && term.ActorRef.Equals(liveProbe)));
            switch (obj)
            {
                case PersistenceLivenessStatus status:
                    ExpectTerminated(liveProbe);
                    Unwatch(liveProbe);
                    return status;
                case Terminated:
                    Unwatch(liveProbe);
                    if (expectResponse)
                        throw new XunitException("Response was expected but probe was terminated");
                    return null;
                default:
                    throw new XunitException($"Unexpected response: {obj}");
            }
        }
        
        private void AssertFirstProbe()
        {
            if (_count != 0)
                throw new Exception("Must be called as the first probe!");
            
            var status = PerformProbe(false);
            status.Should().BeNull();
        }
    }
}