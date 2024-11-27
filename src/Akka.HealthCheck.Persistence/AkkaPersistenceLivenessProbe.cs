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
using Phobos.Actor.Common;

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

    public class AkkaPersistenceLivenessProbe : ActorBase, IWithTimers
    {
        public static readonly string PersistenceId = $"Akka.HealthCheck-{Guid.NewGuid()}";
        
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
            
            Become(Active);
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
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    internal class SuicideProbe : ReceivePersistentActor, INeverInstrumented
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