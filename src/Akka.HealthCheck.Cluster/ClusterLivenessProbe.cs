// -----------------------------------------------------------------------
// <copyright file="ClusterLivenessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.HealthCheck.Liveness;

namespace Akka.HealthCheck.Cluster
{
    /// <summary>
    ///     Algorithm that indicates that we are live if we are a member of the cluster, and we are not
    ///     if we are removed from the cluster's membership.
    /// </summary>
    public sealed class ClusterLivenessProbe : ReceiveActor
    {
        public static readonly LivenessStatus DefaultClusterLivenessStatus =
            new LivenessStatus(true, "not yet joined cluster");

        private readonly Akka.Cluster.Cluster _cluster = Akka.Cluster.Cluster.Get(Context.System);
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private readonly bool _logInfo;

        private LivenessStatus _livenessStatus;

        public ClusterLivenessProbe(bool logInfo) : this(logInfo, DefaultClusterLivenessStatus)
        {
        }
        
        public ClusterLivenessProbe(bool logInfo, LivenessStatus livenessStatus)
        {
            _logInfo = logInfo;
            _livenessStatus = livenessStatus;

            Receive<LivenessStatus>(s =>
            {
                _livenessStatus = s;
                foreach (var sub in _subscribers) sub.Tell(s);
            });

            Receive<GetCurrentLiveness>(_ => Sender.Tell(_livenessStatus));

            Receive<SubscribeToLiveness>(s =>
            {
                _subscribers.Add(s.Subscriber);
                Context.Watch(s.Subscriber);
                s.Subscriber.Tell(_livenessStatus);
            });

            Receive<UnsubscribeFromLiveness>(u =>
            {
                _subscribers.Remove(u.Subscriber);
                Context.Unwatch(u.Subscriber);
            });

            Receive<Terminated>(t => { _subscribers.Remove(t.ActorRef); });
        }

        protected override void PreStart()
        {
            var self = Self;

            _cluster.RegisterOnMemberUp(() => { self.Tell(new LivenessStatus(true)); });

            _cluster.RegisterOnMemberRemoved(() => { self.Tell(new LivenessStatus(false)); });
        }
    }
}