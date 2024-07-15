// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Liveness;
using Akka.Persistence;

#nullable enable
namespace Akka.HealthCheck.Persistence
{
    public sealed class PersistenceLivenessStatus: LivenessStatus, INoSerializationVerificationNeeded
    {
        public new static PersistenceLivenessStatus Healthy(string? message) 
            => new(
                status: AkkaHealthStatus.Healthy, 
                failure: null,
                message: message);

        public static PersistenceLivenessStatus Unhealthy(Exception? cause, string? message)
            => new(
                status: AkkaHealthStatus.Unhealthy,
                failure: cause,
                message: message);
        
        public static PersistenceLivenessStatus Degraded(Exception? cause, string? message)
            => new(
                status: AkkaHealthStatus.Degraded,
                failure: cause,
                message: message);
        
        private readonly string? _message;
        
        public PersistenceLivenessStatus(AkkaHealthStatus status, string message)
            : this(status, null, message)
        {
        }

        public PersistenceLivenessStatus(
            AkkaHealthStatus status,
            Exception? failure, 
            string? message = null): base(status)
        {
            Failure = failure;
            _message = message;
        }

        public override string StatusMessage => _message ?? ToString();

        public Exception? Failure { get; }

        public override string ToString()
        {
            return $"{nameof(PersistenceLivenessStatus)}(" +
                   $"{nameof(Status)}={Status}, " +
                   $"{nameof(Failure)}={Failure?.ToString() ?? "null"})";
        }
    }

    internal sealed class CreateProbe
    {
        public static readonly CreateProbe Instance = new();

        private CreateProbe()
        {
        }
    }

    internal sealed class CheckTimeout
    {
        public static readonly CheckTimeout Instance = new();

        private CheckTimeout()
        {
        }
    }

    internal sealed class WarmupComplete
    {
        public static readonly WarmupComplete Instance = new();
        private WarmupComplete(){ }
    }

    internal sealed class WarmupFailed
    {
        public WarmupFailed(Exception cause)
        {
            Cause = cause;
        }

        public Exception Cause { get; }
    }

    public class AkkaPersistenceLivenessProbe : ActorBase, IWithTimers
    {
        public const string PersistenceId = "Akka.HealthCheck.Probe";
        
        private readonly ILoggingAdapter _log;
        private readonly HashSet<IActorRef> _subscribers = new ();
        private PersistenceLivenessStatus _currentLivenessStatus = new(AkkaHealthStatus.Degraded, "Warming up probe. Recovery status is still undefined");
        private IActorRef? _probe;
        private readonly TimeSpan _delay;
        private readonly TimeSpan _timeout;
        private readonly bool _logInfo;

        private readonly int _maxRetry;
        private int _retryCount;
        
        public AkkaPersistenceLivenessProbe(bool logInfo, TimeSpan delay, TimeSpan timeout, int maxRetry)
        {
            _delay = delay;
            _timeout = timeout;
            _maxRetry = maxRetry;
            _logInfo = logInfo;
            _log = Context.GetLogger();
            
            Become(WarmingUp);
        }

        public ITimerScheduler Timers { get; set; } = null!;

        public static Props PersistentHealthCheckProps(bool logInfo, TimeSpan delay, TimeSpan timeout, int maxRetry)
        {
            return Props.Create(() => new AkkaPersistenceLivenessProbe(logInfo, delay, timeout, maxRetry))
                .WithDeploy(Deploy.Local);
        }

        protected override void PostStop()
        {
            if(_probe is not null)
            {
                Context.Stop(_probe);
                _probe = null;
            }
            base.PostStop();
        }
        
        private bool HandleSubscriptions(object msg)
        {
            switch (msg)
            {
                case GetCurrentLiveness _:
                    Sender.Tell(_currentLivenessStatus);
                    break;
                case SubscribeToLiveness sub:
                    _subscribers.Add(sub.Subscriber);
                    Context.Watch(sub.Subscriber);
                    sub.Subscriber.Tell(_currentLivenessStatus);
                    break;
                case UnsubscribeFromLiveness unsub:
                    _subscribers.Remove(unsub.Subscriber);
                    Context.Unwatch(unsub.Subscriber);
                    break;
                case Terminated term:
                    _subscribers.Remove(term.ActorRef);
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool WarmingUp(object message)
        {
            switch (message)
            {
                case Terminated terminated:
                    if (!terminated.ActorRef.Equals(_probe))
                        return false;
                    
                    Context.Unwatch(_probe);
                    _probe = null;

                    if (_currentLivenessStatus.Status is AkkaHealthStatus.Healthy)
                    {
                        if(_logInfo)
                            _log.Debug("Persistence warmup probe terminated. Switching to active state");
                        
                        Become(Active);
                        Self.Tell(CreateProbe.Instance);
                        return true;
                    }
                    
                    if(_logInfo)
                        _log.Debug($"Persistence warmup probe terminated. Recreating in {_delay.TotalSeconds} seconds.");
                        
                    ScheduleProbeRestart();
                    return true;
                
                case CreateProbe:
                    if(_logInfo)
                        _log.Debug("Recreating persistence warmup probe.");
                    
                    Timers.StartSingleTimer(CheckTimeout.Instance, CheckTimeout.Instance, _timeout);
                    _probe = Context.System.ActorOf(SuicideWarmupProbe.Props(Self, PersistenceId, _logInfo), PersistenceId);
                    Context.Watch(_probe);
                    return true;

                case CheckTimeout:
                    const string errMsg = "Timeout while checking persistence liveness. Persistence liveness status is undefined.";
                    _log.Warning(errMsg);
                    HandleFailure(null, errMsg);
                    
                    if(_probe is not null)
                        Context.Stop(_probe);
                    return true;
                
                case WarmupComplete:
                    if(_logInfo)
                        _log.Debug("Persistence warmup complete");
                    
                    HandleSuccess("Persistence warmup complete");
                    return true;
                
                case WarmupFailed fail:
                    if(_logInfo)
                        _log.Warning(fail.Cause, "Persistence warmup failed");
                    
                    HandleFailure(fail.Cause, "Persistence warmup failed");
                    return true;
                    
                default:
                    return HandleSubscriptions(message);
            }
        }
        
        private bool Active(object message)
        {
            switch (message)
            {
                case Terminated terminated:
                    if (!terminated.ActorRef.Equals(_probe))
                        return false;
                    
                    if(_logInfo)
                        _log.Debug($"Persistence probe terminated. Recreating in {_delay.TotalSeconds} seconds.");
                    
                    Context.Unwatch(_probe);
                    _probe = null;
                    
                    ScheduleProbeRestart();
                    return true;
                
                case CreateProbe:
                    if(_logInfo)
                        _log.Debug("Recreating persistence probe.");
                    
                    Timers.StartSingleTimer(CheckTimeout.Instance, CheckTimeout.Instance, _timeout);
                    _probe = Context.System.ActorOf(SuicideProbe.Props(Self, PersistenceId, _logInfo), PersistenceId);
                    Context.Watch(_probe);
                    return true;

                case CheckTimeout:
                    const string errMsg = "Timeout while checking persistence liveness. Persistence liveness status is undefined.";
                    _log.Warning(errMsg);
                    HandleFailure(null, errMsg);
                    
                    if(_probe is not null)
                        Context.Stop(_probe);
                    return true;
                
                case PersistenceLivenessStatus status:
                    Timers.CancelAll();
                    if(_logInfo)
                        _log.Debug("Received recovery status {0} from probe", status);
            
                    HandleStatus(status);
                    return true;
                
                default:
                    return HandleSubscriptions(message);
            }
        }
        
        protected override bool Receive(object message)
        {
            throw new NotImplementedException("Should never reach this line");
        }

        protected override void PreStart()
        {
            Self.Tell(CreateProbe.Instance);
        }

        private void HandleFailure(Exception? e, string? message)
        {
            _retryCount++;
            _currentLivenessStatus = _retryCount > _maxRetry
                ? PersistenceLivenessStatus.Unhealthy(e, message)
                : PersistenceLivenessStatus.Degraded(e, message);
            PublishStatusUpdates();
        }

        private void HandleSuccess(string? message)
        {
            _retryCount = 0;
            _currentLivenessStatus = PersistenceLivenessStatus.Healthy(message);
            PublishStatusUpdates();
        }

        private void HandleStatus(PersistenceLivenessStatus status)
        {
            if (status.Status is AkkaHealthStatus.Unhealthy or AkkaHealthStatus.Degraded)
                HandleFailure(status.Failure, status.StatusMessage);
            else
                HandleSuccess(status.StatusMessage);
        }
        
        private void ScheduleProbeRestart()
        {
            Timers.StartSingleTimer(CreateProbe.Instance, CreateProbe.Instance, _delay);
        }
        
        private void PublishStatusUpdates()
        {
            foreach (var sub in _subscribers) sub.Tell(_currentLivenessStatus);
        }
    }

    /// <summary>
    ///     Validate that data exist inside the snapshot store and journal
    /// </summary>
    internal class SuicideWarmupProbe : ReceivePersistentActor
    {
        public static Props Props(IActorRef probe, string persistenceId, bool debugLog)
            => Actor.Props.Create(() => new SuicideWarmupProbe(probe, persistenceId, debugLog))
                .WithDeploy(Deploy.Local)
                .WithSupervisorStrategy(Actor.SupervisorStrategy.StoppingStrategy);
        
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _probe;
        private readonly bool _debugLog;

        private bool _journalRecovered;
        private bool _snapshotRecovered;
        
        public SuicideWarmupProbe(IActorRef probe, string persistenceId, bool debugLog)
        {
            _probe = probe;
            PersistenceId = persistenceId;
            _debugLog = debugLog;
            
            Become(Recovering);
        }

        private void Recovering()
        {
            Recover<string>(_ =>
            {
                _journalRecovered = true;
            });
            
            Recover<SnapshotOffer>(_ =>
            {
                _snapshotRecovered = true;
            });
            
            Recover<RecoveryCompleted>(_ =>
            {
                if (_journalRecovered && _snapshotRecovered)
                {
                    CompleteRecovery();
                    return;
                }
                Become(Persisting());
            });
        }

        private Action Persisting()
        {
            if(!_journalRecovered)
                Persist(PersistenceId, _ =>
                {
                    _journalRecovered = true;
                    if (_snapshotRecovered)
                        CompleteRecovery();
                });
            
            if(!_snapshotRecovered)
                SaveSnapshot(PersistenceId);

            return () =>
            {
                Command<SaveSnapshotSuccess>(_ =>
                {
                    _snapshotRecovered = true;
                    if (_journalRecovered)
                        CompleteRecovery();
                });
                
                Command<SaveSnapshotFailure>(fail =>
                {
                    if(_debugLog)
                        _log.Warning(fail.Cause, "Snapshot failed");
                    
                    _probe.Tell(new WarmupFailed(fail.Cause));
                    Context.Stop(Self);
                });
            };
        }

        private void CompleteRecovery()
        {
            if(_debugLog)
                _log.Debug("Recovery complete");
                    
            _probe.Tell(WarmupComplete.Instance);
            Context.Stop(Self);
        }

        public override string PersistenceId { get; }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            _log.Error(cause, "Persist failed");
            
            _probe.Tell(new WarmupFailed(cause));
            Context.Stop(Self);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            _log.Error(cause, "Persist rejected");
            
            _probe.Tell(new WarmupFailed(cause));
            Context.Stop(Self);
        }

        protected override void OnRecoveryFailure(Exception cause, object? message = null)
        {
            _log.Error(cause, $"Recovery failure{(message is null ? "" : $": {message}")}");
            
            _probe.Tell(new WarmupFailed(cause));
            Context.Stop(Self);
        }
    }
    
    /// <summary>
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    internal class SuicideProbe : ReceivePersistentActor
    {
        public static Props Props(IActorRef probe, string persistenceId, bool debugLog)
            => Actor.Props.Create(() => new SuicideProbe(probe, persistenceId, debugLog))
                .WithDeploy(Deploy.Local)
                .WithSupervisorStrategy(Actor.SupervisorStrategy.StoppingStrategy);
        
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _probe;
        
        public SuicideProbe(IActorRef probe, string persistenceId, bool debugLog)
        {
            _probe = probe;
            PersistenceId = persistenceId;
            
            Recover<string>(_ =>
            {
                // no-op
            });
            
            Recover<SnapshotOffer>(_ =>
            {
                // no-op
            });
            
            Recover<RecoveryCompleted>(_ =>
            {
                _probe.Tell(PersistenceLivenessStatus.Healthy(null));
                if(debugLog)
                    _log.Debug("Recovery complete");
                Context.Stop(Self);
            });
        }

        public override string PersistenceId { get; }

        protected override void OnRecoveryFailure(Exception reason, object? message = null)
        {
            var msg = $"Recovery failure{(message is null ? "" : $": {message}")}";
            _log.Error(reason, msg);
            
            _probe.Tell(PersistenceLivenessStatus.Unhealthy(reason, msg));
            Context.Stop(Self);
        }
    }
}