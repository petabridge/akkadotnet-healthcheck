﻿// -----------------------------------------------------------------------
// <copyright file="MisconfiguredReadinessProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Readiness
{
    /// <summary>
    ///     Used when <see cref="Akka.HealthCheck.Configuration.HealthCheckSettings.Misconfigured" /> is true.
    /// </summary>
    public sealed class MisconfiguredReadinessProvider : ProbeProviderBase
    {
        public MisconfiguredReadinessProvider(ActorSystem system) : base(system)
        {
        }

        public override Props ProbeProps => DefaultReadinessProbe.MisconfiguredProbeProbs();
    }
}