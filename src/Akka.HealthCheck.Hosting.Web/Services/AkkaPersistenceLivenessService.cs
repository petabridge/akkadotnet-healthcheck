﻿// -----------------------------------------------------------------------
// <copyright file="AkkaPersistenceLivenessService.cs" company="Petabridge, LLC">
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
    /// An ASP.NET <see cref="IHealthCheck"/> service implementation for checking Akka.NET persistence liveness status.
    /// </summary>
    public class AkkaPersistenceLivenessService : IAkkaHealthcheck
    {
        private const string Healthy = "Akka.NET persistence is alive";
        private const string UnHealthy = "Akka.NET persistence is not alive";
        private const string Exception = "Exception occured when processing cluster liveness";
        private const string Message = "message";
        
        private readonly IActorRef _probe;

        /// <summary>
        /// Creates a new <see cref="AkkaClusterLivenessService"/> instance.
        /// Note that this constructor is meant to be called by ASP.NET and not called directly by the user.
        /// </summary>
        /// <param name="system">The <see cref="ActorSystem"/> that hosts the cluster node</param>
        public AkkaPersistenceLivenessService(ActorSystem system)
        {
            if (!AkkaHealthCheck.For(system).LivenessProbes.TryGetValue("persistence", out _probe!))
            {
                throw new ConfigurationException("Could not find liveness actor with key 'persistence'. Have you added persistence liveness provider in AkkaHealthCheckOptions?");
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