// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Liveness;
using Akka.Persistence;

#nullable enable
namespace Akka.HealthCheck.Persistence
{
    public sealed class PersistenceLivenessStatus: LivenessStatus, INoSerializationVerificationNeeded
    {
        private readonly string? _message;
        
        public PersistenceLivenessStatus(string message): this(false, false, false, false, false, Array.Empty<Exception>(), message)
        {
        }

        public PersistenceLivenessStatus(
            bool warmup,
            bool journalRecovered,
            bool snapshotRecovered,
            bool journalPersisted,
            bool snapshotSaved, 
            IReadOnlyCollection<Exception> failures, 
            string? message = null): base(false)
        {
            JournalRecovered = journalRecovered;
            SnapshotRecovered = snapshotRecovered;
            JournalPersisted = journalPersisted;
            SnapshotSaved = snapshotSaved;
            Warmup = warmup;
            Failures = failures.Count > 0 ? new AggregateException(failures) : null;
            _message = message;
        }

        public override bool IsLive => JournalRecovered
                                       && SnapshotRecovered
                                       && JournalPersisted
                                       && SnapshotSaved
                                       && Failures is null;

        public override string StatusMessage => _message ?? ToString();

        public bool Warmup { get; }
        
        public bool JournalRecovered { get; }

        public bool SnapshotRecovered { get; }
            
        public bool JournalPersisted { get; }
            
        public bool SnapshotSaved { get; }

        public AggregateException? Failures { get; }

        public override string ToString()
        {
            return $"{nameof(PersistenceLivenessStatus)}(" +
                   $"{nameof(JournalRecovered)}={JournalRecovered}, " +
                   $"{nameof(SnapshotRecovered)}={SnapshotRecovered}, " +
                   $"{nameof(JournalPersisted)}={JournalPersisted}, " +
                   $"{nameof(SnapshotSaved)}={SnapshotSaved}, " +
                   $"{nameof(Failures)}={Failures?.ToString() ?? "null"})";
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
        private const string TimeoutTimerKey = nameof(TimeoutTimerKey);
        private const string CreateProbeTimerKey = nameof(CreateProbeTimerKey);
        
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private PersistenceLivenessStatus _currentLivenessStatus = new(message: "Warming up probe. Recovery status is still undefined");
        private IActorRef? _probe;
        private int _probeCounter;
        private int _firstIndex;
        private readonly TimeSpan _delay;
        private readonly TimeSpan _timeout;
        private readonly string _id;
        private readonly bool _logInfo;

        public AkkaPersistenceLivenessProbe(bool logInfo, TimeSpan delay, TimeSpan timeout)
        {
            _delay = delay;
            _timeout = timeout;
            _id = Guid.NewGuid().ToString("N");
            _logInfo = logInfo;
            
            Become(obj => HandleMessages(obj) || HandleSubscriptions(obj));
        }

        public ITimerScheduler Timers { get; set; } = null!;

        public static Props PersistentHealthCheckProps(bool logInfo, TimeSpan delay, TimeSpan timeout)
        {
            // need to use the stopping strategy in case things blow up right away
            return Props.Create(() => new AkkaPersistenceLivenessProbe(logInfo, delay, timeout))
                .WithSupervisorStrategy(Actor.SupervisorStrategy.StoppingStrategy);
        }

        protected override void PostStop()
        {
            _probe?.Tell(PoisonPill.Instance);
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

        private void HandleRecoveryStatus(PersistenceLivenessStatus livenessStatus)
        {
            if(_logInfo)
                _log.Debug("Received recovery status {0} from probe. First attempt? {1}", livenessStatus, livenessStatus.Warmup);
            
            _currentLivenessStatus = livenessStatus;
            if (livenessStatus.Warmup && (!livenessStatus.SnapshotSaved || !livenessStatus.JournalPersisted))
                _firstIndex++;
            
            PublishStatusUpdates();
        }

        private bool HandleMessages(object message)
        {
            switch (message)
            {
                case Terminated t when t.ActorRef.Equals(_probe):
                    Context.Unwatch(_probe);
                    _probe = null;
                    if(_logInfo)
                        _log.Debug($"Persistence probe terminated. Recreating in {_delay.TotalSeconds} seconds.");
                    
                    Timers.CancelAll();
                    ScheduleProbeRestart();
                    return true;
                
                case CreateProbe:
                    if(_logInfo)
                        _log.Debug("Recreating persistence probe.");
                    
                    Timers.StartSingleTimer(TimeoutTimerKey, CheckTimeout.Instance, _timeout);
                    _probe = Context.ActorOf(Props.Create(() => new SuicideProbe(Self, _probeCounter <= _firstIndex, _id, _logInfo)));
                    Context.Watch(_probe);
                    _probe.Tell("hit" + _probeCounter);
                    _probeCounter++;
                    return true;
                
                case CheckTimeout:
                    const string errMsg = "Timeout while checking persistence liveness. Recovery status is undefined.";
                    _log.Warning(errMsg);
                    _currentLivenessStatus = new PersistenceLivenessStatus(errMsg);
                    PublishStatusUpdates();
                    
                    if(_probe is not null)
                        Context.Stop(_probe);
                    
                    return true;
                
                case PersistenceLivenessStatus status:
                    Timers.CancelAll();
                    HandleRecoveryStatus(status);
                    return true;
            }

            return false;
        }

        protected override bool Receive(object message)
        {
            throw new NotImplementedException("Should never hit this line");
        }

        protected override void PreStart()
        {
            Self.Tell(CreateProbe.Instance);
        }

        private void ScheduleProbeRestart()
        {
            Timers.StartSingleTimer(CreateProbeTimerKey, CreateProbe.Instance, _delay);
        }
        
        private void PublishStatusUpdates()
        {
            foreach (var sub in _subscribers) sub.Tell(_currentLivenessStatus);
        }
    }

    /// <summary>
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    internal class SuicideProbe : ReceivePersistentActor, IWithStash
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _probe;
        private readonly bool _firstAttempt;
        private readonly bool _debugLog;

        private string? _message;
        private bool? _recoveredJournal;
        private bool? _recoveredSnapshotStore;
        private bool? _persistedJournal;
        private bool? _persistedSnapshotStore;
        private bool? _deletedJournal;
        private bool? _deletedSnapshotStore;
        private readonly List<Exception> _failures = new ();
        
        public SuicideProbe(IActorRef probe, bool firstAttempt, string id, bool debugLog)
        {
            _probe = probe;
            _firstAttempt = firstAttempt;
            _debugLog = debugLog;
            PersistenceId = $"Akka.HealthCheck-{id}";
            
            Become(AwaitingRecovery);
        }

        private void AwaitingRecovery()
        {
            Recover<string>(_ =>
            {
                _recoveredJournal = true;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Journal recovered");
            });
            Recover<SnapshotOffer>(_ =>
            {
                _recoveredSnapshotStore = true;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Snapshot recovered");
            });
            Recover<RecoveryCompleted>(_ =>
            {
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Recovery complete");
                DeleteMessages(long.MaxValue);
                DeleteSnapshots(new SnapshotSelectionCriteria(long.MaxValue));
            });
            
            Command<DeleteMessagesSuccess>(_ =>
            {
                _deletedJournal = true;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Journal events deleted");
                
                if(_deletedSnapshotStore is not null)
                {
                    Become(Active);
                    Stash.UnstashAll();
                }
            });
            
            Command<DeleteMessagesFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _deletedJournal = false;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Failed to delete journal events");
                
                if(_deletedSnapshotStore is not null)
                {
                    Become(Active);
                    Stash.UnstashAll();
                }
            });
            
            Command<DeleteSnapshotsSuccess>(_ =>
            {
                _deletedSnapshotStore = true;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Snapshot deleted");
                
                if(_deletedJournal is not null)
                {
                    Become(Active);
                    Stash.UnstashAll();
                }
            });
            
            Command<DeleteSnapshotsFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _deletedSnapshotStore = false;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Failed to delete snapshot");
                
                if(_deletedJournal is not null)
                {
                    Become(Active);
                    Stash.UnstashAll();
                }
            });
            
            CommandAny(_ => Stash.Stash());
        }

        private void Active()
        {
            Command<string>(str =>
            {
                _message = str;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Probe started, saving snapshot");
                SaveSnapshot(str);
            });
            
            Command<SaveSnapshotSuccess>(_ =>
            {
                _persistedSnapshotStore = true;
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Snapshot saved");
                Persist(_message, 
                    _ =>
                    {
                        _persistedJournal = true;
                        if(_debugLog)
                            _log.Debug($"{PersistenceId}: Journal persisted");
                        SendRecoveryStatusWhenFinished();
                    });
            });
            
            Command<SaveSnapshotFailure>(fail =>
            {
                _log.Error(fail.Cause,"Failed to save snapshot store");
                
                _failures.Add(fail.Cause);
                _persistedSnapshotStore = false;
                Persist(_message, 
                    _ =>
                    {
                        _persistedJournal = true;
                        if(_debugLog)
                            _log.Debug($"{PersistenceId}: Journal persisted");
                        SendRecoveryStatusWhenFinished();
                    });
            });

        }

        public override string PersistenceId { get; }

        private void SendRecoveryStatusWhenFinished()
        {
            // First case, snapshot failed to save or journal write was rejected, there will be no deletion.
            if( (_persistedSnapshotStore is false && _persistedJournal is { }) || (_persistedJournal is false && _persistedSnapshotStore is { }))
            {
                _probe.Tell(CreateStatus());
                Context.Stop(Self);
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: First case: " +
                               $"_persistedJournal:{_persistedJournal} " +
                               $"_persistedSnapshotStore:{_persistedSnapshotStore}, ");
                return;
            }
            
            // Second case, this is the first time the probe ran, there is no deletion
            if (_firstAttempt
                && _persistedJournal is { }
                && _persistedSnapshotStore is { })
            {
                var msg = _persistedJournal == true && _persistedSnapshotStore == true
                    ? "Warming up probe. Recovery status is still undefined"
                    : null;
                _probe.Tell(CreateStatus(msg));
                Context.Stop(Self);
                if (_debugLog)
                    _log.Debug($"{PersistenceId}: Second case: " +
                               $"_persistedJournal:{_persistedJournal} " +
                               $"_persistedSnapshotStore:{_persistedSnapshotStore}");
                return;
            }
            
            // Third case, all fields should be populated
            if (_persistedJournal is { }
                && _persistedSnapshotStore is { } 
                && _deletedJournal is { } 
                && _deletedSnapshotStore is { })
            {
                _probe.Tell(CreateStatus());
                Context.Stop(Self);
                if(_debugLog)
                    _log.Debug($"{PersistenceId}: Third case: " +
                               $"_persistedJournal:{_persistedJournal}, " +
                               $"_persistedSnapshotStore:{_persistedSnapshotStore}, " +
                               $"_recoveredJournal:{_recoveredJournal?.ToString() ?? "null"} " +
                               $"_recoveredSnapshotStore:{_recoveredSnapshotStore?.ToString() ?? "null"} " +
                               $"_deletedJournal:{_deletedJournal} " +
                               $"_deletedSnapshotStore:{_deletedSnapshotStore}");
            }
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            _log.Error(cause, "Journal persist failure");
            _failures.Add(cause);
            _persistedJournal = false;
            _probe.Tell(CreateStatus("Journal persist failure"));
            Context.Stop(Self);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            _log.Error(cause, "Journal persist rejected");
            _failures.Add(cause);
            _persistedJournal = false;
            _probe.Tell(CreateStatus("Journal persist rejected"));
            Context.Stop(Self);
        }

        protected override void OnRecoveryFailure(Exception reason, object? message = null)
        {
            var msg = $"Recovery failure{(message is null ? "" : $": {message}")}";
            _log.Error(reason, msg);
            
            _failures.Add(reason);
            _probe.Tell(CreateStatus(msg));
            Context.Stop(Self);
        }

        private PersistenceLivenessStatus CreateStatus(string? message = null)
            => new PersistenceLivenessStatus(
                warmup: _firstAttempt,
                journalRecovered: _recoveredJournal ?? false,
                snapshotRecovered: _recoveredSnapshotStore ?? false,
                journalPersisted: _persistedJournal ?? false,
                snapshotSaved: _persistedSnapshotStore ?? false,
                failures: _failures,
                message: message);
    }
}