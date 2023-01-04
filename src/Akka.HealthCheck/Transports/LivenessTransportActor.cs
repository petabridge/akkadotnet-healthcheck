// -----------------------------------------------------------------------
// <copyright file="LivenessTransportActor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Liveness;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Akka.HealthCheck.Transports
{
    /// <summary>
    ///     Subscribes to <see cref="LivenessStatus" /> changes and signals
    ///     the underlying <see cref="IStatusTransport" /> accordingly.
    /// </summary>
    public sealed class LivenessTransportActor : ReceiveActor
    {
        private const int LivenessTimeout = 1000;
        private readonly List<IActorRef> _livenessProbes;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IStatusTransport _statusTransport;
        private readonly bool _logInfo;

        public LivenessTransportActor(IStatusTransport statusTransport, ImmutableDictionary<string, IActorRef> livenessProbes, bool log)
        {
            _statusTransport = statusTransport;
            var probeReverseLookup = livenessProbes.ToImmutableDictionary(kvp => kvp.Value, kvp => kvp.Key);
            _livenessProbes = livenessProbes.Values.ToList();
            _logInfo = log;

            ReceiveAsync<LivenessStatus>(async status =>
            {
                var probeName = probeReverseLookup[Sender];
                
                if (_logInfo)
                 _log.Info("Received liveness status from probe [{0}]. Live: {1}, Message: {2}", probeName, 
                     status.IsLive, status.StatusMessage);
               
                var cts = new CancellationTokenSource(LivenessTimeout);
                TransportWriteStatus writeStatus;
                try
                {
                    if (status.IsLive)
                        writeStatus = await _statusTransport.Go($"[{probeName}] {status.StatusMessage}", cts.Token);
                    else
                        writeStatus = await _statusTransport.Stop($"[{probeName}] {status.StatusMessage}", cts.Token);
                }
                catch (Exception e)
                {
                    if (_logInfo)
                        _log.Error(e, $"While processing status from probe [{probeName}]. Failed to write to transport.");

                    throw new ProbeUpdateException(ProbeKind.Liveness,
                        $"While processing status from probe [{probeName}]. Failed to update underlying transport {_statusTransport}", e);
                }
                finally
                {
                    cts.Dispose();
                }

                if (!writeStatus.Success)
                {
                    if (_logInfo)
                        _log.Error(writeStatus.Exception, $"While processing status from probe [{probeName}]. Failed to write to transport.");

                    throw new ProbeUpdateException(ProbeKind.Liveness,
                        $"While processing status from probe [{probeName}]. Failed to update underlying transport {_statusTransport}", writeStatus.Exception);
                }
            });

            Receive<Terminated>(t =>
            {
                _livenessProbes.Remove(t.ActorRef);
                if (_livenessProbes.Count == 0)
                {
                    _log.Warning("All liveness probe actors terminated! Shutting down.");
                    Context.Stop(Self);
                }
            });
        }

        protected override void PreStart()
        {
            foreach (var probe in _livenessProbes)
            {
                probe.Tell(new SubscribeToLiveness(Self));
                Context.Watch(probe);
            }
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
                _log.Error(ex, "Error while attempting to stop liveness probe after [{0}] ms. Shutting down anyway.",
                    LivenessTimeout);
            }
        }
    }
}