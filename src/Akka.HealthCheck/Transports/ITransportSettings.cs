namespace Akka.HealthCheck.Transports
{
    /// <summary>
    /// Marker interface for the transport settings.
    /// </summary>
    public interface ITransportSettings
    {
        ProbeTransport TransportType { get; }

        /// <summary>
        /// Provides a human-readable diagnostic message that will be logged
        /// by <see cref="AkkaHealthCheck"/> at startup - used to make sure
        /// that the output of the probes is being written to the location expected
        /// by Kubernetes, the load balancer, or whatever.
        /// </summary>
        string StartupMessage { get; }
    }
}