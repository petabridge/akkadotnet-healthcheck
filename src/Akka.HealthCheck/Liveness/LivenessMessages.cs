using Akka.Actor;

namespace Akka.HealthCheck.Liveness
{
    /// <summary>
    /// Subscribe to <see cref="LivenessStatus"/> updates and receive the current
    /// status from the probe.
    /// </summary>
    public sealed class SubscribeToLiveness
    {
        public SubscribeToLiveness(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        /// <summary>
        /// The actor who will subscribe to <see cref="LivenessStatus"/> notifications
        /// </summary>
        public IActorRef Subscriber { get; }
    }

    /// <summary>
    /// Unsubscribe from notifications from the liveness probe. 
    /// </summary>
    public sealed class UnsubscribeFromLiveness
    {
        public UnsubscribeFromLiveness(IActorRef subscriber)
        {
            Subscriber = subscriber;
        }

        /// <summary>
        /// The actor who will subscribe to <see cref="LivenessStatus"/> notifications
        /// </summary>
        public IActorRef Subscriber { get; }
    }

    /// <summary>
    /// Used to query the current <see cref="LivenessStatus"/> from
    /// the liveness probe actor.
    /// </summary>
    public sealed class GetCurrentLiveness
    {
        public static readonly GetCurrentLiveness Instance = new GetCurrentLiveness();

        private GetCurrentLiveness() { }
    }
}