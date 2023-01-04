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
        private Socket? _socket;
        private CancellationTokenSource? _abortSocket;
        private static readonly byte[] Msg = Encoding.ASCII.GetBytes("akka.net");

        public SocketStatusTransport(SocketTransportSettings settings)
        {
            Settings = settings;
        }

        public SocketTransportSettings Settings { get; }

        public Task<TransportWriteStatus> Go(string? statusMessage, CancellationToken token)
        {
            try
            {
                if (_socket == null)
                {
                    _abortSocket = new CancellationTokenSource();
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    _socket.Bind(new IPEndPoint(IPAddress.Any, Settings.Port));
                    _socket.Listen(10);

#pragma warning disable CS4014
                    // want this to run async, without waiting
                    _socket.AcceptAsync().ContinueWith(HandleAccept, _socket, _abortSocket.Token);
#pragma warning restore CS4014
                }

                return Task.FromResult(new TransportWriteStatus(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TransportWriteStatus(false, ex));
            }
        }

        private void HandleAccept(Task<Socket> tr, object? o)
        {
            if (o is null)
                throw new Exception("Continuation error, state object is null");

            if (o is not Socket parentSocket)
                throw new Exception($"Continuation error, state object is not of type Socket, was: {o.GetType()}");
            
            // Not blocking, handler delegate is called when the task completed. 
            using var connectionSocket = tr.Result;
            try
            {
                System.Diagnostics.Debug.Assert(parentSocket != connectionSocket);
                connectionSocket.Send(Msg);
                connectionSocket.Shutdown(SocketShutdown.Both);
                connectionSocket.Close();
            }
            finally
            {
                connectionSocket.Dispose();
            }
            
            if(_abortSocket is { })
            {
                _abortSocket.Token.ThrowIfCancellationRequested();
                parentSocket.AcceptAsync().ContinueWith(HandleAccept, parentSocket, _abortSocket.Token);
            }
        }

        public Task<TransportWriteStatus> Stop(string? statusMessage, CancellationToken token)
        {
            try
            {
                if (_abortSocket is { })
                {
                    _abortSocket.Cancel();
                    _abortSocket.Dispose();
                    _abortSocket = null; // force recreate of token later
                }
                
                if (_socket != null)
                {
                    _socket.Close();
                    _socket.Dispose();
                    _socket = null;
                }

                return Task.FromResult(new TransportWriteStatus(true));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TransportWriteStatus(false, ex));
            }
        }
    }
}