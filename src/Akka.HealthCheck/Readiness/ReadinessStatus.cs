// -----------------------------------------------------------------------
// <copyright file="ReadinessStatus.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck.Readiness
{
    /// <summary>
    ///     Used to signal changes in readiness status to the downstream consumers.
    /// </summary>
    public sealed class ReadinessStatus
    {
        public ReadinessStatus(bool isReady, string statusMessage = null)
        {
            IsReady = isReady;
            StatusMessage = statusMessage ?? string.Empty;
        }

        /// <summary>
        ///     If <c>true</c>, the current node is ready to begin accepting requests.
        /// </summary>
        public bool IsReady { get; }

        /// <summary>
        ///     An optional status message that will be written out to the
        ///     target (if it supports text) as part of the readiness check.
        /// </summary>
        public string StatusMessage { get; }
    }
}