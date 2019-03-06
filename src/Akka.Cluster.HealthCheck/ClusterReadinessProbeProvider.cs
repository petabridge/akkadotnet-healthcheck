// -----------------------------------------------------------------------
// <copyright file="ClusterReadinessProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck;
using Akka.HealthCheck.Readiness;

namespace Akka.Cluster.HealthCheck
{
    /// <summary>
    ///     <see cref="IProbeProvider" /> readiness implementation intended for use with Akka.Cluster.
    /// </summary>
    public sealed class ClusterReadinessProbeProvider : ProbeProviderBase
    {
        public ClusterReadinessProbeProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => Props.Create(() => new ClusterReadinessProbe());
    }

    /// <summary>
    ///     Readiness algorithm for Akka.Cluster. We are ready when we join cluster,
    ///     not ready when we leave the cluster OR are the only reachable node in the cluster,
    ///     meaning that we have been partitioned away from everyone else for a lengthy period of
    ///     time.
    /// </summary>
    public sealed class ClusterReadinessProbe : ReceiveActor
    {
        public static readonly ReadinessStatus DefaultClusterReadinessStatus =
            new ReadinessStatus(false, "not yet joined cluster");

        private readonly Cluster _cluster = Cluster.Get(Context.System);
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly ReadinessStatus _readinessStatus;
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private ICancelable _notReadyTask;

        public ClusterReadinessProbe() : this(DefaultClusterReadinessStatus)
        {
        }

        public ClusterReadinessProbe(ReadinessStatus readinessStatus)
        {
            _readinessStatus = readinessStatus;

            Receive<GetCurrentReadiness>(_ => Sender.Tell(_readinessStatus));

            Receive<SubscribeToReadiness>(s =>
            {
                _subscribers.Add(s.Subscriber);
                Context.Watch(s.Subscriber);
                s.Subscriber.Tell(_readinessStatus);
            });

            Receive<UnsubscribeFromReadiness>(u =>
            {
                _subscribers.Remove(u.Subscriber);
                Context.Unwatch(u.Subscriber);
            });

            Receive<Terminated>(t => { _subscribers.Remove(t.ActorRef); });

            Receive<ClusterEvent.UnreachableMember>(r =>
            {
                if (_cluster.State.Unreachable.SetEquals(_cluster.State.Members.Remove(_cluster.SelfMember)))
                    if (_notReadyTask == null)
                        _notReadyTask = Context.System.Scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(20),
                            Self,
                            new ReadinessStatus(false, "everyone else is unreachable"), ActorRefs.NoSender);
            });

            Receive<ClusterEvent.ReachableMember>(r =>
            {
                // someone else has become reachable again. Can abort "not ready" task if it was already running
                _notReadyTask?.Cancel();
            });
        }

        protected override void PreStart()
        {
            var self = Self;

            _cluster.RegisterOnMemberUp(() => { self.Tell(new ReadinessStatus(true)); });

            _cluster.RegisterOnMemberRemoved(() => { self.Tell(new ReadinessStatus(false)); });

            _cluster.Subscribe(Self, ClusterEvent.SubscriptionInitialStateMode.InitialStateAsSnapshot,
                typeof(ClusterEvent.IReachabilityEvent));
        }
    }
}