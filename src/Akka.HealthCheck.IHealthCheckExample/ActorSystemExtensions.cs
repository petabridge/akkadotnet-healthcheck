using Akka.Actor;
using Akka.Hosting;

namespace Akka.HealthCheck.IHealthCheckExample;

public static class ActorSystemExtensions
{
    public static void CreateAndRegisterHealthProbe<T>(this ActorSystem actorSystem, IActorRegistry registry, IProbeProvider probeProvider)
    {
        var props = probeProvider.ProbeProps;
        var name = typeof(T).Name;
        var actor = actorSystem.ActorOf(props, name);
        registry.Register<T>(actor);
    }
}
