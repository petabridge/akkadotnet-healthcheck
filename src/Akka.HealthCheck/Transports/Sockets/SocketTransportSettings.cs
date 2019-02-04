// -----------------------------------------------------------------------
// <copyright file="SocketTransportSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck.Transports.Sockets
{
    /// <summary>
    ///     Settings class for the <see cref="SocketStatusTransport" />
    /// </summary>
    public sealed class SocketTransportSettings : ITransportSettings
    {
        public SocketTransportSettings(int port)
        {
            Port = port;
        }

        /// <summary>
        ///     The port used to open the TCP socket.
        /// </summary>
        public int Port { get; }

        public ProbeTransport TransportType => ProbeTransport.TcpSocket;
        public string StartupMessage => $"Writing probe data to TCP Socket at [0.0.0.0]{Port}";
    }
}