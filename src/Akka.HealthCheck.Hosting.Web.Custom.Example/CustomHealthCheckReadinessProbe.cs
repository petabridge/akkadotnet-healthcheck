// -----------------------------------------------------------------------
// <copyright file="CustomHealthCheckReadinessProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Web.Custom.Example;

public class CustomHealthCheckReadinessProbe: IHealthCheck
{
    private readonly IActorRef _probe;
    
    public CustomHealthCheckReadinessProbe(ActorSystem system)
    {
        if (!AkkaHealthCheck.For(system).ReadinessProbes.TryGetValue("custom", out _probe!))
        {
            throw new ConfigurationException("Could not find readiness actor with key 'custom'.");
        }
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            var status = await _probe.Ask<ReadinessStatus>(
                message: GetCurrentReadiness.Instance, 
                cancellationToken: cancellationToken);
                
            return status.IsReady
                ? HealthCheckResult.Healthy("healthy", new Dictionary<string, object> { ["message"] = status.StatusMessage })
                : HealthCheckResult.Unhealthy("unhealthy", data: new Dictionary<string, object> { ["message"] = status.StatusMessage });
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("unhealthy", e);
        }
    }
}