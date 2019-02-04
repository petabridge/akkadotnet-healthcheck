using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Liveness;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    /// Subscribes to <see cref="LivenessStatus"/> changes and signals
    /// the underlying <see cref="IStatusTransport"/> accordingly.
    /// </summary>
    public sealed class LivenessTransportActor : ReceiveActor
    {
        const int LivenessTimeout = 1000;
        private readonly IStatusTransport _statusTransport;
        private readonly IActorRef _livenessProbe;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public LivenessTransportActor(IStatusTransport statusTransport, IActorRef livenessProbe)
        {
            _statusTransport = statusTransport;
            _livenessProbe = livenessProbe;

            ReceiveAsync<LivenessStatus>(async status =>
            {
                _log.Info("Received updated liveness status. Live: {0}, Message: {1}", status.IsLive, status.StatusMessage);
                var cts = new CancellationTokenSource(LivenessTimeout);
                TransportWriteStatus writeStatus = null;
                if (status.IsLive)
                {
                    writeStatus = await _statusTransport.Go(status.StatusMessage, cts.Token);
                }
                else
                {
                    writeStatus = await _statusTransport.Stop(status.StatusMessage, cts.Token);
                }

                if (!writeStatus.Success)
                {
                    _log.Error(writeStatus.Exception, "Failed to write to transport.");
                    throw new ProbeUpdateException(ProbeKind.Liveness,
                        $"Failed to update underlying transport {_statusTransport}", writeStatus.Exception);
                }
            });

            Receive<Terminated>(t =>
            {
                _log.Warning("Liveness probe actor terminated! Shutting down.");
                Context.Stop(Self);
            });
        }

        protected override void PreStart()
        {
            _livenessProbe.Tell(new SubscribeToLiveness(Self));
            Context.Watch(_livenessProbe);
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
                _log.Error(ex, "Error while attempting to stop liveness probe after [{0}] ms. Shutting down anyway.", LivenessTimeout);
            }
        }
    }
}
