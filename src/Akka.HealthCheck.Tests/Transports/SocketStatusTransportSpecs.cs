// -----------------------------------------------------------------------
// <copyright file="SocketStatusTransportSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Sockets;
using Akka.Util;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Tests.Transports
{
    public class SocketStatusTransportSpecs : TestKit.Xunit.TestKit
    {
        public SocketStatusTransportSpecs()
        {
            PortNumber = ThreadLocalRandom.Current.Next(10000, 64000);
            Transport = new SocketStatusTransport(new SocketTransportSettings(PortNumber));
        }

        public int PortNumber { get; }
        public IStatusTransport Transport { get; }

        [Fact(DisplayName = "SocketStatusTransport should successfully open and close socket signal")]
        public async Task Should_successfully_open_and_close_signal()
        {
            var result = await Transport.Go("foo", CancellationToken.None);
            result.Success.Should().BeTrue();

            var tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, PortNumber);

            var deleteResult = await Transport.Stop(null, CancellationToken.None);
            deleteResult.Success.Should().BeTrue();

            try
            {
                var bytesRead = await tcpClient.GetStream().ReadAsync(new byte[10], 0, 10);
                bytesRead.Should().Be(8);
            }
            catch
            {
            }

            var tcpClient2 = new TcpClient(AddressFamily.InterNetworkV6);
            try
            {
                await tcpClient2.ConnectAsync(IPAddress.IPv6Loopback, PortNumber);
            }
            catch { }
            tcpClient2.Connected.Should().BeFalse();
        }

        [Fact(DisplayName = "SocketTransport should idempotently close TCP signal")]
        public async Task Should_successfully_repeatedly_close_signal()
        {
            var deleteResult = await Transport.Stop(null, CancellationToken.None);
            deleteResult.Success.Should().BeTrue();

            var deleteResult2 = await Transport.Stop(null, CancellationToken.None);
            deleteResult2.Success.Should().BeTrue();
        }

        [Fact(DisplayName = "SocketTransport should idempotently open TCP signal")]
        public async Task Should_successfully_repeatedly_open_signal()
        {
            var result = await Transport.Go("foo", CancellationToken.None);
            result.Success.Should().BeTrue();

            var tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
            await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, PortNumber);

            var result2 = await Transport.Go("bar", CancellationToken.None);
            result.Success.Should().BeTrue();

            var bytesRead = tcpClient.Available;
            bytesRead.Should().Be(8);
            tcpClient.Connected.Should().BeTrue();

            // special case - need to test the NULL pattern
            var result3 = await Transport.Go(null, CancellationToken.None);
            result.Success.Should().BeTrue();

            bytesRead = tcpClient.Available;
            bytesRead.Should().Be(8);
            tcpClient.Connected.Should().BeTrue();
        }
    }
}