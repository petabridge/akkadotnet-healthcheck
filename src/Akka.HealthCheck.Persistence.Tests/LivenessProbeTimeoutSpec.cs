// -----------------------------------------------------------------------
// <copyright file="LivenessProbeTimeoutSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2024 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.HealthCheck.Liveness;
using Akka.Persistence.TestKit;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Akka.HealthCheck.Persistence.Tests;

public class LivenessProbeTimeoutSpec: PersistenceTestKit
{
    public LivenessProbeTimeoutSpec(ITestOutputHelper output) : base(nameof(LivenessProbeTimeoutSpec), output)
    {
    }
    
    [Fact(DisplayName = "AkkaPersistenceLivenessProbe should time out if snapshot recovery does not respond")]
    public async Task SnapshotLoadTimeoutTest()
    {
        using var cts = new CancellationTokenSource();
        var delay = new SnapshotInterceptors.CancelableDelay(30.Minutes(), SnapshotInterceptors.Noop.Instance, cts.Token);

        await WithSnapshotLoad(
            save => save.SetInterceptorAsync(delay),
            () => TestTimeout(cts));
    }
    
    [Fact(DisplayName = "AkkaPersistenceLivenessProbe should time out if journal recovery does not respond")]
    public async Task JournalRecoveryTimeoutTest()
    {
        using var cts = new CancellationTokenSource();
        var delay = new JournalInterceptors.CancelableDelay(30.Minutes(), JournalInterceptors.Noop.Instance, cts.Token);

        await WithJournalRecovery(
            save => save.SetInterceptorAsync(delay),
            () => TestTimeout(cts));
    }
    
    private async Task TestTimeout(CancellationTokenSource cts)
    {
        var probeActor = Sys.ActorOf(Props.Create(() => new AkkaPersistenceLivenessProbe(true, 250.Milliseconds(), 500.Milliseconds(), 0)));
        probeActor.Tell(new SubscribeToLiveness(TestActor));
        
        var timeoutStatus = await FishForMessageAsync<PersistenceLivenessStatus>(
            msg => msg.Status is AkkaHealthStatus.Unhealthy, 
            6.Seconds());
        
        timeoutStatus.IsLive.Should().BeFalse();
        timeoutStatus.StatusMessage.Should().StartWith("Timeout while checking persistence liveness.");
        
        cts.Cancel();
        
        await FishForMessageAsync<PersistenceLivenessStatus>(msg => msg.Status is AkkaHealthStatus.Healthy);
    }
}