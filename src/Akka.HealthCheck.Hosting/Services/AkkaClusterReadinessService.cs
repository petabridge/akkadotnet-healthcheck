using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Cluster;
using Akka.HealthCheck.Readiness;
using Akka.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Services
{
    /// <summary>
    /// An ASP.NET <see cref="IHealthCheck"/> service implementation for checking Akka.NET cluster readiness status.
    /// </summary>
    public class AkkaClusterReadinessService : IHealthCheck
    {
        private readonly IActorRef _probe;

        /// <summary>
        /// Creates a new <see cref="AkkaClusterReadinessService"/> instance.
        /// Note that this constructor is meant to be called by ASP.NET and not called directly by the user.
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem"/> that hosts the cluster node</param>
        public AkkaClusterReadinessService(ActorSystem system)
        {
            var sys = (ExtendedActorSystem)system;
            _probe = sys.SystemGuardian.GetChild(new[] { "asp-healthcheck-cluster-readiness" });
            if (_probe.Equals(ActorRefs.Nobody))
            {
                _probe = sys.SystemActorOf(
                    Props.Create(() => new ClusterReadinessProbe()),
                    "asp-healthcheck-cluster-readiness");
            }
        }

        ///<inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await _probe.Ask<ReadinessStatus>(
                    message: GetCurrentReadiness.Instance,
                    cancellationToken: cancellationToken);
                return status.IsReady 
                    ? new HealthCheckResult(HealthStatus.Healthy, $"Status is ready:{status.StatusMessage}") 
                    : new HealthCheckResult(HealthStatus.Unhealthy, $"Status is not ready:{status.StatusMessage}");
            }
            catch (Exception e)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, "Probe is not ready.", e);
            }
        }
    }    
}