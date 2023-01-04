// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheckSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Akka.Configuration;
using Akka.HealthCheck.Cluster;
using Akka.HealthCheck.Persistence;
using Akka.HealthCheck.Readiness;
using Akka.Hosting;

namespace Akka.HealthCheck.Hosting
{
    public enum HealthCheckTransport
    {
        Custom,
        File,
        Tcp
    }
    
    public sealed class AkkaHealthCheckOptions
    {
        public ProviderOptions Liveness { get; } = new ProviderOptions();
        public ProviderOptions Readiness { get; } = new ProviderOptions();
        public bool? LogConfigOnStart { get; set; }
        public bool? LogInfo { get; set; }

        public AkkaHealthCheckOptions AddClusterReadinessProvider()
        {
            Readiness.AddProvider<ClusterReadinessProbeProvider>("cluster");
            return this;
        }

        public AkkaHealthCheckOptions AddClusterLivenessProvider()
        {
            Liveness.AddProvider<ClusterLivenessProbeProvider>("cluster");
            return this;
        }

        public AkkaHealthCheckOptions AddPersistenceLivenessProvider()
        {
            Liveness.AddProvider<AkkaPersistenceLivenessProbeProvider>("persistence");
            return this;
        }
        
        public AkkaHealthCheckOptions AddReadinessProvider<T>(string key) where T : IProbeProvider
        {
            Readiness.AddProvider<T>(key);
            return this;
        }
        
        public AkkaHealthCheckOptions AddLivenessProvider<T>(string key) where T : IProbeProvider
        {
            Liveness.AddProvider<T>(key);
            return this;
        }
        
        internal Config? ToConfig()
        {
            var sb = new StringBuilder();
            var liveness = Liveness.GetStringBuilder();
            if (liveness is { })
            {
                sb.AppendLine("liveness {")
                    .Append(liveness)
                    .AppendLine("}");
            }

            var readiness = Readiness.GetStringBuilder();
            if (readiness is { })
            {
                sb.AppendLine("readiness {")
                    .Append(readiness)
                    .AppendLine("}");
            }

            if (LogInfo is { })
                sb.AppendLine($"log-info = {(LogInfo.Value ? "on" : "off")}");

            if (LogConfigOnStart is { })
                sb.AppendLine($"log-info = {(LogConfigOnStart.Value ? "on" : "off")}");

            if (sb.Length <= 0) 
                return null;
            
            sb.Insert(0, "akka.healthcheck {");
            sb.Append('}');
                
            return (Config)sb.ToString();

        }
    }

    public sealed class ProviderOptions
    {
        public ImmutableDictionary<string, Type> Providers { get; private set; } = ImmutableDictionary<string, Type>.Empty;
            
        public HealthCheckTransport? Transport { get; set; }
        public string? FilePath { get; set; }
        public int? TcpPort { get; set; }

        public ProviderOptions AddProvider<T>(string key) where T : IProbeProvider
        {
            Providers = Providers.SetItem(key, typeof(T));
            return this;
        }

        internal StringBuilder? GetStringBuilder()
        {
            var sb = new StringBuilder();
            if (Providers.Count > 0)
            {
                sb.AppendLine("providers {");
                foreach (var kvp in Providers)
                {
                    sb.AppendLine($"{kvp.Key} = {kvp.Value.AssemblyQualifiedName!.ToHocon()}");
                }
                sb.AppendLine("}");
            }
            
            if (Transport is { })
                sb.AppendLine($"transport = {Transport.ToString()?.ToLower()}");
            if (FilePath is { })
                sb.AppendLine($"file.path = {FilePath}");
            if (TcpPort is { })
                sb.AppendLine($"tcp.port = {TcpPort}");

            return sb.Length > 0 ? sb : null;
        }
    }
}