// -----------------------------------------------------------------------
// <copyright file="RegressionProbeFailureSpec.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.TestKit;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using LogEvent = Akka.Event.LogEvent;

namespace Akka.HealthCheck.Persistence.Tests;

public class RegressionProbeFailureSpec: PersistenceTestKit
{
    public RegressionProbeFailureSpec(ITestOutputHelper output) : base("akka.loglevel = DEBUG", nameof(RegressionProbeFailureSpec), output)
    {
    }
    
    [Fact(DisplayName = "Probe should be performed in proper interval with snapshot recovery failure")]
    public async Task IntervalTest()
    {
        await WithSnapshotLoad(load => load.Fail(), async () =>
        {
            Sys.EventStream.Subscribe(TestActor, typeof(LogEvent));
            var probe = Sys.ActorOf(Props.Create(() =>
                new AkkaPersistenceLivenessProbe(true, 400.Milliseconds(), 3.Seconds())));
            await FishForMessageAsync<LogEvent>(e => e.Message.ToString() is "Recreating persistence probe.");

            var stopwatch = Stopwatch.StartNew();
            // Default circuit breaker max-failures is 10
            foreach (var _ in Enumerable.Range(0, 15))
            {
                stopwatch.Restart();
                await FishForMessageAsync<LogEvent>(e => e.Message.ToString() is "Recreating persistence probe.");
                stopwatch.Stop();
                // In the original issue, suicide probe is being recreated immediately after failure without waiting
                stopwatch.Elapsed.Should().BeGreaterThan(300.Milliseconds());
            }
        });
    }
}