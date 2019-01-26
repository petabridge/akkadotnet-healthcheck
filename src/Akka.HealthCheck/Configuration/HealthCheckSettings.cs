using Akka.Configuration;

namespace Akka.HealthCheck.Configuration
{
   /// <summary>
   /// All of the settings used by the Akka.HealthCheck extension.
   /// </summary>
    public sealed class HealthCheckSettings
    {
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
