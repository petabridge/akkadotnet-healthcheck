// -----------------------------------------------------------------------
// <copyright file="LivenessTransportActorSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Tests.Transports;
using Akka.HealthCheck.Transports;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests.Liveness
{
    public class LivenessTransportActorSpecs : TestKit.Xunit.TestKit
    {
        public LivenessTransportActorSpecs(ITestOutputHelper helper)
            : base(output: helper)
        {
        }

        [Fact(DisplayName = "LivenessTransportActor should crash and during try or stop failure")]
        public void LivenessTransportActor_should_crash_when_Stop_or_Go_failure()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(false, false, TimeSpan.Zero));
            var fakeLiveness = CreateTestProbe("liveness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeLiveness }.ToImmutableDictionary();;

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, dict, true)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();

            // we expect the LivenessTransportActor to throw this exception
            EventFilter.Exception<ProbeUpdateException>().ExpectOne(() =>
            {
                fakeLiveness.Reply(new LivenessStatus(true));

                AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                     && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);
            });


            // actor should crash and restart here
            fakeLiveness.ExpectMsg<SubscribeToLiveness>();

            // should throw second exception when we try to change status again
            EventFilter.Exception<ProbeUpdateException>().ExpectOne(() =>
            {
                fakeLiveness.Reply(new LivenessStatus(false));

                AwaitCondition(() => testTransport.SystemCalls.Count == 4
                                     && testTransport.SystemCalls.Count(x =>
                                         x == TestStatusTransport.TransportCall.Go) == 1
                                     && testTransport.SystemCalls.Count(
                                         x => x == TestStatusTransport.TransportCall.Stop) == 3);
            });
        }

        [Fact(DisplayName =
            "LivenessTransportActor should crash and try to stop signal upon timeout during signal change")]
        public void LivenessTransportActor_should_crash_when_Timedout()
        {
            var testTransport =
                new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.FromSeconds(1.5)));
            var fakeLiveness = CreateTestProbe("liveness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeLiveness }.ToImmutableDictionary();;

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, dict, true)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness.Reply(new LivenessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness.Reply(new LivenessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 4
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Go) == 1
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Stop) == 3,
                5.Seconds(), 100.Milliseconds());
        }

        [Fact(DisplayName = "LivenessTransportActor should process Go and Stop signals successfully")]
        public void LivenessTransportActor_should_process_Go_and_Stop_successfully()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeLiveness = CreateTestProbe("liveness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeLiveness }.ToImmutableDictionary();;

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, dict, true)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness.Reply(new LivenessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeLiveness.Reply(new LivenessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[1] == TestStatusTransport.TransportCall.Stop);
        }

        [Fact(DisplayName = "LivenessTransportActor should send Stop signal when terminated")]
        public void LivenessTransportActor_should_send_Stop_when_terminated()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeLiveness = CreateTestProbe("liveness");
            var dict = new Dictionary<string, IActorRef> { ["default"] = fakeLiveness }.ToImmutableDictionary();

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, dict, true)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            Watch(transportActor);
            Sys.Stop(transportActor);

            ExpectTerminated(transportActor);
            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
        }
        
        [Fact(DisplayName = "LivenessTransportActor with multiple probes should report correctly based on probe responses")]
        public async Task LivenessTransportActorMultiProbeTest()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeLiveness1 = CreateTestProbe("liveness_1");
            var fakeLiveness2 = CreateTestProbe("liveness_2");
            var dict = new Dictionary<string, IActorRef>
            {
                ["first"] = fakeLiveness1,
                ["second"] = fakeLiveness2
            }.ToImmutableDictionary();

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, dict, true)));

            fakeLiveness1.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness2.ExpectMsg<SubscribeToLiveness>();

            // "second" status should still be false because it has not reported in yet
            transportActor.Tell(new LivenessStatus(true), fakeLiveness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 1 
                && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
            
            // both probe status is true, Go should be called
            transportActor.Tell(new LivenessStatus(true), fakeLiveness2);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 2 
                && testTransport.SystemCalls[1] == TestStatusTransport.TransportCall.Go);
            
            // probes reported true, Go should be called all the time
            foreach (var i in Enumerable.Range(2, 8))
            {
                transportActor.Tell(new LivenessStatus(true), i % 2 == 0 ? fakeLiveness1 : fakeLiveness2);
                await AwaitConditionAsync(() => 
                    testTransport.SystemCalls.Count == i + 1
                    && testTransport.SystemCalls[i] == TestStatusTransport.TransportCall.Go);
            }
            
            // Stop should be called as soon as one of the probe failed
            transportActor.Tell(new LivenessStatus(false), fakeLiveness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 11
                && testTransport.SystemCalls[10] == TestStatusTransport.TransportCall.Stop);
            
            // Go should be called again as soon as the failing probe reports true
            transportActor.Tell(new LivenessStatus(true), fakeLiveness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 12
                && testTransport.SystemCalls[11] == TestStatusTransport.TransportCall.Go);

            // Stop should be called when a probe died
            Watch(fakeLiveness1);
            fakeLiveness1.Tell(PoisonPill.Instance);
            ExpectTerminated(fakeLiveness1);
            Unwatch(fakeLiveness1);
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 13
                && testTransport.SystemCalls[12] == TestStatusTransport.TransportCall.Stop);
            
            // transport actor should stop when all probe died
            Watch(fakeLiveness2);
            Watch(transportActor);
            fakeLiveness2.Tell(PoisonPill.Instance);
            ExpectTerminated(fakeLiveness2);
            ExpectTerminated(transportActor);
            
            // Last Stop call from PostStop
            await AwaitConditionAsync(() => 
                testTransport.SystemCalls.Count == 15
                && testTransport.SystemCalls[14] == TestStatusTransport.TransportCall.Stop);
        }
        
    }
}