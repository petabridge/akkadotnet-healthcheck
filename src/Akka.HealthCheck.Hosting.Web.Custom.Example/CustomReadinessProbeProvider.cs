// -----------------------------------------------------------------------
// <copyright file="CustomProbeProvider.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;

namespace Akka.HealthCheck.Hosting.Web.Custom.Example;

public sealed class CustomReadinessProbeProvider: ProbeProviderBase
{
    public override Props ProbeProps => Props.Create(() => new CustomReadinessProbe());
    
    public CustomReadinessProbeProvider(ActorSystem system) : base(system)
    {
    }
}