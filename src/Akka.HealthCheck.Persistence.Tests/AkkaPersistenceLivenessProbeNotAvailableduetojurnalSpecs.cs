// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbeNotAvailableduetojurnalSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Akka.HealthCheck.Persistence.AkkaPersistenceLivenessProbe;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeNotAvailableduetojurnalSpecs : TestKit.Xunit.TestKit
    {
        public AkkaPersistenceLivenessProbeNotAvailableduetojurnalSpecs(ITestOutputHelper helper)
                    : base(config, output: helper)
        {
        }
        public static string config = @"akka.persistence {
                                         
                                         journal {
                                                    
                                                    plugin = ""akka.persistence.journal.sqlite""
                                                    recovery-event-timeout = 2s
                                                    circuit-breaker{reset-timeout = 2s}
                                                    sqlite {
                                                            class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                                                            auto-initialize = on
                                                            connection-string = ""Fake=file:memdb.db;Mode=Memory;Cache=Shared""
                                                     }}
                                         snapshot-store {
                                                plugin = ""akka.persistence.snapshot-store.sqlite""
                                                sqlite {
                                                class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                                                auto-initialize = on
                                                connection-string = ""Filename=file:memdb.db;Mode=Memory;Cache=Shared""
                       }
                   }}";

        [Fact(DisplayName = " ActorSystem should correcly report when Akk.Persistence is unavailable due to bad journal configuration")]
        public void AkkaPersistenceLivenessProbeProvidert_Should_Report_Akka_Persistance_Is_Unavailable_With_Bad_Journal_Setup()
        {
            
            
            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            AwaitAssert(() => ExpectNoMsg(), TimeSpan.FromSeconds(35));
            ProbActor.Tell(GetCurrentLiveness.Instance);
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();

        }
    }
}
