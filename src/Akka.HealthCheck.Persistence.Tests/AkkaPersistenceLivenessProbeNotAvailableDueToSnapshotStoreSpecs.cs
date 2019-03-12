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
                    : base(TestConfig.badSnapshotConfig, output: helper)
        {
        }


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
