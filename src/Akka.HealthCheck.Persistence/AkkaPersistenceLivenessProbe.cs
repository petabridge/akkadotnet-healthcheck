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

namespace Akka.HealthCheck.Persistence
{
    public class AkkaPersistenceLivenessProbe : ActorBase, IWithUnboundedStash
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private LivenessStatus _currentStatus = new LivenessStatus(false, "unknown");
        private bool _journalLive;
        private IActorRef _probe;
        private int _probeCounter;
        private bool _snapshotStoreLive;
        private readonly TimeSpan _delay;

        public AkkaPersistenceLivenessProbe(TimeSpan delay)
        {
            _delay = delay;
        }
        public AkkaPersistenceLivenessProbe() : this(TimeSpan.FromSeconds(10))
        {

        }
        public IStash Stash { get; set; }

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
                    Sender.Tell(_currentStatus);
                    break;
                case SubscribeToLiveness sub:
                    _subscribers.Add(sub.Subscriber);
                    Context.Watch(sub.Subscriber);
                    sub.Subscriber.Tell(_currentStatus);
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
                case RecoveryStatus status:
                    HandleRecoveryStatus(status);
                    return true;
            }

            return false;
        }

        private void HandleRecoveryStatus(RecoveryStatus status)
        {
            _log.Info("Received recovery status {0} from probe.", status);
            _journalLive = status.JournalRecovered;
            _snapshotStoreLive = status.SnapshotRecovered;
            _currentStatus = new LivenessStatus(_journalLive && _snapshotStoreLive, status.ToString());
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
                case RecoveryStatus status:
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
            _probe = Context.ActorOf(Props.Create(() => new AkkaPersistenceHealthCheckProbe(Self, firstTime)));
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
            foreach (var sub in _subscribers) sub.Tell(_currentStatus);
        }

        public class RecoveryStatus
        {
            public RecoveryStatus(bool journalRecovered, bool snapshotRecovered)
            {
                JournalRecovered = journalRecovered;
                SnapshotRecovered = snapshotRecovered;
            }

            public bool JournalRecovered { get; }

            public bool SnapshotRecovered { get; }

            public override string ToString()
            {
                return $"RecoveryStatus(JournalRecovered={JournalRecovered}, SnapshotRecovered={SnapshotRecovered})";
            }
        }
    }

    /// <summary>
    ///     Validate that the snapshot store and the journal and both working
    /// </summary>
    public class AkkaPersistenceHealthCheckProbe : ReceivePersistentActor
    {
        private readonly IActorRef _probe;
        private readonly bool _firstAttempt;
        private bool _recoveredJournal;
        private bool _recoveredSnapshotStore;

        public AkkaPersistenceHealthCheckProbe(IActorRef probe, bool firstAttempt)
        {
            _probe = probe;
            _firstAttempt = firstAttempt;
            PersistenceId = "Akka.HealthCheck";

            Recover<string>(str =>
            {
                _recoveredJournal = true;
                SendRecoveryStatusWhenFinished();
            });
            Recover<SnapshotOffer>(offer =>
            {
                _recoveredSnapshotStore = true;
                SendRecoveryStatusWhenFinished();
            });

            Command<string>(str =>
            {
                Persist(str, 
                s =>
                {
                    SaveSnapshot(s);
                }); });

            Command<SaveSnapshotSuccess>(save =>
            {
                if (!_firstAttempt)
                {
                    DeleteMessages(LastSequenceNr - 1);
                     DeleteSnapshots(new SnapshotSelectionCriteria(save.Metadata.SequenceNr - 1));
                }

                Context.Stop(Self);
            });
        }

        public override string PersistenceId { get; }

        private void SendRecoveryStatusWhenFinished()
        {
            if (IsRecoveryFinished)
                _probe.Tell(
                    new AkkaPersistenceLivenessProbe.RecoveryStatus(_recoveredJournal, _recoveredSnapshotStore));
        }

        protected override void OnRecoveryFailure(Exception reason, object message = null)
        {
            throw new ApplicationException("Failed to recover", reason);
        }
    }
}