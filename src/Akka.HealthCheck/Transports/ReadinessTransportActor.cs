// -----------------------------------------------------------------------
// <copyright file="ReadinessTransportActor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    ///     Subscribes to <see cref="ReadinessStatus" /> changes and signals
    ///     the underlying <see cref="IStatusTransport" /> accordingly.
    /// </summary>
    public sealed class ReadinessTransportActor : ReceiveActor
    {
        private const int LivenessTimeout = 1000;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _readinessProbe;
        private readonly IStatusTransport _statusTransport;
        private readonly bool _logInfo;

        public ReadinessTransportActor(IStatusTransport statusTransport, IActorRef readinessProbe, bool log)
        {
            _statusTransport = statusTransport;
            _readinessProbe = readinessProbe;
            _logInfo = log;

            ReceiveAsync<ReadinessStatus>(async status =>
            {
                if (_logInfo)
                    _log.Info("Received readiness status. Ready: {0}, Message: {1}", status.IsReady,
                    status.StatusMessage);

                var cts = new CancellationTokenSource(LivenessTimeout);
                TransportWriteStatus writeStatus = null;
                if (status.IsReady)
                    writeStatus = await _statusTransport.Go(status.StatusMessage, cts.Token);
                else
                    writeStatus = await _statusTransport.Stop(status.StatusMessage, cts.Token);

                if (!writeStatus.Success)
                {
                    _log.Error(writeStatus.Exception, "Failed to write to transport.");
                    throw new ProbeUpdateException(ProbeKind.Readiness,
                        $"Failed to update underlying transport {_statusTransport}", writeStatus.Exception);
                }
            });

            Receive<Terminated>(t =>
            {
                _log.Warning("Readiness probe actor terminated! Shutting down.");
                Context.Stop(Self);
            });
        }

        protected override void PreStart()
        {
            _readinessProbe.Tell(new SubscribeToReadiness(Self));
            Context.Watch(_readinessProbe);
        }

        protected override void PostStop()
        {
            var cts = new CancellationTokenSource(LivenessTimeout);

            try
            {
                _statusTransport.Stop(null, cts.Token).Wait(cts.Token);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while attempting to stop readiness probe after [{0}] ms. Shutting down anyway.",
                    LivenessTimeout);
            }
        }
    }
}