// -----------------------------------------------------------------------
// <copyright file="ReadinessTransportActorSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Tests.Transports;
using Akka.HealthCheck.Transports;
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

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, fakeReadiness)));

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

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, fakeReadiness)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness.Reply(new ReadinessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            fakeReadiness.Reply(new ReadinessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 4
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Go) == 1
                                 && testTransport.SystemCalls.Count(x => x == TestStatusTransport.TransportCall.Stop) ==
                                 3);
        }

        [Fact(DisplayName = "ReadinessTransportActor should process Go and Stop signals successfully")]
        public void ReadinessTransportActor_should_process_Go_and_Stop_successfully()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeReadiness = CreateTestProbe("readiness");

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, fakeReadiness)));

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

            var transportActor =
                Sys.ActorOf(Props.Create(() => new ReadinessTransportActor(testTransport, fakeReadiness)));

            fakeReadiness.ExpectMsg<SubscribeToReadiness>();
            Watch(transportActor);
            Sys.Stop(transportActor);

            ExpectTerminated(transportActor);
            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
        }
    }
}