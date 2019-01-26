// -----------------------------------------------------------------------
// <copyright file="IProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck
{
    /// <summary>
    ///     Used to construct liveness and readiness probes for detecting changes
    ///     in system liveness or readiness.
    /// </summary>
    /// <remarks>
    /// NOTE: all <see cref="IProbeProvider"/> implementations need to take a <see cref="ActorSystem"/>
    /// as their only constructor argument. Use <see cref="ProbeProviderBase"/> as a template if you wish
    /// to extend Akka.HealthCheck with your own custom <see cref="IProbeProvider"/> implementations.
    /// </remarks>
    public interface IProbeProvider
    {
        /// <summary>
        ///     The <see cref="Props" /> that will be used to create the liveness or readiness probe
        ///     for this system.
        /// </summary>
        Props ProbeProps { get; }
    }
}