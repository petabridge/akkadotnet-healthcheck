// -----------------------------------------------------------------------
// <copyright file="HealthCheckSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Files;
using Akka.HealthCheck.Transports.Sockets;

namespace Akka.HealthCheck.Configuration
{
    /// <summary>
    ///     All of the settings used by the Akka.HealthCheck extension.
    /// </summary>
    public sealed class HealthCheckSettings
    {
        public HealthCheckSettings(ActorSystem system)
            : this(system.Settings.Config.WithFallback(DefaultConfig()).GetConfig("akka.healthcheck"))
        {
        }

        public HealthCheckSettings(Config healthcheckConfig)
        {
            // liveness probe type checking and setting
            MisconfiguredLiveness = ImmutableDictionary<string, string>.Empty;
            LivenessProbeProviders = ImmutableDictionary<string, Type>.Empty;
            var livenessProviderMap = healthcheckConfig.GetConfig("liveness.providers").AsEnumerable();
            foreach (var kvp in livenessProviderMap)
            {
                if (!TryValidateProbeType(kvp.Value.GetString(), out var provider))
                {
                    MisconfiguredLiveness = MisconfiguredLiveness.SetItem(kvp.Key, kvp.Value.GetString()); 
                    LivenessProbeProviders = LivenessProbeProviders.SetItem(kvp.Key, typeof(MisconfiguredLivenessProvider));
                }
                else
                {
                    LivenessProbeProviders = LivenessProbeProviders.SetItem(kvp.Key, provider);
                }
            }

            LivenessTransport = MapToTransport(healthcheckConfig.GetString("liveness.transport"));

            LivenessTransportSettings = PopulateSettings(healthcheckConfig.GetConfig("liveness"), LivenessTransport);

            // readiness probe type checking and setting
            MisconfiguredReadiness = ImmutableDictionary<string, string>.Empty;
            ReadinessProbeProviders = ImmutableDictionary<string, Type>.Empty;
            var readinessProviderMap = healthcheckConfig.GetConfig("readiness.providers").AsEnumerable();
            foreach (var kvp in readinessProviderMap)
            {
                if (!TryValidateProbeType(kvp.Value.GetString(), out var provider))
                {
                    MisconfiguredReadiness = MisconfiguredReadiness.SetItem(kvp.Key, kvp.Value.GetString());
                    ReadinessProbeProviders = ReadinessProbeProviders.SetItem(kvp.Key, typeof(MisconfiguredReadinessProvider));
                }
                else
                {
                    ReadinessProbeProviders = ReadinessProbeProviders.SetItem(kvp.Key, provider);
                }
            }

            ReadinessTransport = MapToTransport(healthcheckConfig.GetString("readiness.transport"));

            ReadinessTransportSettings = PopulateSettings(healthcheckConfig.GetConfig("readiness"), ReadinessTransport);

            LogConfigOnStart = healthcheckConfig.GetBoolean("log-config-on-start");

            LogInfoEvents = healthcheckConfig.GetBoolean("log-info");
        }

        /// <summary>
        ///     Gets a value indicating whether [log configuration on start].
        /// </summary>
        /// <value><c>true</c> if [log configuration on start]; otherwise, <c>false</c>.</value>
        public bool LogConfigOnStart { get; }

        /// <summary>
        ///     Gets a value indicating whether Rediness/Liveness probe logs are turned on.
        /// </summary>
        /// <value><c>true</c> if probe logs on; otherwise, <c>false</c>.</value>
        public bool LogInfoEvents { get; }

        /// <summary>
        ///     If <c>true</c>, the probe and healthcheck configurations are invalid
        ///     and some healthcheck providers are not started.
        ///     If <c>false</c>, then the configuration is valid and all providers should
        ///     start correctly.
        /// </summary>
        public bool Misconfigured => MisconfiguredLiveness.Count != 0 || MisconfiguredReadiness.Count != 0;

        /// <summary>
        ///     If contains entries, the probe and healthcheck configurations are invalid
        ///     and this liveness provider should not be started.
        ///     If empty, then the configuration is valid and the liveness provider should
        ///     start correctly.
        /// </summary>
        public ImmutableDictionary<string, string> MisconfiguredLiveness { get; }

        /// <summary>
        ///     If contains entries, the probe and healthcheck configurations are invalid
        ///     and the readiness provider should not be started.
        ///     If empty, then the configuration is valid and the readiness provider should
        ///     start correctly.
        /// </summary>
        public ImmutableDictionary<string, string> MisconfiguredReadiness { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementations used in this instance
        ///     for liveness probes.
        /// </summary>
        public ImmutableDictionary<string, Type> LivenessProbeProviders { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementation used in this instance
        ///     for liveness probes.
        /// </summary>
        public Type LivenessProbeProvider => LivenessProbeProviders.ContainsKey("default")
            ? LivenessProbeProviders["default"]
            : LivenessProbeProviders.Values.First();

        /// <summary>
        ///     The transportation medium we're going to use
        ///     for signaling liveness data.
        /// </summary>
        public ProbeTransport LivenessTransport { get; }

        /// <summary>
        ///     Liveness transport settings.
        /// </summary>
        public ITransportSettings LivenessTransportSettings { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementation used in this instance
        ///     for readiness probes.
        /// </summary>
        public ImmutableDictionary<string, Type> ReadinessProbeProviders { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementation used in this instance
        ///     for readiness probes.
        /// </summary>
        public Type ReadinessProbeProvider => ReadinessProbeProviders.ContainsKey("default")
            ? ReadinessProbeProviders["default"]
            : ReadinessProbeProviders.Values.First();

        /// <summary>
        ///     The transportation medium we're going to use
        ///     for signaling readiness data.
        /// </summary>
        public ProbeTransport ReadinessTransport { get; }

        /// <summary>
        ///     Readiness transport settings.
        /// </summary>
        public ITransportSettings ReadinessTransportSettings { get; }

        private static bool TryValidateProbeType(string probeType, out Type livenessType)
        {
            livenessType = null;
            if (!string.IsNullOrEmpty(probeType))
                livenessType = Type.GetType(probeType, false);

            return livenessType != null;
        }

        public static ProbeTransport MapToTransport(string transportName)
        {
            switch (transportName.ToLowerInvariant())
            {
                case "tcp":
                    return ProbeTransport.TcpSocket;
                case "file":
                    return ProbeTransport.File;
                default:
                    return ProbeTransport.Custom;
            }
        }

        private static ITransportSettings PopulateSettings(Config config, ProbeTransport transportType)
        {
            switch (transportType)
            {
                case ProbeTransport.File:
                    return new FileTransportSettings(config.GetString("file.path"));
                case ProbeTransport.TcpSocket:
                    return new SocketTransportSettings(config.GetInt("tcp.port"));
                default:
                    return new CustomTransportSettings();
            }
        }

        /// <summary>
        ///     Used to load the default HOCON <see cref="Config" /> used by Akka.HealthCheck
        ///     at startup.
        /// </summary>
        public static Config DefaultConfig()
        {
            return ConfigurationFactory.FromResource<HealthCheckSettings>(
                "Akka.HealthCheck.Configuration.akka.healthcheck.conf");
        }
    }
}