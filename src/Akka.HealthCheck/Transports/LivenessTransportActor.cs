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
        private readonly Dictionary<string, LivenessStatus> _statuses = new ();
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IStatusTransport _statusTransport;
        private readonly bool _logInfo;

        public LivenessTransportActor(IStatusTransport statusTransport, ImmutableDictionary<string, IActorRef> livenessProbes, bool log)
        {
            _statusTransport = statusTransport;
            var probeReverseLookup = livenessProbes.ToImmutableDictionary(kvp => kvp.Value, kvp => kvp.Key);
            foreach (var kvp in livenessProbes)
            {
                Context.Watch(kvp.Value);
                _statuses[kvp.Key] = new LivenessStatus(false, $"Probe {kvp.Key} starting up.");
            }
            _livenessProbes = livenessProbes.Values.ToList();
            _logInfo = log;

            ReceiveAsync<LivenessStatus>(async status =>
            {
                var probeName = probeReverseLookup[Sender];
                using var cts = new CancellationTokenSource(LivenessTimeout);
                TransportWriteStatus writeStatus;
                try
                {
                    if (_logInfo)
                        _log.Debug("Received liveness status from probe [{0}]. Live: {1}, Message: {2}", probeName, 
                            status.IsLive, status.StatusMessage);
                    
                    _statuses[probeName] = status;
                    var statusMessage = string.Join(
                        Environment.NewLine, 
                        _statuses.Select(kvp => $"[{kvp.Key}][{(kvp.Value.IsLive ? "Live" : "Not Live")}] {kvp.Value.StatusMessage}"));
                    
                    if (_statuses.Values.All(s => s.IsLive))
                        writeStatus = await _statusTransport.Go(statusMessage, cts.Token);
                    else
                        writeStatus = await _statusTransport.Stop(statusMessage, cts.Token);
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
                var probeName = probeReverseLookup[t.ActorRef];
                if (_logInfo)
                    _log.Info("Liveness probe {0} terminated", probeName);
                
                _livenessProbes.Remove(t.ActorRef);
                if (_livenessProbes.Count == 0)
                {
                    _log.Info("All liveness probe actors terminated! Shutting down.");
                    Context.Stop(Self);
                }
                else
                {
                    Self.Tell(new LivenessStatus(false, "Probe terminated"), t.ActorRef);
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
            using var cts = new CancellationTokenSource(LivenessTimeout);
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