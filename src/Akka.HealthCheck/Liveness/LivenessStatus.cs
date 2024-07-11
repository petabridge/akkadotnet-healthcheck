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
        public LivenessStatus(AkkaHealthStatus status, string? statusMessage = null)
        {
            Status = status;
            StatusMessage = statusMessage ?? string.Empty;
        }

        public virtual bool IsLive => Status is AkkaHealthStatus.Healthy or AkkaHealthStatus.Degraded;
        
        /// <summary>
        ///     Contains the health status of the current node, either Healthy, Degraded, or Unhealthy.
        ///     If <c>Healthy</c>, the current node is live. If <c>Unhealthy</c>, the current node's
        ///     health is compromised and will likely need to be restarted.
        /// </summary>
        public virtual AkkaHealthStatus Status { get; }

        /// <summary>
        ///     An optional status message that will be written out to the
        ///     target (if it supports text) as part of the liveness check.
        /// </summary>
        public virtual string StatusMessage { get; }

        public static LivenessStatus Healthy(string? statusMessage = null)
            => new(AkkaHealthStatus.Healthy, statusMessage);

        public static LivenessStatus Degraded(string? statusMessage = null)
            => new(AkkaHealthStatus.Degraded, statusMessage);

        public static LivenessStatus Unhealthy(string? statusMessage = null)
            => new(AkkaHealthStatus.Unhealthy, statusMessage);
    }
}