// -----------------------------------------------------------------------
// <copyright file="HealthCheckSettings.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;

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
            LivenessProbeProvider = ValidateProbeType(healthcheckConfig.GetString("liveness.provider"),
                typeof(DefaultLivenessProvider));

            // readiness probe type checking and setting
            ReadinessProbeProvider = ValidateProbeType(healthcheckConfig.GetString("readiness.provider"),
                typeof(DefaultReadinessProvider));
        }

        /// <summary>
        ///     If <c>true</c>, the probe and healthcheck configurations are invalid
        ///     and this system should not be started.
        ///     If <c>false</c>, then the configuration is valid and the system should
        ///     start correctly.
        /// </summary>
        public bool Misconfigured { get; set; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementation used in this instance
        ///     for liveness probes.
        /// </summary>
        public Type LivenessProbeProvider { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> implementation used in this instance
        ///     for readiness probes.
        /// </summary>
        public Type ReadinessProbeProvider { get; }

        private Type ValidateProbeType(string probeType, Type defaultValue)
        {
            Type livenessType = null;
            if (!string.IsNullOrEmpty(probeType))
                livenessType = Type.GetType(probeType, false);

            if (livenessType == null)
            {
                livenessType = defaultValue;
                Misconfigured = true;
            }

            return livenessType;
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