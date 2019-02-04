// -----------------------------------------------------------------------
// <copyright file="CustomTransportSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck.Transports
{
    /// <inheritdoc />
    /// <summary>
    ///     If this class is used, it means that <see cref="F:Akka.HealthCheck.ProbeTransport.Custom" />
    ///     was chosen and <see cref="T:Akka.HealthCheck.AkkaHealthCheck" /> won't automatically try to do
    ///     anything to transmit liveness / readiness data to external listeners.
    /// </summary>
    public sealed class CustomTransportSettings : ITransportSettings
    {
        public ProbeTransport TransportType => ProbeTransport.Custom;

        public string StartupMessage =>
            "No built-in probe transport configured. Relying on user-definted tools to suppply status data.";
    }
}