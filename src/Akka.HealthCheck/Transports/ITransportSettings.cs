namespace Akka.HealthCheck.Transports
{
    /// <summary>
    /// Marker interface for the transport settings.
    /// </summary>
    public interface ITransportSettings
    {
        ProbeTransport TransportType { get; }
    }
}