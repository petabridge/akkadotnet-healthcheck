using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Persistence;
using Akka.Persistence.Journal;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests
{
    public class AkkaPersistenceLivenessProbeHandlesPersistenceBecomingUnavailableSpecs : TestKit.Xunit.TestKit
    {
        public AkkaPersistenceLivenessProbeHandlesPersistenceBecomingUnavailableSpecs(ITestOutputHelper helper)
                    : base(TestConfig.GetCustomConfigurationString, output: helper)
        {
        }

        internal class Evt
        {
            public Evt(object data)
            {
                Data = data;
            }

            public object Data { get; private set; }

            public override string ToString()
            {
                return "Evt(" + Data + ")";
            }
        }
        internal class SimulatedException : Exception
        {
            public SimulatedException()
            {
            }

            public SimulatedException(string message) : base(message)
            {
            }
        }
        internal class DoomedMemoryJournal : MemoryJournal
        {
            protected override Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
            {
                var msgs = messages.ToList();
                if (KillJournal(msgs))
                    throw new SimulatedException("Simulated Store failure");

                foreach (var w in messages)
                {
                    foreach (var p in (IEnumerable<IPersistentRepresentation>)w.Payload)
                    {
                       
                            Add(p);
                    }
                }
                return Task.FromResult((IImmutableList<Exception>)null); // all good
            }

            private bool KillJournal(IEnumerable<AtomicWrite> messages)
            {
                return
                    messages.Any(
                        a =>
                            ((IEnumerable<IPersistentRepresentation>)a.Payload).Any(
                                p => ((string)((Evt)p.Payload).Data).Contains("wrong")));
            }
        }

        [Fact(DisplayName = " ActorSystem should correcly report when Akk.Persistence is unavailable due to bad snapshot-store configuration")]
        public void AkkaPersistenceLivenessProbeProvidert_Should_Report_Akka_Persistance_Become_Unavailable()
        {

            var ProbActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(TimeSpan.FromMilliseconds(250))));
            ProbActor.Tell(new SubscribeToLiveness(TestActor));
            ExpectMsg<LivenessStatus>().IsLive.Should().BeFalse("System should not be live");
            ExpectMsg<LivenessStatus>(TimeSpan.FromMinutes(1)).IsLive.Should().BeFalse("System should not be live");

        }
    }


    



}
