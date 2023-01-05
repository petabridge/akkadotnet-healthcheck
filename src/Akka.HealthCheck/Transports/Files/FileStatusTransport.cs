// -----------------------------------------------------------------------
// <copyright file="FileStatusTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.HealthCheck.Transports.Files
{
    /// <inheritdoc />
    /// <summary>
    ///     Used to write status data out to the file system.
    /// </summary>
    public sealed class FileStatusTransport : IStatusTransport
    {
        public FileStatusTransport(FileTransportSettings settings)
        {
            Settings = settings;
        }

        public FileTransportSettings Settings { get; }

        public Task<TransportWriteStatus> Go(string? statusMessage, CancellationToken token)
        {
            try
            {
                var data = statusMessage ?? string.Empty;
                File.WriteAllText(Settings.FilePath, data);

                return Task.FromResult(new TransportWriteStatus(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TransportWriteStatus(false, ex));
            }
        }

        public Task<TransportWriteStatus> Stop(string? statusMessage, CancellationToken token)
        {
            try
            {
                if (File.Exists(Settings.FilePath)) // check first
                    File.Delete(Settings.FilePath);
                return Task.FromResult(new TransportWriteStatus(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TransportWriteStatus(false, ex));
            }
        }
    }
}