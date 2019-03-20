// -----------------------------------------------------------------------
// <copyright file="DefaultReadinessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;

namespace Akka.HealthCheck.Readiness
{
    /// <inheritdoc />
    /// <summary>
    ///     The default readiness probe implementation. Reports that the application
    ///     is ready as soon as the <see cref="T:Akka.Actor.ActorSystem" /> is up.
    /// </summary>
    public sealed class DefaultReadinessProbe : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly ReadinessStatus _readinessStatus;
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();

        public DefaultReadinessProbe() : this(new ReadinessStatus(true, $"Live: {DateTimeOffset.UtcNow}"))
        {
        }

        public DefaultReadinessProbe(ReadinessStatus readinessStatus)
        {
            _readinessStatus = readinessStatus;

            Receive<GetCurrentReadiness>(_ => {
                Sender.Tell(_readinessStatus);
            });

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

            Receive<Terminated>(t => {
                _subscribers.Remove(t.ActorRef);
            });
        }

        /// <summary>
        ///     Used in cases where the end-user screwed up their configuration.
        /// </summary>
        /// <returns><see cref="Props" /> for a <see cref="DefaultReadinessProbe" /> that will indicate the system is not ready.</returns>
        public static Props MisconfiguredProbeProbs()
        {
            return Props.Create(() => new DefaultReadinessProbe(new ReadinessStatus(false,
                "akka.healthcheck.readiness.provider is misconfigured. No suitable type found.")));
        }
    }
}