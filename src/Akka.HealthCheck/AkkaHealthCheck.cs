using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.HealthCheck.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck
{
    /// <summary>
    /// INTERNAL API.
    /// </summary>
    public sealed class AkkaHealthCheckExt : ExtensionIdProvider<AkkaHealthCheck>
    {
        public override AkkaHealthCheck CreateExtension(ExtendedActorSystem system)
        {
            return new AkkaHealthCheck(new HealthCheckSettings(system), system);
        }
    }

    /// <summary>
    /// The <see cref="ActorSystem"/> extension used to access the liveness
    /// and readiness probes for the current ActorSystem.
    /// </summary>
    public sealed class AkkaHealthCheck : IExtension
    {
        public AkkaHealthCheck(HealthCheckSettings settings, ExtendedActorSystem system)
        {
            Settings = settings;

            if (!settings.Misconfigured)
            {
                LivenessProvider = TryCreateProvider(settings.LivenessProbeProvider, system);
                ReadinessProvider = TryCreateProvider(settings.ReadinessProbeProvider, system);
            }
            else // if we are misconfigured
            {
                LivenessProvider = new MisconfiguredLivenessProvider(system);
                ReadinessProvider = new MisconfiguredReadinessProvider(system);
            }
            
            // start the probes
            LivenessProbe = system.SystemActorOf(LivenessProvider.ProbeProps, "healthcheck-live");
            ReadinessProbe = system.SystemActorOf(ReadinessProvider.ProbeProps, "healthcheck-readiness");
        }

        internal static IProbeProvider TryCreateProvider(Type providerType, ActorSystem system)
        {
            return (IProbeProvider)Activator.CreateInstance(providerType, system);
        }

        /// <summary>
        /// The healthcheck settings.
        /// </summary>
        public HealthCheckSettings Settings { get; }

        /// <summary>
        /// The <see cref="IProbeProvider"/> used to create <see cref="LivenessProbe"/>.
        /// </summary>
        public IProbeProvider LivenessProvider { get; }

        /// <summary>
        /// The running instance of the liveness probe actor. 
        /// 
        /// Can be queried via a <see cref="GetCurrentLiveness"/> message, 
        /// or you can subscribe to changes in liveness via <see cref="SubscribeToLiveness"/>
        /// and unsubscribe via <see cref="UnsubscribeFromLiveness"/>.
        /// </summary>
        public IActorRef LivenessProbe { get; }

        /// <summary>
        /// The <see cref="IProbeProvider"/> used to create <see cref="ReadinessProbe"/>.
        /// </summary>
        public IProbeProvider ReadinessProvider { get; }

        /// <summary>
        /// The running instance of the readiness probe actor. 
        /// 
        /// Can be queried via a <see cref="GetCurrentReadiness"/> message, 
        /// or you can subscribe to changes in liveness via <see cref="SubscribeToReadiness"/>
        /// and unsubscribe via <see cref="UnsubscribeFromReadiness"/>.
        /// </summary>
        public IActorRef ReadinessProbe { get; }
    }
}
