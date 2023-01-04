// -----------------------------------------------------------------------
// <copyright file="ReadinessTransportActorSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Tests.Transports;
using Akka.HealthCheck.Transports;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests.Readiness
{
    public class ReadinessTransportActorSpecs : TestKit.Xunit.TestKit
    {
        public ReadinessTransportActorSpecs(ITestOutputHelper helper)
            : base(output: helper)
        {
        }

        [Fact(DisplayName = "ReadinessTransportActor should crash and during try or stop failure")]
        public void ReadinessTransportActor_should_crash_when_Stop_or_Go_failure()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(false, false, TimeSpan.Zero));
            var fakeReadiness = CreateTestProbe("readiness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeReadiness }.ToImmutableDictionary();;

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, dict, true)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();

            // we expect the ReadinessTransportActor to throw this exception
            EventFilter.Exception<ProbeUpdateException>().ExpectOne(() =>
            {
                fakeReadiness.Reply(new ReadinessStatus(true));

                AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                     && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);
            });


            // actor should crash and restart here
            fakeReadiness.ExpectMsg<SubscribeToReadiness>();

            // should throw second exception when we try to change status again
            EventFilter.Exception<ProbeUpdateException>().ExpectOne(() =>
            {
                fakeReadiness.Reply(new ReadinessStatus(false));

                AwaitCondition(() => testTransport.SystemCalls.Count == 4
                                     && testTransport.SystemCalls.Count(x =>
                                         x == TestStatusTransport.TransportCall.Go) == 1
                                     && testTransport.SystemCalls.Count(
                                         x => x == TestStatusTransport.TransportCall.Stop) == 3);
            });
        }


        [Fact(DisplayName =
            "ReadinessTransportActor should crash and try to stop signal upon timeout during signal change")]
        public void ReadinessTransportActor_should_crash_when_Timedout()
        {
            var testTransport =
                new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.FromSeconds(1.5)));
            var fakeReadiness = CreateTestProbe("readiness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeReadiness }.ToImmutableDictionary();;

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, dict, true)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness.Reply(new ReadinessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness.Reply(new ReadinessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 4
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Go) == 1
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Stop) == 3,
                5.Seconds(), 100.Milliseconds());
        }

        [Fact(DisplayName = "ReadinessTransportActor should process Go and Stop signals successfully")]
        public void ReadinessTransportActor_should_process_Go_and_Stop_successfully()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeReadiness = CreateTestProbe("readiness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeReadiness }.ToImmutableDictionary();

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, dict, true)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness.Reply(new ReadinessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeReadiness.Reply(new ReadinessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[1] == TestStatusTransport.TransportCall.Stop);
        }

        [Fact(DisplayName = "ReadinessTransportActor should send Stop signal when terminated")]
        public void ReadinessTransportActor_should_send_Stop_when_terminated()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeReadiness = CreateTestProbe("readiness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeReadiness }.ToImmutableDictionary();

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, dict, true)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            Watch(transportActor);
            Sys.Stop(transportActor);

            ExpectTerminated(transportActor);
            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
        }
        
        [Fact(DisplayName = "ReadinessTransportActor with multiple probes should report correctly based on probe responses")]
        public async Task ReadinessTransportActorMultiProbeTest()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeReadiness1 = CreateTestProbe("readiness_1");
            var fakeReadiness2 = CreateTestProbe("readiness_2");
            var dict = new Dictionary<string, IActorRef>
            {
                ["first"] = fakeReadiness1,
                ["second"] = fakeReadiness2
            }.ToImmutableDictionary();

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, dict, true)));

            fakeReadiness1.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness2.ExpectMsg<SubscribeToReadiness>();

            // "second" status should still be false because it has not reported in yet
            transportActor.Tell(new ReadinessStatus(true), fakeReadiness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 1 
                && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
            
            // both probe status is true, Go should be called
            transportActor.Tell(new ReadinessStatus(true), fakeReadiness2);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 2 
                && testTransport.SystemCalls[1] == TestStatusTransport.TransportCall.Go);
            
            // probes reported true, Go should be called all the time
            foreach (var i in Enumerable.Range(2, 8))
            {
                transportActor.Tell(new ReadinessStatus(true), i % 2 == 0 ? fakeReadiness1 : fakeReadiness2);
                await AwaitConditionAsync(() => 
                    testTransport.SystemCalls.Count == i + 1
                    && testTransport.SystemCalls[i] == TestStatusTransport.TransportCall.Go);
            }
            
            // Stop should be called as soon as one of the probe failed
            transportActor.Tell(new ReadinessStatus(false), fakeReadiness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 11
                && testTransport.SystemCalls[10] == TestStatusTransport.TransportCall.Stop);
            
            // Go should be called again as soon as the failing probe reports true
            transportActor.Tell(new ReadinessStatus(true), fakeReadiness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 12
                && testTransport.SystemCalls[11] == TestStatusTransport.TransportCall.Go);

            // Stop should be called when a probe died
            Watch(fakeReadiness1);
            fakeReadiness1.Tell(PoisonPill.Instance);
            ExpectTerminated(fakeReadiness1);
            Unwatch(fakeReadiness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 13
                && testTransport.SystemCalls[12] == TestStatusTransport.TransportCall.Stop);
            
            // transport actor should stop when all probe died
            Watch(fakeReadiness2);
            Watch(transportActor);
            fakeReadiness2.Tell(PoisonPill.Instance);
            ExpectTerminated(fakeReadiness2);
            ExpectTerminated(transportActor);
            
            // Last Stop call from PostStop
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 14
                && testTransport.SystemCalls[13] == TestStatusTransport.TransportCall.Stop);
        }        
    }
}