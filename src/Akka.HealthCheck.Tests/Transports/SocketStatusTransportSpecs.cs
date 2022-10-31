// -----------------------------------------------------------------------
// <copyright file="SocketStatusTransportSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Sockets;
using Akka.Util;
using FluentAssertions;
using FluentAssertions.Extensions;
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
            if (!result.Success)
                ExceptionDispatchInfo.Capture(result.Exception).Throw();
            
            result.Success.Should().BeTrue();

            var tcpClient = new TcpClient(AddressFamily.InterNetwork);
            await tcpClient.ConnectAsync(IPAddress.Loopback, PortNumber);

            await AwaitAssertAsync(async () => (await tcpClient.GetStream().ReadAsync(new byte[10], 0, 10)).Should().Be(8));

            var deleteResult = await Transport.Stop(null, CancellationToken.None);
            deleteResult.Success.Should().BeTrue();

            var tcpClient2 = new TcpClient(AddressFamily.InterNetwork);
            //Should throw exception as socket will refuse to establish a connection
            await tcpClient2.Awaiting(client => client.ConnectAsync(IPAddress.Loopback, PortNumber))
                .Should().ThrowAsync<SocketException>();
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

            var tcpClient = new TcpClient(AddressFamily.InterNetwork);
            await tcpClient.ConnectAsync(IPAddress.Loopback, PortNumber);

            var result2 = await Transport.Go("bar", CancellationToken.None);
            result2.Success.Should().BeTrue();
            
            await AwaitAssertAsync(()=> tcpClient.Available.Should().Be(8));
            tcpClient.Connected.Should().BeTrue();

            // special case - need to test the NULL pattern
            var result3 = await Transport.Go(null, CancellationToken.None);
            result3.Success.Should().BeTrue();

            
            await AwaitAssertAsync(() => tcpClient.Available.Should().Be(8)); 
            tcpClient.Connected.Should().BeTrue();
        }
    }
}