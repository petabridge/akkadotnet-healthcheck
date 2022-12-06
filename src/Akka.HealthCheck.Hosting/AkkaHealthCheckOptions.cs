// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheckSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Text;
using Akka.Configuration;

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
        public Type? Provider { get; }
        public HealthCheckTransport? Transport { get; set; }
        public string? FilePath { get; set; }
        public int? TcpPort { get; set; }

        public ProviderOptions()
        {
            Provider = null;
        }

        private ProviderOptions(Type providerType)
        {
            Provider = providerType;
        }

        public ProviderOptions WithProvider<T>() where T : IProbeProvider
            => new ProviderOptions(typeof(T))
            {
                Transport = Transport,
                FilePath = FilePath,
                TcpPort = TcpPort
            };

        internal StringBuilder? GetStringBuilder()
        {
            var sb = new StringBuilder();
            if (Provider is { })
                sb.AppendLine($"provider = {Provider.AssemblyQualifiedName}");
            if (Transport is { })
                sb.AppendLine($"transport = {Transport.ToString().ToLower()}");
            if (FilePath is { })
                sb.AppendLine($"file.path = {FilePath}");
            if (TcpPort is { })
                sb.AppendLine($"tcp.port = {TcpPort}");

            return sb.Length > 0 ? sb : null;
        }
    }
}