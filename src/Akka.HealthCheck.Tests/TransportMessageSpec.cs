using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class TransportMessageSpec : TestKit.Xunit.TestKit
    {
        public TransportMessageSpec(ITestOutputHelper helper) : base(GetConfig(), output: helper)
        {
        }

        private static Config GetConfig()
        {
            var PortNumber = ThreadLocalRandom.Current.Next(10000, 64000);

            Config HealthcheckConfig = @"
log-config-on-start = off
            log-info = off
            akka.healthcheck{
                liveness{
                    transport = tcp
                    tcp.port = " + PortNumber + @"
                }
            }
        ";
            return HealthcheckConfig;
        }
        [Fact(DisplayName ="Should show debugging message regarding liveness transport tcp type")]
        public void Should_Show_Debugg_Messages_Regarding_Transport()
        {
            var healthCheck = AkkaHealthCheck.For(Sys);
            
            var eventFilter = new EventFilterFactory(new TestKit.Xunit.TestKit(Sys));
            eventFilter.Info(message: "Liveness TCP transport created. Bound to port");

        }
    }
}
