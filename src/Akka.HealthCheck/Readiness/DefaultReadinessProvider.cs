// -----------------------------------------------------------------------
// <copyright file="DefaultReadinessProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Readiness
{
    /// <inheritdoc />
    /// <summary>
    ///     The default <see cref="T:Akka.HealthCheck.IProbeProvider" /> implementation for readiness.
    /// </summary>
    public sealed class DefaultReadinessProvider : ProbeProviderBase
    {
        public DefaultReadinessProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => Props.Create(() => new DefaultReadinessProbe());
    }
}