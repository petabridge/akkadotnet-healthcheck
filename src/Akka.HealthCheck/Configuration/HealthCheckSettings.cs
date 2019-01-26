using System;
using Akka.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck.Configuration
{
   /// <summary>
   /// All of the settings used by the Akka.HealthCheck extension.
   /// </summary>
    public sealed class HealthCheckSettings
    {
        public HealthCheckSettings(Config healthcheckConfig)
        {
            // liveness probe type checking and setting
            LivenessProbeProvider = ValidateProbeType(healthcheckConfig.GetString("liveness.provider"), typeof(DefaultLivenessProvider));

            // readiness probe type checking and setting
            ReadinessProbeProvider = ValidateProbeType(healthcheckConfig.GetString("readiness.provider"), typeof(DefaultReadinessProvider));
        }

        private Type ValidateProbeType(string probeType, Type defaultValue)
        {
            Type livenessType = null;
            if (!string.IsNullOrEmpty(probeType))
            {
                livenessType = Type.GetType(probeType, false);
            }

            if (livenessType == null)
            {
                livenessType = defaultValue;
                Misconfigured = true;
            }

            return livenessType;
        }

        /// <summary>
        /// If <c>true</c>, the probe and healthcheck configurations are invalid
        /// and this system should not be started.
        /// 
        /// If <c>false</c>, then the configuration is valid and the system should
        /// start correctly.
        /// </summary>
        public bool Misconfigured { get; set; }

        /// <summary>
        /// The <see cref="IProbeProvider"/> implementation used in this instance
        /// for liveness probes.
        /// </summary>
        public Type LivenessProbeProvider { get; }

        /// <summary>
        /// The <see cref="IProbeProvider"/> implementation used in this instance
        /// for readiness probes.
        /// </summary>
        public Type ReadinessProbeProvider { get; }

        /// <summary>
        /// Used to load the default HOCON <see cref="Config"/> used by Akka.HealthCheck
        /// at startup.
        /// </summary>
        public static Config DefaultConfig()
        {
            return ConfigurationFactory.FromResource<HealthCheckSettings>("Akka.HealthCheck.Configuration.akka.healthcheck.conf");
        }
    }
}
