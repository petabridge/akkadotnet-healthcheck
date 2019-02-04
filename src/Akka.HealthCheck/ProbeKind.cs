namespace Akka.HealthCheck
{
    /// <summary>
    /// Describes the type of probe used in this particular operation.
    /// </summary>
    public enum ProbeKind
    {
        /// <summary>
        /// Liveness probes.
        /// </summary>
        Liveness,

        /// <summary>
        /// Readiness probes.
        /// </summary>
        Readiness
    }
}