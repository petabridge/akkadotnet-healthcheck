// -----------------------------------------------------------------------
// <copyright file="ReadinessTransportActor.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private const int ReadinessTimeout = 1000;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly List<IActorRef> _readinessProbes;
        private readonly Dictionary<string, ReadinessStatus> _statuses = new ();
        private readonly IStatusTransport _statusTransport;
        private readonly bool _logInfo;

        public ReadinessTransportActor(IStatusTransport statusTransport, ImmutableDictionary<string, IActorRef> readinessProbe, bool log)
        {
            _statusTransport = statusTransport;
            var probeReverseLookup = readinessProbe.ToImmutableDictionary(kvp => kvp.Value, kvp => kvp.Key);
            foreach (var kvp in readinessProbe)
            {
                Context.Watch(kvp.Value);
                _statuses[kvp.Key] = new ReadinessStatus(false, $"Probe {kvp.Key} starting up.");
            }
            _readinessProbes = readinessProbe.Values.ToList();
            _logInfo = log;

            ReceiveAsync<ReadinessStatus>(async status =>
            {
                var probeName = probeReverseLookup[Sender];
                using var cts = new CancellationTokenSource(ReadinessTimeout);
                TransportWriteStatus writeStatus;
                try
                {
                    if (_logInfo)
                        _log.Info("Received readiness status from probe [{0}]. Ready: {1}, Message: {2}", probeName, 
                            status.IsReady, status.StatusMessage);

                    _statuses[probeName] = status;
                    var statusMessage = string.Join(
                        Environment.NewLine, 
                        _statuses.Select(kvp => $"[{kvp.Key}][{(kvp.Value.IsReady ? "Ready" : "Not Ready")}] {kvp.Value.StatusMessage}"));
                    
                    if (_statuses.Values.All(s => s.IsReady))
                        writeStatus = await _statusTransport.Go(statusMessage, cts.Token);
                    else
                        writeStatus = await _statusTransport.Stop(statusMessage, cts.Token);
                }
                catch (Exception e)
                {
                    if (_logInfo)
                        _log.Error(e, $"While processing status from probe [{probeName}]. Failed to write to transport.");

                    throw new ProbeUpdateException(ProbeKind.Readiness,
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
                    
                    throw new ProbeUpdateException(ProbeKind.Readiness,
                        $"While processing status from probe [{probeName}]. Failed to update underlying transport {_statusTransport}", writeStatus.Exception);
                }
            });

            Receive<Terminated>(t =>
            {
                var probeName = probeReverseLookup[t.ActorRef];
                if (_logInfo)
                    _log.Info("Readiness probe {0} terminated", probeName);
                
                _readinessProbes.Remove(t.ActorRef);
                if (_readinessProbes.Count == 0)
                {
                    _log.Warning("All readiness probe actors terminated! Shutting down.");
                    Context.Stop(Self);
                }
                else
                {
                    Self.Tell(new ReadinessStatus(false, "Probe terminated"), t.ActorRef);
                }
            });
        }

        protected override void PreStart()
        {
            foreach (var probe in _readinessProbes)
            {
                probe.Tell(new SubscribeToReadiness(Self));
                Context.Watch(probe);
            }
        }

        protected override void PostStop()
        {
            using var cts = new CancellationTokenSource(ReadinessTimeout);
            try
            {
                _statusTransport.Stop(null, cts.Token).Wait(cts.Token);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while attempting to stop readiness probe after [{0}] ms. Shutting down anyway.",
                    ReadinessTimeout);
            }
        }
    }
}