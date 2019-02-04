// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheck.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Files;
using Akka.HealthCheck.Transports.Sockets;

namespace Akka.HealthCheck
{
    /// <summary>
    ///     INTERNAL API.
    /// </summary>
    public sealed class AkkaHealthCheckExt : ExtensionIdProvider<AkkaHealthCheck>
    {
        public override AkkaHealthCheck CreateExtension(ExtendedActorSystem system)
        {
            return new AkkaHealthCheck(new HealthCheckSettings(system), system);
        }
    }

    /// <summary>
    ///     The <see cref="ActorSystem" /> extension used to access the liveness
    ///     and readiness probes for the current ActorSystem.
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

            // Need to set up transports (possibly)
        }

       

        /// <summary>
        ///     The healthcheck settings.
        /// </summary>
        public HealthCheckSettings Settings { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> used to create <see cref="LivenessProbe" />.
        /// </summary>
        public IProbeProvider LivenessProvider { get; }

        /// <summary>
        ///     The running instance of the liveness probe actor.
        ///     Can be queried via a <see cref="GetCurrentLiveness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToLiveness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromLiveness" />.
        /// </summary>
        public IActorRef LivenessProbe { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> used to create <see cref="ReadinessProbe" />.
        /// </summary>
        public IProbeProvider ReadinessProvider { get; }

        /// <summary>
        ///     The running instance of the readiness probe actor.
        ///     Can be queried via a <see cref="GetCurrentReadiness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToReadiness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromReadiness" />.
        /// </summary>
        public IActorRef ReadinessProbe { get; }

        /*
         * Not used in the event that the transport
         * is set to "ProbeTransport.Custom"
         */
        internal IActorRef ReadinessTransportActor;
        internal IActorRef LivenessTransportActor;

        internal static IProbeProvider TryCreateProvider(Type providerType, ActorSystem system)
        {
            return (IProbeProvider) Activator.CreateInstance(providerType, system);
        }

        /// <summary>
        ///     Gets the current <see cref="AkkaHealthCheck" /> instance registered with the <see cref="ActorSystem" />.
        /// </summary>
        /// <param name="system">The actor system.</param>
        /// <returns>The current <see cref="AkkaHealthCheck" /> instance.</returns>
        public static AkkaHealthCheck For(ActorSystem system)
        {
            return system.WithExtension<AkkaHealthCheck, AkkaHealthCheckExt>();
        }
    }
}