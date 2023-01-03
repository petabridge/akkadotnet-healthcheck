// -----------------------------------------------------------------------
// <copyright file="LivenessStatus.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck.Liveness
{
    /// <summary>
    ///     Used to signal changes in liveness status to the downstream consumers.
    /// </summary>
    public class LivenessStatus
    {
        public LivenessStatus(bool isLive, string? statusMessage = null)
        {
            IsLive = isLive;
            StatusMessage = statusMessage ?? string.Empty;
        }

        /// <summary>
        ///     If <c>true</c>, the current node is live. If <c>false</c>, the current node's
        ///     health is compromised and will likely need to be restarted.
        /// </summary>
        public virtual bool IsLive { get; }

        /// <summary>
        ///     An optional status message that will be written out to the
        ///     target (if it supports text) as part of the liveness check.
        /// </summary>
        public virtual string StatusMessage { get; }
    }
}