// -----------------------------------------------------------------------
// <copyright file="DefaultLivenessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace Akka.HealthCheck.Liveness
{
    /// <inheritdoc />
    /// <summary>
    ///     The default liveness probe implementation. Reports that the application
    ///     is live as soon as the <see cref="T:Akka.Actor.ActorSystem" /> is live.
    /// </summary>
    public sealed class DefaultLivenessProbe : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly LivenessStatus _livenessStatus;
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();

        public DefaultLivenessProbe() : this(new LivenessStatus(true, $"Live: {DateTimeOffset.UtcNow}"))
        {
            _log.Debug("Liveness probe correctly configuread");
        }

        public DefaultLivenessProbe(LivenessStatus livenessStatus)
        {
            _livenessStatus = livenessStatus;

            Receive<GetCurrentLiveness>(_ => {
                _log.Debug("Liveness status {0} reported.", _livenessStatus);
                Sender.Tell(_livenessStatus);
            });

            Receive<SubscribeToLiveness>(s =>
            {
                _log.Debug("Liveness subscription.");
                _subscribers.Add(s.Subscriber);
                Context.Watch(s.Subscriber);
                s.Subscriber.Tell(_livenessStatus);
            });

            Receive<UnsubscribeFromLiveness>(u =>
            {
                _log.Debug("Unsibscribed from Liveness");
                _subscribers.Remove(u.Subscriber);
                Context.Unwatch(u.Subscriber);
            });

            Receive<Terminated>(t => { _subscribers.Remove(t.ActorRef); });
        }

        /// <summary>
        ///     Used in cases where the end-user screwed up their configuration.
        /// </summary>
        /// <returns><see cref="Props" /> for a <see cref="DefaultLivenessProbe" /> that will indicate the system is not live.</returns>
        public static Props MisconfiguredProbeProbs()
        {
            return Props.Create(() => new DefaultLivenessProbe(new LivenessStatus(false,
                "akka.healthcheck.liveness.provider is misconfigured. No suitable type found.")));
        }
    }
}