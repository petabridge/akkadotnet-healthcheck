using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.IHealthCheckExample.HealthChecks;

public class AkkaLivenessActorHealthCheck<T> : IHealthCheck where T : ActorBase
{
    private readonly IActorRef probe;

    public AkkaLivenessActorHealthCheck(ActorRegistry registry)
    {
        probe = registry.Get<T>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            LivenessStatus status = await probe.Ask<LivenessStatus>(GetCurrentLiveness.Instance);
            if (status.IsLive)
            {
                return new HealthCheckResult(HealthStatus.Healthy, $"Status is live:{status.StatusMessage}");
            }
            else
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, $"Status is not live:{status.StatusMessage}");
            }
        }
        catch (Exception e)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, $"Probe is not live.", e);
        }
    }
}
