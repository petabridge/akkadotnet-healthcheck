// -----------------------------------------------------------------------
// <copyright file="DefaultReadinessProbeSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.HealthCheck.Readiness;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Tests
{
    public class DefaultReadinessProbeSpec : ReadinessProbeSpecBase
    {
        public DefaultReadinessProbeSpec(ITestOutputHelper helper) : base(helper)
        {
        }

        protected override Props ReadinessProbeProps => Props.Create(() => new DefaultReadinessProbe());
    }
}