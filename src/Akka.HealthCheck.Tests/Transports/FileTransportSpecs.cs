using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Files;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Tests.Transports
{
    public class FileTransportSpecs : IDisposable
    {
        public string FileName { get; }

        public IStatusTransport Transport { get; }

        public FileTransportSpecs()
        {
            FileName = Path.GetRandomFileName();
            Transport = new FileStatusTransport(new FileTransportSettings(FileName));
        }

        [Fact(DisplayName = "FileTransport should successfully open and write file signal")]
        public async Task Should_successfully_open_and_close_signal()
        {
            var result = await Transport.Go("foo", CancellationToken.None);
            result.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeTrue();
            File.ReadAllText(FileName).Should().Be("foo");

            var deleteResult = await Transport.Stop(null, CancellationToken.None);
            deleteResult.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeFalse();
        }

        [Fact(DisplayName = "FileTransport should idempotently close file signal")]
        public async Task Should_successfully_repeatedly_close_signal()
        {
            var deleteResult = await Transport.Stop(null, CancellationToken.None);
            deleteResult.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeFalse();

            var deleteResult2 = await Transport.Stop(null, CancellationToken.None);
            deleteResult2.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeFalse();
        }

        [Fact(DisplayName = "FileTransport should idempotently open file signal")]
        public async Task Should_successfully_repeatedly_open_signal()
        {
            var result = await Transport.Go("foo", CancellationToken.None);
            result.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeTrue();
            File.ReadAllText(FileName).Should().Be("foo");

            var result2 = await Transport.Go("bar", CancellationToken.None);
            result.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeTrue();
            File.ReadAllText(FileName).Should().Be("bar");

            // special case - need to test the NULL pattern
            var result3 = await Transport.Go(null, CancellationToken.None);
            result.Success.Should().BeTrue();
            File.Exists(FileName).Should().BeTrue();
            File.ReadAllText(FileName).Should().Be(string.Empty);
        }

        public void Dispose()
        {
            // attempt to clean up any leaked files afterwards
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
        }
    }
}
