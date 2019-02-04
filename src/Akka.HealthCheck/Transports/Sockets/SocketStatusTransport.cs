using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Akka.HealthCheck.Transports.Sockets
{
    /// <summary>
    /// Used to write liveness / readiness status data by opening and maintaining a TCP socket.
    /// </summary>
    public sealed class SocketStatusTransport : IStatusTransport
    {
        public SocketStatusTransport(SocketTransportSettings settings)
        {
            Settings = settings;
        }

        public SocketTransportSettings Settings { get; }

        private Socket _socket;

        public async Task<TransportWriteStatus> Go(string statusMessage, CancellationToken token)
        {
            try
            {
                if (_socket == null)
                {
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    _socket.Bind(new IPEndPoint(IPAddress.IPv6Any, Settings.Port));
                    _socket.Listen(10);
                }

                return new TransportWriteStatus(true);
            }
            catch (Exception ex)
            {
                return new TransportWriteStatus(false, ex);
            }
        }

        public async Task<TransportWriteStatus> Stop(string statusMessage, CancellationToken token)
        {
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }

                return new TransportWriteStatus(true);
            }
            catch (Exception ex)
            {
                return new TransportWriteStatus(false, ex);
            }
        }
    }
}