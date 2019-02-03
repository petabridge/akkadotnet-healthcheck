// -----------------------------------------------------------------------
// <copyright file="MisconfiguredLivenessProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.HealthCheck.Configuration;

namespace Akka.HealthCheck.Liveness
{
    /// <summary>
    ///     Used in instances where <see cref="HealthCheckSettings.Misconfigured" /> is true.
    /// </summary>
    public sealed class MisconfiguredLivenessProvider : ProbeProviderBase
    {
        public MisconfiguredLivenessProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => DefaultLivenessProbe.MisconfiguredProbeProbs();
    }
}