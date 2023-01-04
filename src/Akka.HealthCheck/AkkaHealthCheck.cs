// -----------------------------------------------------------------------
// <copyright file="AkkaHealthCheck.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
            return new AkkaHealthCheck(system);
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

        public AkkaHealthCheck(ExtendedActorSystem system)
        {
            system.Settings.InjectTopLevelFallback(HealthCheckSettings.DefaultConfig());
            var settings = Settings = new HealthCheckSettings(system);
            
            if (settings.LogConfigOnStart)
            {
                var sb = new StringBuilder()
                    .AppendLine("Liveness Probe Providers:");
                foreach (var kvp in Settings.LivenessProbeProviders)
                {
                    sb.AppendLine($"\t{kvp.Key}: {kvp.Value}");
                }
                system.Log.Info(sb.ToString());
                system.Log.Info("Liveness Transport Type: {0}", Settings.LivenessTransport.ToString());
                system.Log.Info(Settings.LivenessTransportSettings.StartupMessage);
                
                sb.Clear().AppendLine("Readiness Probe Providers:");
                foreach (var kvp in Settings.ReadinessProbeProviders)
                {
                    sb.AppendLine($"\t{kvp.Key}: {kvp.Value}");
                }
                system.Log.Info(sb.ToString());
                system.Log.Info("Readiness Transport Type: {0}", Settings.ReadinessTransport.ToString());
                system.Log.Info(Settings.ReadinessTransportSettings.StartupMessage);
            }

            if (settings.MisconfiguredLiveness.Count == 0)
            {
                if (settings.LogConfigOnStart)
                {
                    system.Log.Info("Liveness settings Correctly Configured");
                }
            }
            else // if we are misconfigured
            {
                if (settings.LogConfigOnStart)
                {
                    system.Log.Info(
                        "Liveness settings misconfigured:\n" +
                        $"{string.Join("\n\t", settings.MisconfiguredLiveness.Select(kvp => $"Invalid liveness provider type {kvp.Value} for key {kvp.Key}"))}");
                }
            }

            if (settings.MisconfiguredReadiness.Count == 0)
            {
                if (settings.LogConfigOnStart)
                {
                    system.Log.Info("Readiness settings Correctly Configured");
                }
            }
            else // if we are misconfigured
            {
                if (settings.LogConfigOnStart)
                {
                    system.Log.Info(
                        "Readiness settings misconfigured:\n" +
                        $"{string.Join("\n\t", settings.MisconfiguredReadiness.Select(kvp => $"Invalid readiness provider type {kvp.Value} for key {kvp.Key}"))}");
                }
            }
            
            // Create liveness providers and start probes
            LivenessProviders = ImmutableDictionary<string, IProbeProvider>.Empty;
            LivenessProbes = ImmutableDictionary<string, IActorRef>.Empty; 
            foreach (var kvp in settings.LivenessProbeProviders)
            {
                // !: Activator.CreateInstance only returns null for Nullable<T> instances
                var provider = settings.MisconfiguredLiveness.ContainsKey(kvp.Key)
                    ? (IProbeProvider) Activator.CreateInstance(typeof(MisconfiguredLivenessProvider), kvp.Key, system)!
                    : (IProbeProvider) Activator.CreateInstance(kvp.Value, system)!;
                LivenessProviders = LivenessProviders.SetItem(kvp.Key, provider);
                LivenessProbes = LivenessProbes.SetItem(
                    kvp.Key,
                    system.SystemActorOf(provider.ProbeProps, $"healthcheck-live-{kvp.Key}"));
            }
            
            // Create readiness providers and start probes
            ReadinessProviders = ImmutableDictionary<string, IProbeProvider>.Empty;
            ReadinessProbes = ImmutableDictionary<string, IActorRef>.Empty;
            foreach (var kvp in settings.ReadinessProbeProviders)
            {
                // !: Activator.CreateInstance only returns null for Nullable<T> instances
                var provider = settings.MisconfiguredReadiness.ContainsKey(kvp.Key)
                    ? (IProbeProvider)Activator.CreateInstance(typeof(MisconfiguredReadinessProvider), kvp.Key, system)!
                    : (IProbeProvider)Activator.CreateInstance(kvp.Value, system)!;
                ReadinessProviders = ReadinessProviders.SetItem(kvp.Key, provider);
                ReadinessProbes = ReadinessProbes.SetItem(
                    kvp.Key,
                    system.SystemActorOf(provider.ProbeProps, $"healthcheck-readiness-{kvp.Key}"));
            }

            // Need to set up transports (possibly)
            LivenessTransportActor = StartTransportActor(Settings.LivenessTransportSettings, system, ProbeKind.Liveness,
                LivenessProbes, Settings.LogInfoEvents);

            ReadinessTransportActor = StartTransportActor(Settings.ReadinessTransportSettings, system,
                ProbeKind.Readiness, ReadinessProbes, Settings.LogInfoEvents);
        }

        /// <summary>
        ///     The healthcheck settings.
        /// </summary>
        public HealthCheckSettings Settings { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider"/>s used to create <see cref="LivenessProbes"/>s.
        /// </summary>
        public ImmutableDictionary<string, IProbeProvider> LivenessProviders { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider" /> used to create the default <see cref="LivenessProbes" />.
        /// </summary>
        public IProbeProvider LivenessProvider => LivenessProviders.ContainsKey("default")
            ? LivenessProviders["default"]
            : LivenessProviders.Values.First();

        /// <summary>
        ///     The running instances of the liveness probe actor.
        ///     Can be queried via a <see cref="GetCurrentLiveness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToLiveness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromLiveness" />.
        /// </summary>
        public ImmutableDictionary<string, IActorRef> LivenessProbes { get; }

        /// <summary>
        ///     The running default instance of the liveness probe actor or the first registered liveness probe.
        ///     Can be queried via a <see cref="GetCurrentLiveness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToLiveness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromLiveness" />.
        /// </summary>
        public IActorRef LivenessProbe =>
            LivenessProbes.ContainsKey("default") ? LivenessProbes["default"] : LivenessProbes.Values.First();

        /// <summary>
        ///     The <see cref="IProbeProvider"/>s used to create <see cref="ReadinessProbes"/>s.
        /// </summary>
        public ImmutableDictionary<string, IProbeProvider> ReadinessProviders { get; }

        /// <summary>
        ///     The <see cref="IProbeProvider"/>s used to create the default <see cref="ReadinessProbes"/>s.
        /// </summary>
        public IProbeProvider ReadinessProvider => ReadinessProviders.ContainsKey("default")
            ? ReadinessProviders["default"]
            : ReadinessProviders.Values.First();
        
        /// <summary>
        ///     The running instances of the readiness probe actor.
        ///     Can be queried via a <see cref="GetCurrentReadiness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToReadiness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromReadiness" />.
        /// </summary>
        public ImmutableDictionary<string, IActorRef> ReadinessProbes { get; }

        /// <summary>
        ///     The running default instance of the liveness probe actor or the first registered liveness probe.
        ///     Can be queried via a <see cref="GetCurrentLiveness" /> message,
        ///     or you can subscribe to changes in liveness via <see cref="SubscribeToLiveness" />
        ///     and unsubscribe via <see cref="UnsubscribeFromLiveness" />.
        /// </summary>
        public IActorRef ReadinessProbe =>
            ReadinessProbes.ContainsKey("default") ? ReadinessProbes["default"] : ReadinessProbes.Values.First();

        public static IActorRef StartTransportActor(ITransportSettings settings, ExtendedActorSystem system,
            ProbeKind probeKind, ImmutableDictionary<string, IActorRef> probes, bool log)
        {
            if (settings is FileTransportSettings fileTransport)
                switch (probeKind)
                {
                    case ProbeKind.Liveness:
                        return system.ActorOf(
                            Props.Create(
                                () => new LivenessTransportActor(new FileStatusTransport(fileTransport), probes, log)),
                            "liveness-transport" + ThreadLocalRandom.Current.Next());
                    case ProbeKind.Readiness:
                    default:
                        return system.ActorOf(
                            Props.Create(
                                () => new ReadinessTransportActor(new FileStatusTransport(fileTransport), probes, log)),
                            "readiness-transport" + ThreadLocalRandom.Current.Next());
                }

            if (settings is SocketTransportSettings socketTransport)
                switch (probeKind)
                {
                    case ProbeKind.Liveness:
                        return system.ActorOf(
                            Props.Create(
                                () => new LivenessTransportActor(new SocketStatusTransport(socketTransport), probes, log)),
                            "liveness-transport" + ThreadLocalRandom.Current.Next());
                    case ProbeKind.Readiness:
                    default:
                        return system.ActorOf(
                            Props.Create(
                                () => new ReadinessTransportActor(new SocketStatusTransport(socketTransport), probes, log)),
                            "readiness-transport" + ThreadLocalRandom.Current.Next());
                }

            // means that we don't have an automatic transport setup
            return ActorRefs.Nobody;
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