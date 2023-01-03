// -----------------------------------------------------------------------
// <copyright file="AkkaAspHostingExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Linq;
using Akka.HealthCheck.Hosting.Web.Probes;
using Akka.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Akka.HealthCheck.Hosting.Web
{
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
    
    public static class AkkaWebHostingExtensions
    {
        #region IServiceCollection extension methods

        public static IServiceCollection WithAkkaHealthCheck(this IServiceCollection services, HealthCheckType types)
        {
            var builder = services.AddHealthChecks();
            if((types & HealthCheckType.DefaultLiveness) > 0)
                builder.AddCheck<AkkaLivenessProbe>(Helper.Names.Liveness, HealthStatus.Unhealthy, Helper.Tags.Liveness);
            
            if((types & HealthCheckType.DefaultReadiness) > 0)
                builder.AddCheck<AkkaReadinessProbe>(Helper.Names.Readiness, HealthStatus.Unhealthy, Helper.Tags.Readiness);
            
            if((types & HealthCheckType.ClusterLiveness) > 0)
                builder.AddCheck<AkkaClusterLivenessProbe>(Helper.Names.ClusterLiveness, HealthStatus.Unhealthy, Helper.Tags.ClusterLiveness);
            
            if((types & HealthCheckType.ClusterReadiness) > 0)
                builder.AddCheck<AkkaClusterReadinessProbe>(Helper.Names.ClusterReadiness, HealthStatus.Unhealthy, Helper.Tags.ClusterReadiness);
            
            if((types & HealthCheckType.PersistenceLiveness) > 0)
                builder.AddCheck<AkkaPersistenceLivenessProbe>(Helper.Names.PersistenceLiveness, HealthStatus.Unhealthy, Helper.Tags.PersistenceLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaLivenessProbe(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaLivenessProbe>(Helper.Names.Liveness, HealthStatus.Unhealthy, Helper.Tags.Liveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaReadinessProbe(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaReadinessProbe>(Helper.Names.Readiness, HealthStatus.Unhealthy, Helper.Tags.Readiness);

            return services;
        }
        
        public static IServiceCollection WithAkkaPersistenceLivenessProbe(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaPersistenceLivenessProbe>(Helper.Names.PersistenceLiveness, HealthStatus.Unhealthy, Helper.Tags.PersistenceLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaClusterLivenessProbe(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaClusterLivenessProbe>(Helper.Names.ClusterLiveness, HealthStatus.Unhealthy, Helper.Tags.ClusterLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaClusterReadinessProbe(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaClusterReadinessProbe>(Helper.Names.ClusterReadiness, HealthStatus.Unhealthy, Helper.Tags.ClusterReadiness);

            return services;
        }
        
        #endregion

        #region WebApplication extension methods

        private static IEndpointConventionBuilder MapAkkaHealthCheckService<T>(
            this IEndpointRouteBuilder builder,
            ISet<string> tags,
            string prependPath = "/healthz",
            Action<ISet<string>, HealthCheckOptions>? optionConfigure = null) where T: IAkkaHealthcheck
        {
            var path = prependPath.SanitizePath();
            var opt = new HealthCheckOptions();
            optionConfigure?.Invoke(tags, opt);

            var type = typeof(T);
            return type switch
            {
                _ when type == typeof(AkkaLivenessProbe) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.Liveness.ToPath()}", opt.WithPredicate(Helper.Filters.Liveness)),
                _ when type == typeof(AkkaClusterLivenessProbe) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.ClusterLiveness.ToPath()}", opt.WithPredicate(Helper.Filters.ClusterLiveness)),
                _ when type == typeof(AkkaPersistenceLivenessProbe) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.PersistenceLiveness.ToPath()}", opt.WithPredicate(Helper.Filters.PersistenceLiveness)),
                _ when type == typeof(AkkaReadinessProbe) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.Readiness.ToPath()}", opt.WithPredicate(Helper.Filters.Readiness)),
                _ when type == typeof(AkkaClusterReadinessProbe) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.ClusterReadiness.ToPath()}", opt.WithPredicate(Helper.Filters.ClusterReadiness)),
                _ => throw new Exception($"Unknown Akka.HealthCheck ASP.NET service: {type}")
            };
        }
        
        public static IEndpointRouteBuilder MapAkkaHealthCheckRoutes(
            this IEndpointRouteBuilder builder, 
            string prependPath = "/healthz",
            Action<ISet<string>, HealthCheckOptions>? optionConfigure = null,
            Action<ISet<string>, IEndpointConventionBuilder>? endpointConfigure = null)
        {
            var services = builder.ServiceProvider;
            var hcOpt = services.GetService<IOptions<HealthCheckServiceOptions>>();
            if (hcOpt == null)
                return builder;
            
            var containsLive = false;
            var containsReady = false;
            var regs = hcOpt.Value.Registrations.ToHashSet();
            
            var reg = regs.FirstOrDefault(r => r.Name == Helper.Names.Liveness);
            if (reg is { })
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaLivenessProbe>(reg.Tags, prependPath, optionConfigure);
                endpointConfigure?.Invoke(reg.Tags, epBuilder);
            }
            
            reg = regs.FirstOrDefault(r => r.Name == Helper.Names.ClusterLiveness);
            if (reg is { })
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaClusterLivenessProbe>(reg.Tags, prependPath, optionConfigure);
                endpointConfigure?.Invoke(reg.Tags, epBuilder);
            }
            
            reg = regs.FirstOrDefault(r => r.Name == Helper.Names.PersistenceLiveness);
            if (reg is { })
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaPersistenceLivenessProbe>(reg.Tags, prependPath, optionConfigure);
                endpointConfigure?.Invoke(reg.Tags, epBuilder);
            }

            reg = regs.FirstOrDefault(r => r.Name == Helper.Names.Readiness);
            if (reg is { })
            {
                containsReady = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaReadinessProbe>(reg.Tags, prependPath, optionConfigure);
                endpointConfigure?.Invoke(reg.Tags, epBuilder);
            }

            reg = regs.FirstOrDefault(r => r.Name == Helper.Names.ClusterReadiness);
            if (reg is { })
            {
                containsReady = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaClusterReadinessProbe>(reg.Tags, prependPath, optionConfigure);
                endpointConfigure?.Invoke(reg.Tags, epBuilder);
            }

            var path = prependPath.SanitizePath();
            if (containsLive)
            {
                var opt = new HealthCheckOptions();
                optionConfigure?.Invoke(Helper.Tags.Live.ToHashSet(), opt);
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Live.ToPath()}", opt.WithPredicate(Helper.Filters.AllLiveness));
                endpointConfigure?.Invoke(Helper.Tags.Live.ToHashSet(), epBuilder);
            }

            if (containsReady)
            {
                var opt = new HealthCheckOptions();
                optionConfigure?.Invoke(Helper.Tags.Ready.ToHashSet(), opt);
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Ready.ToPath()}", opt.WithPredicate(Helper.Filters.AllReadiness));
                endpointConfigure?.Invoke(Helper.Tags.Ready.ToHashSet(), epBuilder);
            }

            if (containsReady || containsLive)
            {
                var opt = new HealthCheckOptions();
                optionConfigure?.Invoke(Helper.Tags.Akka.ToHashSet(), opt);
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Akka.ToPath()}", opt.WithPredicate(Helper.Filters.All));
                endpointConfigure?.Invoke(Helper.Tags.Akka.ToHashSet(), epBuilder);
            }
            
            return builder;
        }

        private static HealthCheckOptions WithPredicate(
            this HealthCheckOptions option, 
            Func<HealthCheckRegistration, bool> predicate)
            => new HealthCheckOptions
            {
                AllowCachingResponses = option.AllowCachingResponses,
                ResponseWriter = option.ResponseWriter,
                ResultStatusCodes = option.ResultStatusCodes,
                Predicate = predicate
            };
        
        private static string SanitizePath(this string prependPath)
        {
            prependPath = prependPath.TrimEnd('/');
            if (prependPath[0] != '/')
                prependPath = $"/{prependPath}";
            return prependPath;
        }

        #endregion
        
        public static AkkaConfigurationBuilder WithWebHealthCheck(
            this AkkaConfigurationBuilder builder,
            IServiceProvider services)
        {
            var hcOpt = services.GetService<IOptions<HealthCheckServiceOptions>>();
            if (hcOpt == null)
                return builder;
            
            var regs = hcOpt.Value.Registrations.ToHashSet();
            
            var options = new AkkaHealthCheckOptions();
            if (regs.Any(r => r.Name == Helper.Names.PersistenceLiveness))
            {
                options.AddPersistenceLivenessProvider();
            }

            if (regs.Any(r => r.Name == Helper.Names.ClusterLiveness))
            {
                options.AddClusterLivenessProvider();
            }
            
            if (regs.Any(r => r.Name == Helper.Names.ClusterReadiness))
            {
                options.AddClusterReadinessProvider();
            }

            var config = options.ToConfig();
            if (config is { })
            {
                builder.AddHocon(config, HoconAddMode.Prepend);
            }
            
            builder.AddStartup((system, _) =>
            {
                AkkaHealthCheck.For(system);
            });
            
            return builder;
        }
        

    }
}