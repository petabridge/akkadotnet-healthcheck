// -----------------------------------------------------------------------
// <copyright file="DefaultLivenessProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Liveness
{
    /// <inheritdoc />
    /// <summary>
    ///     The default <see cref="T:Akka.HealthCheck.IProbeProvider" /> implementation for liveness checks.
    /// </summary>
    public sealed class DefaultLivenessProvider : ProbeProviderBase
    {
        public DefaultLivenessProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => Props.Create(() => new DefaultLivenessProbe());
    }
}