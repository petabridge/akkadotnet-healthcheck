using System;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    /// Subscribes to <see cref="ReadinessStatus"/> changes and signals
    /// the underlying <see cref="IStatusTransport"/> accordingly.
    /// </summary>
    public sealed class ReadinessTransportActor : ReceiveActor
    {
        const int LivenessTimeout = 1000;
        private readonly IStatusTransport _statusTransport;
        private readonly IActorRef _readinessProbe;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        public ReadinessTransportActor(IStatusTransport statusTransport, IActorRef readinessProbe)
        {
            _statusTransport = statusTransport;
            _readinessProbe = readinessProbe;

            ReceiveAsync<ReadinessStatus>(async status =>
            {
                _log.Info("Received updated readiness status. Ready: {0}, Message: {1}", status.IsReady, status.StatusMessage);
                var cts = new CancellationTokenSource(LivenessTimeout);
                TransportWriteStatus writeStatus = null;
                if (status.IsReady)
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
                _log.Error(ex, "Error while attempting to stop readiness probe after [{0}] ms. Shutting down anyway.", LivenessTimeout);
            }
        }
    }
}