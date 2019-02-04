// -----------------------------------------------------------------------
// <copyright file="ProbeTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck
{
    /// <summary>
    ///     Used to tell Akka.Healthcheck how to transmit this readiness data to an external
    ///     system, such as via a file, a TCP port, or a custom transport such as an HTTP endpoint.
    /// </summary>
    public enum ProbeTransport
    {
        /// <summary>
        ///     Writes the <see cref="ReadinessStatus" /> or <see cref="LivenessStatus" />
        ///     out to disk to a specified file location. Used in combination with liveness
        ///     checks such as "command line execution" checks.
        /// </summary>
        /// <remarks>
        ///     See
        ///     https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/#define-a-liveness-command
        ///     for example.
        /// </remarks>
        File,

        /// <summary>
        ///     Signals the <see cref="ReadinessStatus" /> or <see cref="LivenessStatus" />
        ///     by opening or closing a TCP socket.
        /// </summary>
        /// <remarks>
        ///     This transport mode can't write out any
        ///     of the <see cref="ReadinessStatus.StatusMessage" /> or <see cref="LivenessStatus.StatusMessage" /> data.
        ///     See
        ///     https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/#define-a-tcp-liveness-probe
        /// </remarks>
        TcpSocket,

        /// <summary>
        ///     Used to specify that no built-in transport will be used.
        ///     Typically users will query / subscribe to the Readiness or Liveness probe actors
        ///     and pipe the changes in liveness / readiness status out to something like
        ///     a custom HTTP endpoint.
        /// </summary>
        Custom
    }
}