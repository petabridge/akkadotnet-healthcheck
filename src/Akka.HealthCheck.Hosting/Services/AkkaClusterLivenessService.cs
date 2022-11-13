﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Cluster;
using Akka.HealthCheck.Liveness;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Services
{
    public class AkkaClusterLivenessService : IHealthCheck
    {
        private readonly IActorRef _probe;

        public AkkaClusterLivenessService(ActorSystem system)
        {
            var sys = (ExtendedActorSystem)system;
            _probe = sys.SystemGuardian.GetChild(new[] { "asp-healthcheck-cluster-liveness" });
            if (_probe.Equals(ActorRefs.Nobody))
            {
                _probe = sys.SystemActorOf(
                    Props.Create(() => new ClusterLivenessProbe()),
                    "asp-healthcheck-cluster-liveness");
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await _probe.Ask<LivenessStatus>(
                    message: GetCurrentLiveness.Instance, 
                    cancellationToken: cancellationToken);
                return status.IsLive 
                    ? new HealthCheckResult(HealthStatus.Healthy, $"Status is live:{status.StatusMessage}") 
                    : new HealthCheckResult(HealthStatus.Unhealthy, $"Status is not live:{status.StatusMessage}");
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, "Probe is not live.", e);
            }
        }
    }
}
