using Akka.Actor;
using Akka.Configuration;
using Akka.HealthCheck;
using System;

namespace Akka.HealthCheck.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var hocon = @"{
                akka{
                    healthcheck{
                        log-config-on-start = on
                        log-info = on
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
            actorSystem.WhenTerminated.Wait();
        }
    }
}
