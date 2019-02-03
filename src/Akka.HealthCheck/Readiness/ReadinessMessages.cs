// -----------------------------------------------------------------------
// <copyright file="ReadinessMessages.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Readiness
{
    /// <summary>
    ///     Subscribe to <see cref="ReadinessStatus" /> updates and receive the current
    ///     status from the probe.
    /// </summary>
    public sealed class SubscribeToReadiness
    {
        public SubscribeToReadiness(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        /// <summary>
        ///     The actor who will subscribe to <see cref="ReadinessStatus" /> notifications
        /// </summary>
        public IActorRef Subscriber { get; }
    }

    /// <summary>
    ///     Unsubscribe from notifications from the readiness probe.
    /// </summary>
    public sealed class UnsubscribeFromReadiness
    {
        public UnsubscribeFromReadiness(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        /// <summary>
        ///     The actor who will subscribe to <see cref="ReadinessStatus" /> notifications
        /// </summary>
        public IActorRef Subscriber { get; }
    }

    /// <summary>
    ///     Used to query the current <see cref="ReadinessStatus" /> from
    ///     the readiness probe actor.
    /// </summary>
    public sealed class GetCurrentReadiness
    {
        public static readonly GetCurrentReadiness Instance = new GetCurrentReadiness();

        private GetCurrentReadiness()
        {
        }
    }
}