// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheck.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.HealthCheck.Configuration;
using Akka.HealthCheck.Liveness;
using Akka.HealthCheck.Readiness;
using Akka.HealthCheck.Transports;
using Akka.HealthCheck.Transports.Files;
using Akka.HealthCheck.Transports.Sockets;
using Akka.Util;

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
        internal IActorRef LivenessTransportActor;

        /*
         * Not used in the event that the transport
         * is set to "ProbeTransport.Custom"
         */
        internal IActorRef ReadinessTransportActor;

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
            LivenessTransportActor = StartTransportActor(Settings.LivenessTransportSettings, system, ProbeKind.Liveness,
                LivenessProbe);

            ReadinessTransportActor = StartTransportActor(Settings.ReadinessTransportSettings, system,
                ProbeKind.Readiness, ReadinessProbe);
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

        public static IActorRef StartTransportActor(ITransportSettings settings, ExtendedActorSystem system,
            ProbeKind probeKind, IActorRef probe)
        {
            if (settings is FileTransportSettings fileTransport)
                switch (probeKind)
                {
                    case ProbeKind.Liveness:
                        return system.ActorOf(
                            Props.Create(
                                () => new LivenessTransportActor(new FileStatusTransport(fileTransport), probe)),
                            "liveness-transport" + ThreadLocalRandom.Current.Next());
                    case ProbeKind.Readiness:
                    default:
                        return system.ActorOf(
                            Props.Create(
                                () => new ReadinessTransportActor(new FileStatusTransport(fileTransport), probe)),
                            "readiness-transport" + ThreadLocalRandom.Current.Next());
                }

            if (settings is SocketTransportSettings socketTransport)
                switch (probeKind)
                {
                    case ProbeKind.Liveness:
                        return system.ActorOf(
                            Props.Create(
                                () => new LivenessTransportActor(new SocketStatusTransport(socketTransport), probe)),
                            "liveness-transport" + ThreadLocalRandom.Current.Next());
                    case ProbeKind.Readiness:
                    default:
                        return system.ActorOf(
                            Props.Create(
                                () => new ReadinessTransportActor(new SocketStatusTransport(socketTransport), probe)),
                            "readiness-transport" + ThreadLocalRandom.Current.Next());
                }

            // means that we don't have an automatic transport setup
            return ActorRefs.Nobody;
        }

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