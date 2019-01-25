// -----------------------------------------------------------------------
// <copyright file="DefaultLivenessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;

namespace Akka.HealthCheck.Liveness
{
    /// <inheritdoc />
    /// <summary>
    ///     The default liveness probe implementation. Reports that the application
    ///     is live as soon as the <see cref="T:Akka.Actor.ActorSystem" /> is live.
    /// </summary>
    public sealed class DefaultLivenessProbe : ReceiveActor
    {
        private readonly LivenessStatus _livenessStatus;
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();

        public DefaultLivenessProbe() : this(new LivenessStatus(true, $"Live: {DateTimeOffset.UtcNow}"))
        {
        }

        public DefaultLivenessProbe(LivenessStatus livenessStatus)
        {
            _livenessStatus = livenessStatus;

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
    }
}