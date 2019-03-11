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
    public class AkkaPersistenceLivenessProbeNotAvailableSpecs : TestKit.Xunit.TestKit
    {
        public AkkaPersistenceLivenessProbeNotAvailableSpecs(ITestOutputHelper helper)
                    : base(config, output: helper)
        {
        }
        public static string config = @"akka.persistence {
                                         journal {
                                                    plugin = ""akka.persistence.journal.sqlite""
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
            IActorRef ProbActor;
            try
            {
                 ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
                ProbActor.Tell(new SubscribeToLiveness(TestActor));
            }
            catch (Akka.Actor.ActorInitializationException ex) { }
            

            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();
            AwaitAssert(() => ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue(), TimeSpan.FromSeconds(10));
            //for (var i = 0; i < 1000000000; i++) { }

            //ProbActor.Tell(GetCurrentLiveness.Instance);
            //ProbActor.ToString;
            //ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();
           
               



        }
    }
}
