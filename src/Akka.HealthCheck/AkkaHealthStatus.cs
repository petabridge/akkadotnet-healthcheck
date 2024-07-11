// -----------------------------------------------------------------------
// <copyright file="HealthStatus.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2024 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck;

public enum AkkaHealthStatus
{
    Degraded,
    Unhealthy,
    Healthy,
}