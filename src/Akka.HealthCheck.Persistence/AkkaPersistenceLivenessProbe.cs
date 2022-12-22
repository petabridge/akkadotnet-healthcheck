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
            bool snapshotPersisted, 
            IReadOnlyCollection<Exception> failures,
            string? message = null): base(false)
        {
            JournalRecovered = journalRecovered;
            SnapshotRecovered = snapshotRecovered;
            JournalPersisted = journalPersisted;
            SnapshotPersisted = snapshotPersisted;
            Failures = failures.Count > 0 ? new AggregateException(failures) : null;
            _message = message;
        }

        public override bool IsLive => JournalRecovered
                                       && SnapshotRecovered
                                       && JournalPersisted
                                       && SnapshotPersisted
                                       && Failures is null;

        public override string StatusMessage => _message ?? ToString();

        public bool JournalRecovered { get; }

        public bool SnapshotRecovered { get; }
            
        public bool JournalPersisted { get; }
            
        public bool SnapshotPersisted { get; }

        public Exception? Failures { get; }

        public override string ToString()
        {
            return $"{nameof(PersistenceLivenessStatus)}(" +
                   $"{nameof(JournalRecovered)}={JournalRecovered}, " +
                   $"{nameof(SnapshotRecovered)}={SnapshotRecovered}, " +
                   $"{nameof(JournalPersisted)}={JournalPersisted}, " +
                   $"{nameof(SnapshotPersisted)}={SnapshotPersisted}, " +
                   $"{nameof(Failures)}={Failures?.ToString() ?? "null"})";
        }
    }
    
    public class AkkaPersistenceLivenessProbe : ActorBase
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private PersistenceLivenessStatus _currentLivenessStatus = new PersistenceLivenessStatus(message: "Persistence is still starting up");
        private IActorRef? _probe;
        private int _probeCounter;
        private readonly TimeSpan _delay;
        private readonly string _id;

        public AkkaPersistenceLivenessProbe(TimeSpan delay)
        {
            _delay = delay;
            _id = Guid.NewGuid().ToString("N");
        }
        public AkkaPersistenceLivenessProbe() : this(TimeSpan.FromSeconds(10))
        {
        }

        public static Props PersistentHealthCheckProps()
        {
            // need to use the stopping strategy in case things blow up right away
            return Props.Create(() => new AkkaPersistenceLivenessProbe())
                .WithSupervisorStrategy(Actor.SupervisorStrategy.StoppingStrategy);
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

        private bool Started(object message)
        {
            switch (message)
            {
                case Terminated t when t.ActorRef.Equals(_probe):
                    _log.Info("Persistence probe terminated. Recreating...");
                    CreateProbe(false);
                    Become(obj => Recreating(obj) || HandleSubscriptions(obj));
                    return true;
                case PersistenceLivenessStatus status:
                    HandleRecoveryStatus(status);
                    return true;
            }

            return false;
        }

        private void HandleRecoveryStatus(PersistenceLivenessStatus livenessStatus)
        {
            _log.Info("Received recovery status {0} from probe.", livenessStatus);
            _currentLivenessStatus = livenessStatus;
            PublishStatusUpdates();
        }

        private bool Recreating(object message)
        {
            switch (message)
            {
                case Terminated t when t.ActorRef.Equals(_probe):
                    _log.Debug("Persistence probe terminated. Recreating...");
                    CreateProbe(false);
                    return true;
                case PersistenceLivenessStatus status:
                    HandleRecoveryStatus(status);
                    return true;
            }

            return false;
        }

        protected override bool Receive(object message)
        {
            return Started(message) || HandleSubscriptions(message);
        }

        protected override void PreStart()
        {
            CreateProbe(true);
        }

        private void CreateProbe(bool firstTime)
        {
            _probe = Context.ActorOf(Props.Create(() => new SuicideProbe(Self, firstTime, _id)));
            if(firstTime)
            {
                _probe.Tell("hit" + _probeCounter++);
            }
            else
            {
                Context.System.Scheduler.ScheduleTellOnce(_delay, _probe, "hit" + _probeCounter++, Self);
            }
            Context.Watch(_probe);
        }

        private void PublishStatusUpdates()
        {
            foreach (var sub in _subscribers) sub.Tell(_currentLivenessStatus);
        }
    }

    /// <summary>
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    public class SuicideProbe : ReceivePersistentActor 
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _probe;
        private readonly bool _firstAttempt;
        
        private bool _recoveredJournal;
        private bool _recoveredSnapshotStore;
        private bool? _persistedJournal;
        private bool? _persistedSnapshotStore;
        private bool? _deletedJournal;
        private bool? _deletedSnapshotStore;
        private readonly List<Exception> _failures = new List<Exception>();
        
        public SuicideProbe(IActorRef probe, bool firstAttempt, string id)
        {
            _probe = probe;
            _firstAttempt = firstAttempt;
            PersistenceId = $"Akka.HealthCheck-{id}";

            Recover<string>(str =>
            {
                _recoveredJournal = true;
            });
            Recover<SnapshotOffer>(offer =>
            {
                _recoveredSnapshotStore = true;
            });

            Command<string>(str =>
            {
                SaveSnapshot(str);
                Persist(str, 
                s =>
                {
                    _persistedJournal = true;
                    SendRecoveryStatusWhenFinished();
                });
            });
            
            Command<WriteMessageFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _persistedJournal = false;
                SendRecoveryStatusWhenFinished();
            });

            Command<SaveSnapshotSuccess>(save =>
            {
                _persistedSnapshotStore = true;
                if (!_firstAttempt)
                {
                    DeleteMessages(save.Metadata.SequenceNr - 1);
                    DeleteSnapshots(new SnapshotSelectionCriteria(save.Metadata.SequenceNr - 1));
                }
                SendRecoveryStatusWhenFinished();
            });
            
            Command<SaveSnapshotFailure>(fail =>
            {
                _log.Error(fail.Cause,"Failed to save snapshot store");
                
                _failures.Add(fail.Cause);
                _persistedSnapshotStore = false;
                SendRecoveryStatusWhenFinished();
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
            
            Command<DeleteSnapshotFailure>(fail =>
            {
                _failures.Add(fail.Cause);
                _deletedSnapshotStore = false;
                SendRecoveryStatusWhenFinished();
            });
        }

        public override string PersistenceId { get; }

        private void SendRecoveryStatusWhenFinished()
        {
            // First case, snapshot failed to save, there will be no deletion
            if(_persistedSnapshotStore is false && _persistedJournal is { })
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
                    ? "Warming up probe. Recovery status is still undefined" : null;
                _probe.Tell(CreateStatus(msg));
                Context.Stop(Self);
            }
            
            // Third case, all fields should be populated
            if (_persistedJournal is { } 
                && _persistedSnapshotStore is { } 
                && _deletedJournal is { } 
                && _deletedSnapshotStore is { })
            {
                _probe.Tell(CreateStatus(_firstAttempt ? "Warming up probe. Recovery status is still undefined" : null));
                Context.Stop(Self);
            }
        }

        protected override void OnRecoveryFailure(Exception reason, object? message = null)
        {
            var msg = $"Recovery failure{(message is null ? "" : $": {message}")}";
            _log.Error(reason, msg);
            
            _failures.Add(reason);
            _probe.Tell(CreateStatus(msg));
            
            throw new ApplicationException(msg, reason);
        }

        private PersistenceLivenessStatus CreateStatus(string? message = null)
            => new PersistenceLivenessStatus(
                journalRecovered: _recoveredJournal,
                snapshotRecovered: _recoveredSnapshotStore,
                journalPersisted: _persistedJournal ?? false,
                snapshotPersisted: _persistedSnapshotStore ?? false,
                failures: _failures,
                message: message);
    }
}