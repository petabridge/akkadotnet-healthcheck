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
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class AkkaHealthCheckIntegrationSpec : TestKit.Xunit.TestKit
    {
        public AkkaHealthCheckIntegrationSpec(ITestOutputHelper helper)
            : base(HealthcheckConfig, output: helper)
        {
        }

        public static readonly Config HealthcheckConfig = @"
            akka.healthcheck{
                liveness{
                    transport = tcp
                    tcp.port = 15050
                }

                readiness{
                    transport = file
                    file.path = ""readiness-custom.txt""
                }
            }
        ";

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
            var tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, tcpPort);

            // force shutdown of the ActorSystem and verify that probes are stopped
            await Sys.Terminate();

            // Readiness probe should not exist
            AwaitCondition(() => !File.Exists(filePath));

            //Created a new client to see if it would be able to connect. 
            var tcpClient2 = new TcpClient(AddressFamily.InterNetworkV6);

            // liveness probe should be disconnected
            try
            {
                await tcpClient2.ConnectAsync(IPAddress.IPv6Loopback, tcpPort);
                var bytesRead = await tcpClient.GetStream().ReadAsync(new byte[10], 0, 10);
                bytesRead.Should().Be(0);
            }
            catch
            {
            }
            //Second client should not be able to connect as socket has been closed
            AwaitCondition(()=> !tcpClient2.Connected);
        }
    }
}