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
        
        public PersistenceLivenessStatus(string message): this(false, false, false, false, Array.Empty<Exception>(), message)
        {
        }
            
        public PersistenceLivenessStatus(
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
            Failures = failures.Count > 0 ? new AggregateException(failures) : null;
            _message = message;
        }

        public override bool IsLive => JournalRecovered
                                       && SnapshotRecovered
                                       && JournalPersisted
                                       && SnapshotSaved
                                       && Failures is null;

        public override string StatusMessage => _message ?? ToString();

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
    
    public class AkkaPersistenceLivenessProbe : ActorBase
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private PersistenceLivenessStatus _currentLivenessStatus = new(message: "Warming up probe. Recovery status is still undefined");
        private IActorRef? _probe;
        private int _probeCounter;
        private readonly TimeSpan _delay;
        private readonly string _id;
        private readonly Cancelable _shutdownCancellable;
        private readonly bool _logInfo;

        public AkkaPersistenceLivenessProbe(bool logInfo, TimeSpan delay)
        {
            _delay = delay;
            _id = Guid.NewGuid().ToString("N");
            _shutdownCancellable = new Cancelable(Context.System.Scheduler);
            _logInfo = logInfo;
            
            Become(obj => HandleMessages(obj) || HandleSubscriptions(obj));
        }

        public static Props PersistentHealthCheckProps(bool logInfo, TimeSpan delay)
        {
            // need to use the stopping strategy in case things blow up right away
            return Props.Create(() => new AkkaPersistenceLivenessProbe(logInfo, delay))
                .WithSupervisorStrategy(Actor.SupervisorStrategy.StoppingStrategy);
        }

        protected override void PostStop()
        {
            _probe?.Tell(PoisonPill.Instance);
            _shutdownCancellable.Cancel();
            _shutdownCancellable.Dispose();
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
                _log.Debug("Received recovery status {0} from probe.", livenessStatus);
            _currentLivenessStatus = livenessStatus;
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
                    ScheduleProbeRestart();
                    return true;
                
                case CreateProbe:
                    if(_logInfo)
                        _log.Debug("Recreating persistence probe.");
                    
                    _probe = Context.ActorOf(Props.Create(() => new SuicideProbe(Self, _probeCounter == 0, _id)));
                    Context.Watch(_probe);
                    _probe.Tell("hit" + _probeCounter);
                    _probeCounter++;
                    return true;
                
                case PersistenceLivenessStatus status:
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
            Context.System.Scheduler.ScheduleTellOnce(_delay, Self, CreateProbe.Instance, Self, _shutdownCancellable);
        }
        
        private void PublishStatusUpdates()
        {
            foreach (var sub in _subscribers) sub.Tell(_currentLivenessStatus);
        }
    }

    /// <summary>
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    internal class SuicideProbe : ReceivePersistentActor 
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _probe;
        private readonly bool _firstAttempt;

        private string? _message;
        private bool? _recoveredJournal;
        private bool? _recoveredSnapshotStore;
        private bool? _persistedJournal;
        private bool? _persistedSnapshotStore;
        private bool? _deletedJournal;
        private bool? _deletedSnapshotStore;
        private readonly List<Exception> _failures = new ();
        
        public SuicideProbe(IActorRef probe, bool firstAttempt, string id)
        {
            _probe = probe;
            _firstAttempt = firstAttempt;
            PersistenceId = $"Akka.HealthCheck-{id}";

            Recover<string>(_ =>
            {
                _recoveredJournal = true;
            });
            Recover<SnapshotOffer>(_ =>
            {
                _recoveredSnapshotStore = true;
            });
            Recover<RecoveryCompleted>(_ =>
            {
                DeleteMessages(long.MaxValue);
                DeleteSnapshots(new SnapshotSelectionCriteria(long.MaxValue));
            });

            Command<string>(str =>
            {
                _message = str;
                SaveSnapshot(str);
            });
            
            Command<SaveSnapshotSuccess>(_ =>
            {
                _persistedSnapshotStore = true;
                Persist(_message, 
                    _ =>
                    {
                        _persistedJournal = true;
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
                        SendRecoveryStatusWhenFinished();
                    });
            });
            
            Command<DeleteMessagesSuccess>(_ =>
            {
                _deletedJournal = true;
                SendRecoveryStatusWhenFinished();
            });
            
            Command<DeleteMessagesFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _deletedJournal = false;
                SendRecoveryStatusWhenFinished();
            });
            
            Command<DeleteSnapshotsSuccess>(_ =>
            {
                _deletedSnapshotStore = true;
                SendRecoveryStatusWhenFinished();
            });
            
            Command<DeleteSnapshotsFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _deletedSnapshotStore = false;
                SendRecoveryStatusWhenFinished();
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
            }
            
            // Third case, all fields should be populated
            if (_recoveredJournal is { }
                && _recoveredSnapshotStore is { }
                && _persistedJournal is { } 
                && _persistedSnapshotStore is { } 
                && _deletedJournal is { } 
                && _deletedSnapshotStore is { })
            {
                _probe.Tell(CreateStatus());
                Context.Stop(Self);
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
                journalRecovered: _recoveredJournal ?? false,
                snapshotRecovered: _recoveredSnapshotStore ?? false,
                journalPersisted: _persistedJournal ?? false,
                snapshotSaved: _persistedSnapshotStore ?? false,
                failures: _failures,
                message: message);
    }
}