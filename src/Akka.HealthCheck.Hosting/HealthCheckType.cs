// -----------------------------------------------------------------------
// <copyright file="HealthCheckType.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Akka.HealthCheck.Hosting;

[Flags]
public enum HealthCheckType
{
    DefaultLiveness = 1,
    DefaultReadiness = 2,
    Default = DefaultLiveness | DefaultReadiness,
    ClusterLiveness = 4,
    ClusterReadiness = 8,
    Cluster = ClusterLiveness | ClusterReadiness,
    PersistenceLiveness = 16,
    Persistence = PersistenceLiveness,
    All = Default | Cluster | Persistence
}