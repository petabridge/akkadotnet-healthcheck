// -----------------------------------------------------------------------
// <copyright file="AkkaAspHostingExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------


using System;
using System.Linq;
using Akka.HealthCheck.Hosting.Web.Services;
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
                builder.AddCheck<AkkaLivenessService>(Helper.Names.Liveness, HealthStatus.Unhealthy, Helper.Tags.Liveness);
            
            if((types & HealthCheckType.DefaultReadiness) > 0)
                builder.AddCheck<AkkaReadinessService>(Helper.Names.Readiness, HealthStatus.Unhealthy, Helper.Tags.Readiness);
            
            if((types & HealthCheckType.ClusterLiveness) > 0)
                builder.AddCheck<AkkaClusterLivenessService>(Helper.Names.ClusterLiveness, HealthStatus.Unhealthy, Helper.Tags.ClusterLiveness);
            
            if((types & HealthCheckType.ClusterReadiness) > 0)
                builder.AddCheck<AkkaClusterReadinessService>(Helper.Names.ClusterReadiness, HealthStatus.Unhealthy, Helper.Tags.ClusterReadiness);
            
            if((types & HealthCheckType.PersistenceLiveness) > 0)
                builder.AddCheck<AkkaPersistenceLivenessService>(Helper.Names.PersistenceLiveness, HealthStatus.Unhealthy, Helper.Tags.PersistenceLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaLivenessService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaLivenessService>(Helper.Names.Liveness, HealthStatus.Unhealthy, Helper.Tags.Liveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaReadinessService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaReadinessService>(Helper.Names.Readiness, HealthStatus.Unhealthy, Helper.Tags.Readiness);

            return services;
        }
        
        public static IServiceCollection WithAkkaPersistenceLivenessService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaPersistenceLivenessService>(Helper.Names.PersistenceLiveness, HealthStatus.Unhealthy, Helper.Tags.PersistenceLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaClusterLivenessService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaClusterLivenessService>(Helper.Names.ClusterLiveness, HealthStatus.Unhealthy, Helper.Tags.ClusterLiveness);

            return services;
        }
        
        public static IServiceCollection WithAkkaClusterReadinessService(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AkkaClusterReadinessService>(Helper.Names.ClusterReadiness, HealthStatus.Unhealthy, Helper.Tags.ClusterReadiness);

            return services;
        }
        
        #endregion

        #region WebApplication extension methods

        public static IEndpointConventionBuilder MapAkkaHealthCheckService<T>(
            this IEndpointRouteBuilder builder,
            string prependPath = "/healthz",
            Action<HealthCheckOptions>? optionConfigure = null) where T: IAkkaHealthcheck
        {
            var path = prependPath.SanitizePath();
            var opt = new HealthCheckOptions();
            optionConfigure?.Invoke(opt);

            var type = typeof(T);
            return type switch
            {
                _ when type == typeof(AkkaLivenessService) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.Liveness.ToPath()}", opt.WithPredicate(Helper.Filters.Liveness)),
                _ when type == typeof(AkkaClusterLivenessService) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.ClusterLiveness.ToPath()}", opt.WithPredicate(Helper.Filters.ClusterLiveness)),
                _ when type == typeof(AkkaPersistenceLivenessService) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.PersistenceLiveness.ToPath()}", opt.WithPredicate(Helper.Filters.PersistenceLiveness)),
                _ when type == typeof(AkkaReadinessService) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.Readiness.ToPath()}", opt.WithPredicate(Helper.Filters.Readiness)),
                _ when type == typeof(AkkaClusterReadinessService) =>
                    builder.MapHealthChecks($"{path}/{Helper.Tags.ClusterReadiness.ToPath()}", opt.WithPredicate(Helper.Filters.ClusterReadiness)),
                _ => throw new Exception($"Unknown Akka.HealthCheck ASP.NET service: {type}")
            };
        }
        
        public static IEndpointRouteBuilder MapAkkaHealthCheckRoutes(
            this IEndpointRouteBuilder builder, 
            string prependPath = "/healthz",
            Action<HealthCheckOptions>? optionConfigure = null,
            Action<IEndpointConventionBuilder>? endpointConfigure = null)
        {
            var services = builder.ServiceProvider;
            var hcOpt = services.GetService<IOptions<HealthCheckServiceOptions>>();
            if (hcOpt == null)
                return builder;
            
            var containsLive = false;
            var containsReady = false;
            var regs = hcOpt.Value.Registrations.ToHashSet();
            if (regs.Any(r => r.Name == Helper.Names.Liveness))
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaLivenessService>(prependPath, optionConfigure);
                endpointConfigure?.Invoke(epBuilder);
            }
            
            if (regs.Any(r => r.Name == Helper.Names.ClusterLiveness))
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaClusterLivenessService>(prependPath, optionConfigure);
                endpointConfigure?.Invoke(epBuilder);
            }
            
            if (regs.Any(r => r.Name == Helper.Names.PersistenceLiveness))
            {
                containsLive = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaPersistenceLivenessService>(prependPath, optionConfigure);
                endpointConfigure?.Invoke(epBuilder);
            }

            if (regs.Any(r => r.Name == Helper.Names.Readiness))
            {
                containsReady = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaReadinessService>(prependPath, optionConfigure);
                endpointConfigure?.Invoke(epBuilder);
            }

            if (regs.Any(r => r.Name == Helper.Names.ClusterReadiness))
            {
                containsReady = true;
                var epBuilder = builder.MapAkkaHealthCheckService<AkkaClusterReadinessService>(prependPath, optionConfigure);
                endpointConfigure?.Invoke(epBuilder);
            }

            var path = prependPath.SanitizePath();
            var opt = new HealthCheckOptions();
            optionConfigure?.Invoke(opt);
            if (containsLive)
            {
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Live.ToPath()}", opt.WithPredicate(Helper.Filters.AllLiveness));
                endpointConfigure?.Invoke(epBuilder);
            }

            if (containsReady)
            {
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Ready.ToPath()}", opt.WithPredicate(Helper.Filters.AllReadiness));
                endpointConfigure?.Invoke(epBuilder);
            }

            if (containsReady || containsLive)
            {
                var epBuilder = builder.MapHealthChecks($"{path}/{Helper.Tags.Akka.ToPath()}", opt.WithPredicate(Helper.Filters.All));
                endpointConfigure?.Invoke(epBuilder);
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