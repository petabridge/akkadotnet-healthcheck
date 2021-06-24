// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheckIntegrationSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Transports.Files;
using Akka.HealthCheck.Transports.Sockets;
using Akka.Util;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class AkkaHealthCheckIntegrationSpec : TestKit.Xunit.TestKit
    {
        public AkkaHealthCheckIntegrationSpec(ITestOutputHelper helper)
            : base(GetConfig(), output: helper)
        {
        }



        public static Config GetConfig()
        {
            var PortNumber = ThreadLocalRandom.Current.Next(10000, 64000);

            Config HealthcheckConfig = @"
            akka.healthcheck{
                liveness{
                    transport = tcp
                    tcp.port = " + PortNumber + @"
                }

                readiness{
                    transport = file
                    file.path = ""readiness-custom.txt""
                }
            }
        ";
            return HealthcheckConfig;
        }

        [Fact(DisplayName = "End2End: should load complete custom Akka.HealthCheck config")]
        public async Task Should_load_custom_HealthCheck_system_correctly()
        {
            // forces the plugin to load
            var healthCheck = AkkaHealthCheck.For(Sys);
            var readinessSubscriber = CreateTestProbe();
            var livenessSubscriber = CreateTestProbe();

            healthCheck.LivenessProbe.Tell(new SubscribeToLiveness(livenessSubscriber));
            healthCheck.ReadinessProbe.Tell(new SubscribeToReadiness(readinessSubscriber));

            livenessSubscriber.ExpectMsg<LivenessStatus>().IsLive.Should().BeTrue();
            readinessSubscriber.ExpectMsg<ReadinessStatus>().IsReady.Should().BeTrue();

            var filePath = healthCheck.Settings.ReadinessTransportSettings.As<FileTransportSettings>().FilePath;
            var tcpPort = healthCheck.Settings.LivenessTransportSettings.As<SocketTransportSettings>().Port;

            // check to see that our probes are up and running using the supplied transports
            AwaitCondition(() => File.Exists(filePath));
            var tcpClient = new TcpClient(AddressFamily.InterNetwork);
            await tcpClient.ConnectAsync(IPAddress.Loopback, tcpPort);

            // force shutdown of the ActorSystem and verify that probes are stopped
            await Sys.Terminate();

            // Readiness probe should not exist
            AwaitCondition(() => !File.Exists(filePath));

            //Created a new client to see if it would be able to connect. 
            var tcpClient2 = new TcpClient(AddressFamily.InterNetwork);

            // liveness probe should be disconnected
            tcpClient2.Awaiting(client => client.ConnectAsync(IPAddress.Loopback, tcpPort))
                .Should().Throw<SocketException>();
            
            //Second client should not be able to connect as socket has been closed
            AwaitCondition(()=> !tcpClient2.Connected);
        }
    }
}