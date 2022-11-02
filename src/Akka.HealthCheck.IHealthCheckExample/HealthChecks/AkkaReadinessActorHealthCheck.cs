using Akka.Actor;
using Akka.HealthCheck.Readiness;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.IHealthCheckExample.HealthChecks;

public class AkkaReadinessActorHealthCheck<T> : IHealthCheck where T : ActorBase
{
    private readonly IActorRef probe;

    public AkkaReadinessActorHealthCheck(ActorRegistry registry)
    {
        probe = registry.Get<T>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ReadinessStatus status = await probe.Ask<ReadinessStatus>(GetCurrentReadiness.Instance);
            if (status.IsReady)
            {
                return new HealthCheckResult(HealthStatus.Healthy, $"Status is ready:{status.StatusMessage}");
            }
            else
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, $"Status is not ready:{status.StatusMessage}");
            }
        }
        catch (Exception e)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, $"Probe is not ready.", e);
        }
    }
}