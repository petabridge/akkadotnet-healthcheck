// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeNotAvailableDueToSnapshotStoreSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Util.Internal;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbe;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeNotAvailableDueToSnapshotStoreSpecs : TestKit.Xunit.TestKit
    {
        public AkkaPersistenceLivenessProbeNotAvailableDueToSnapshotStoreSpecs(ITestOutputHelper helper)
                    : base(config, output: helper)
        {
        }
        private static AtomicCounter counter = new AtomicCounter(0);

        public static string config = @"akka.persistence {
                                         journal { 
                                                    plugin = ""akka.persistence.journal.sqlite""
                                                    recovery-event-timeout = 2s
                                                    circuit-breaker.reset-timeout = 2s
                                                    sqlite {
                                                            class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                                                            auto-initialize = on
                                                            connection-string = ""Filename=file:memdb-" + counter.IncrementAndGet() + @".db;Mode=Memory;Cache=Shared""                  
                                                     }}
                                         snapshot-store {
                                                plugin = ""akka.persistence.snapshot-store.sqlite""
                                                sqlite {
                                                class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                                                auto-initialize = on
                                                connection-string = ""Filename=file:memdb-" + counter.IncrementAndGet() + @".db;Mode=Memory;Cache=Shared"" #Invalid connetion string
                                                     }
                                          }}";

        [Fact(DisplayName = " ActorSystem should correcly report when Akk.Persistence is unavailable due to bad snapshot-store configuration")]
        public void AkkaPersistenceLivenessProbeProvidert_Should_Report_Akka_Persistance_Is_Unavailable_With_Bad_Snapshot_Store_Setup()
        {

            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse("System should not be live");
            ExpectMsg<LivenessStatus>(TimeSpan.FromMinutes(1)).IsLive.Should().BeFalse("System should not be live");

        }
    }
}
