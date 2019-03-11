using Akka.Actor;
using Akka.HealthCheck.Liveness;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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
                                                            class = ""Akka.Persistence.False.Journal.FalseSQLIte, Akka.Persistence.Sqlite.Fake""
                                                            auto-initialize = on
                                                            connection-string = ""FakeFilename:memdb.db;Mode=Memory;Cache=Shared""
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
        public async Task AkkaPersistenceLivenessProbeProvidert_Should_Report_Akka_Persistance_Is_Unavailable_With_Bad_Journal_Setup()
        {
            var healthCheck = AkkaHealthCheck.For(Sys);
            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse();
            var livenessStatus =
                    await healthCheck.LivenessProbe.Ask<LivenessStatus>(GetCurrentLiveness.Instance,
                        TimeSpan.FromSeconds(3));
            livenessStatus.IsLive.Should().BeFalse();

        }
    }
}
