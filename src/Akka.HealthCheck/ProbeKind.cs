// -----------------------------------------------------------------------
// <copyright file="ProbeKind.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck
{
    /// <summary>
    ///     Describes the type of probe used in this particular operation.
    /// </summary>
    public enum ProbeKind
    {
        /// <summary>
        ///     Liveness probes.
        /// </summary>
        Liveness,

        /// <summary>
        ///     Readiness probes.
        /// </summary>
        Readiness
    }
}