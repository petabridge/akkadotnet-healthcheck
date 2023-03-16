using Akka.Actor;
using Akka.HealthCheck.Readiness;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Cluster.Tests
{
    public class ReadinessStatusClusterUpSpecs : TestKit.Xunit.TestKit
    {
        public ReadinessStatusClusterUpSpecs(ITestOutputHelper helper)
                    : base(Config, "ClusterServer", output: helper)
        {
        }
        private const string Config = @"
        akka {   
   
    actor {
       
        provider = cluster
}
    remote {
        dot-netty.tcp {
            hostname = ""127.0.0.1""
            port = 3000
        }
    }
    cluster {
        seed-nodes = [""akka.tcp://ClusterServer@127.0.0.1:3000""]
    }  
}
";

        public Akka.Cluster.Cluster Cluster => Akka.Cluster.Cluster.Get(Sys);


        [Fact(DisplayName = "ReadinessStatusCluster should tell subscribers that it is up once it becomes available")]
        public void ReadinessStatusCluster_Should_Tell_Subscribers_When_It_Becomes_Available()
        {
            // step1 - verify joining of cluster
            Cluster.RegisterOnMemberUp(() => TestActor.Tell("ready"));
            ExpectMsg("ready");

            // step2 - create probe

            var probe = Sys.ActorOf(Props.Create(() => new ClusterReadinessProbe(true)));
            probe.Tell(new SubscribeToReadiness(TestActor));

            // step3 - wait for ready status
            FishForMessage<ReadinessStatus>(r => r.IsReady);
        }

        [Fact(DisplayName = "ReadinessStatusCluster should tell subscribers when it leaves the Cluster")]
        public void ReadinessStatusCluster_Should_Tell_Subscribers_When_It_Leaves_Cluster()
        {
            // step1 - verify joining of cluster
            Cluster.RegisterOnMemberUp(() => TestActor.Tell("ready"));
            ExpectMsg("ready");

            // step2 - create probe

            var probe = Sys.ActorOf(Props.Create(() => new ClusterReadinessProbe(true)));
            probe.Tell(new SubscribeToReadiness(TestActor));

            // step3 - wait for ready status
            FishForMessage<ReadinessStatus>(r => r.IsReady);

            Cluster.Leave(Cluster.SelfAddress);
            FishForMessage<ReadinessStatus>(r => !r.IsReady);


        }

    }


}
