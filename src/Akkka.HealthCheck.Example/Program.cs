using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck;
using System;

namespace Akkka.HealthCheck.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var hocon = @"{
                akka{
                    healthcheck{
                        log-config-on-start = on
                        liveness{
                            transport = tcp
                            tcp.port = 8080}
                        readiness{
                            transport = file
                            file.path = ""snapshot.txt""}
                
                 }}";
            var config = ConfigurationFactory.ParseString(hocon);
            var actorSystem = ActorSystem.Create("Probe", config);
            var healthCheck = AkkaHealthCheck.For(actorSystem);

            //healthCheck.LivenessProbe.Tell(new GetCurrentLiveness);
            actorSystem.WhenTerminated.Wait();
        }
    }
}
