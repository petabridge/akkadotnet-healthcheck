// -----------------------------------------------------------------------
// <copyright file="SocketStatusTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Event;

namespace Akka.HealthCheck.Transports.Sockets
{
    /// <summary>
    ///     Used to write liveness / readiness status data by opening and maintaining a TCP socket.
    /// </summary>
    public sealed class SocketStatusTransport : IStatusTransport
    {
        private Socket _socket;
        private CancellationTokenSource _abortSocket;
        private static readonly byte[] Msg = Encoding.ASCII.GetBytes("akka.net");

        public SocketStatusTransport(SocketTransportSettings settings)
        {
            Settings = settings;
        }

        public SocketTransportSettings Settings { get; }

        public async Task<TransportWriteStatus> Go(string statusMessage, CancellationToken token)
        {
            try
            {
                if (_socket == null)
                {
                    _abortSocket = new CancellationTokenSource();
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    _socket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                    _socket.Listen(10);

                    // want this to run async, without waiting
                    _socket.AcceptAsync().ContinueWith(HandleAccept, _socket, _abortSocket.Token);
                }

                return new TransportWriteStatus(true);
            }
            catch (Exception ex)
            {
                return new TransportWriteStatus(false, ex);
            }
        }

        private void HandleAccept(Task<Socket> tr, object o)
        {
            var parentSocket = (Socket) o;
            var connectionSocket = tr.Result;
            System.Diagnostics.Debug.Assert(parentSocket != connectionSocket);
            connectionSocket.Send(Msg);
            connectionSocket.Shutdown(SocketShutdown.Both);
            connectionSocket.Close();
            parentSocket.AcceptAsync().ContinueWith(HandleAccept, parentSocket, _abortSocket.Token);
        }

        public async Task<TransportWriteStatus> Stop(string statusMessage, CancellationToken token)
        {
            try
            {
                if (_socket != null)
                {
                    _abortSocket.Cancel();
                    _abortSocket = null; // force recreate of token later
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