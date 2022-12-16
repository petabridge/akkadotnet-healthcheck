// -----------------------------------------------------------------------
// <copyright file="AkkaLivenessService.cs" company="Petabridge, LLC">
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
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Akka.HealthCheck.Hosting.Web.Services
{
    /// <summary>
    /// An ASP.NET <see cref="IHealthCheck"/> service implementation for checking Akka.NET node liveness status.
    /// </summary>
    public class AkkaLivenessService : IAkkaHealthcheck
    {
        private const string Healthy = "Akka.NET node is alive";
        private const string UnHealthy = "Akka.NET node is not alive";
        private const string Exception = "Exception occured when processing cluster liveness";
        private const string Message = "message";
        
        private readonly IActorRef _probe;

        /// <summary>
        /// Creates a new <see cref="AkkaLivenessService"/> instance.
        /// Note that this constructor is meant to be called by ASP.NET and not called directly by the user.
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem"/> node</param>
        public AkkaLivenessService(ActorSystem system)
        {
            if (!AkkaHealthCheck.For(system).LivenessProbes.TryGetValue("default", out _probe!))
            {
                throw new ConfigurationException("Could not find liveness actor with key 'default'. AkkaHealthCheck have failed to start properly.");
            }
        }

        ///<inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await _probe.Ask<LivenessStatus>(
                    message: GetCurrentLiveness.Instance, 
                    cancellationToken: cancellationToken);
                
                return status.IsLive 
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