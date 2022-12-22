// -----------------------------------------------------------------------
// <copyright file="BaseService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Web.Probes
{
    public interface IAkkaHealthcheck: IHealthCheck
    { }
}