using Akka.Actor;

namespace Akka.HealthCheck
{
    /// <summary>
    /// Used to construct liveness and readiness probes for detecting changes
    /// in system liveness or readiness.
    /// </summary>
    public interface IProbeProvider
    {
        /// <summary>
        /// The <see cref="Props"/> that will be used to create the liveness or readiness probe
        /// for this system.
        /// </summary>
        Props ProbeProps { get; }
    }
}
