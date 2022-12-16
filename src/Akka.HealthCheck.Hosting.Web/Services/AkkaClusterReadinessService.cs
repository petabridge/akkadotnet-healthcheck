// -----------------------------------------------------------------------
// <copyright file="AkkaClusterReadinessService.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Readiness;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Web.Services
{
    /// <summary>
    /// An ASP.NET <see cref="IHealthCheck"/> service implementation for checking Akka.NET cluster readiness status.
    /// </summary>
    public class AkkaClusterReadinessService : IAkkaHealthcheck
    {
        private const string Healthy = "Akka.NET cluster is ready";
        private const string UnHealthy = "Akka.NET cluster is not ready";
        private const string Exception = "Exception occured when processing cluster liveness";
        private const string Message = "message";
        
        private readonly IActorRef _probe;

        /// <summary>
        /// Creates a new <see cref="AkkaClusterReadinessService"/> instance.
        /// Note that this constructor is meant to be called by ASP.NET and not called directly by the user.
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem"/> that hosts the cluster node</param>
        public AkkaClusterReadinessService(ActorSystem system)
        {
            if (!AkkaHealthCheck.For(system).ReadinessProbes.TryGetValue("cluster", out _probe!))
            {
                throw new ConfigurationException("Could not find readiness actor with key 'cluster'. Have you added cluster readiness provider in AkkaHealthCheckOptions?");
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
                    ? HealthCheckResult.Healthy(Healthy, new Dictionary<string, object> { [Message] = status.StatusMessage })
                    : HealthCheckResult.Unhealthy(UnHealthy, data: new Dictionary<string, object> { [Message] = status.StatusMessage });
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy(Exception, e, new Dictionary<string, object> { [Message] = Exception });
            }
        }
    }
}