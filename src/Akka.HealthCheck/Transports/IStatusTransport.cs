// -----------------------------------------------------------------------
// <copyright file="IStatusTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Akka.HealthCheck.Liveness;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    ///     Used by the built-in <see cref="ProbeTransport" /> actors
    ///     to write out <see cref="LivenessStatus" /> data to sockets
    ///     or files.
    /// </summary>
    public interface IStatusTransport
    {
        /// <summary>
        ///     Signal that we are live / ready.
        /// </summary>
        /// <param name="statusMessage">Optional. A message to include with the signal.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>
        ///     A task with a status of <c>true</c> or <c>false</c>. If
        ///     <c>false</c>, we failed to write the updated status to the transport.
        /// </returns>
        Task<TransportWriteStatus> Go(string? statusMessage, CancellationToken token);

        /// <summary>
        ///     Signal that we are NOT live / ready.
        /// </summary>
        /// <param name="statusMessage">Optional. A message to include with the signal.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>
        ///     A task with a status of <c>true</c> or <c>false</c>. If
        ///     <c>false</c>, we failed to close the transport.
        /// </returns>
        Task<TransportWriteStatus> Stop(string? statusMessage, CancellationToken token);
    }
}