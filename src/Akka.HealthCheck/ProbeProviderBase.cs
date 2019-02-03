// -----------------------------------------------------------------------
// <copyright file="ProbeProviderBase.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck
{
    /// <inheritdoc />
    /// <summary>
    ///     Abstract base class for the <see cref="T:Akka.HealthCheck.IProbeProvider" />, designed
    ///     to make it easier for implementors to supply the correct default
    ///     constructor arguments.
    /// </summary>
    public abstract class ProbeProviderBase : IProbeProvider
    {
        /// <summary>
        ///     Constructor takes the <see cref="ActorSystem" /> on which this
        ///     healthcheck will run as a current argument. Designed to allow
        ///     the implementor to pass in arguments into <see cref="ProbeProps" />,
        ///     such as references to other actors, if need be.
        /// </summary>
        /// <param name="system">The current actor system.</param>
        protected ProbeProviderBase(ActorSystem system)
        {
        }

        /// <inheritdoc cref="IProbeProvider.ProbeProps" />
        public abstract Props ProbeProps { get; }
    }
}