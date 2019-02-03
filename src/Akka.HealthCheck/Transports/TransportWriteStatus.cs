using System;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    /// Used to signal the status of changing the underlying probe transport data.
    /// </summary>
    public sealed class TransportWriteStatus
    {
        public TransportWriteStatus(bool success, Exception exception = null)
        {
            Success = success;
            Exception = exception;
        }

        /// <summary>
        /// If <c>true</c>, the attempted operation was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// The <see cref="Exception"/> thrown if there was an error. Can be <c>null</c>.
        /// </summary>
        public Exception Exception { get; }
    }
}