namespace Akka.HealthCheck.Liveness
{
    /// <summary>
    /// Used to signal changes in liveness status to the downstream consumers.
    /// </summary>
    public sealed class LivenessStatus
    {
        public LivenessStatus(bool isLive, string statusMessage = null)
        {
            IsLive = isLive;
            StatusMessage = statusMessage ?? string.Empty;
        }

        /// <summary>
        /// If <c>true</c>, the current node is live. If <c>false</c>, the current node's
        /// health is compromised and will likely need to be restarted.
        /// </summary>
        public bool IsLive { get; }

        /// <summary>
        /// An optional status message that will be written out to the
        /// target (if it supports text) as part of the liveness check.
        /// </summary>
        public string StatusMessage { get; }
    }
}
