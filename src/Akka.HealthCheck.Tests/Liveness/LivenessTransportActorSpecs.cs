using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Tests.Transports;
using Akka.HealthCheck.Transports;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests.Liveness
{
    public class LivenessTransportActorSpecs : TestKit.Xunit.TestKit
    {
        public LivenessTransportActorSpecs(ITestOutputHelper helper) 
            : base(output: helper) { }

        [Fact(DisplayName = "LivenessTransportActor should process Go and Stop signals successfully")]
        public void LivenessTransportActor_should_process_Go_and_Stop_successfully()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.Zero));
            var fakeLiveness = CreateTestProbe("liveness");

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, fakeLiveness)));

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

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, fakeLiveness)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            Watch(transportActor);
            Sys.Stop(transportActor);

            ExpectTerminated(transportActor);
            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Stop);
        }


        [Fact(DisplayName = "LivenessTransportActor should crash and try to stop signal upon timeout during signal change")]
        public void LivenessTransportActor_should_crash_when_Timedout()
        {
            var testTransport = new TestStatusTransport(new TestStatusTransportSettings(true, true, TimeSpan.FromSeconds(1.5)));
            var fakeLiveness = CreateTestProbe("liveness");

            var transportActor =
                Sys.ActorOf(Props.Create(() => new LivenessTransportActor(testTransport, fakeLiveness)));

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness.Reply(new LivenessStatus(true));

            AwaitCondition(() => testTransport.SystemCalls.Count == 1
                                 && testTransport.SystemCalls[0] == TestStatusTransport.TransportCall.Go);

            fakeLiveness.ExpectMsg<SubscribeToLiveness>();
            fakeLiveness.Reply(new LivenessStatus(false));
            AwaitCondition(() => testTransport.SystemCalls.Count == 2
                                 && testTransport.SystemCalls[1] == TestStatusTransport.TransportCall.Stop);
        }
    }
}
