// -----------------------------------------------------------------------
// <copyright file="PersistenceLivenessStatusSpecs.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2024 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Persistence.TestKit;
using FluentAssertions;
using Xunit;

namespace Akka.HealthCheck.Persistence.Tests;

public class PersistenceLivenessStatusSpecs
{
    [Fact(DisplayName = "Healthy status should be alive")]
    public void HealthyStatusTest()
    {
        var status = PersistenceLivenessStatus.Healthy("Test");
        status.IsLive.Should().BeTrue();
        status.StatusMessage.Should().Be("Test");
        status.Failure.Should().BeNull();
        status.Status.Should().Be(AkkaHealthStatus.Healthy);
    }

    [Fact(DisplayName = "Degraded status should still be alive")]
    public void DegradedStatusTest()
    {
        var status = PersistenceLivenessStatus.Degraded(null, "Test");
        status.IsLive.Should().BeTrue();
        status.StatusMessage.Should().Be("Test");
        status.Failure.Should().BeNull();
        status.Status.Should().Be(AkkaHealthStatus.Degraded);

        status = PersistenceLivenessStatus.Degraded(new TestJournalFailureException(), "Test");
        status.IsLive.Should().BeTrue();
        status.StatusMessage.Should().Be("Test");
        status.Failure.Should().BeOfType<TestJournalFailureException>();
        status.Status.Should().Be(AkkaHealthStatus.Degraded);
    }

    [Fact(DisplayName = "Unhealthy status should not be alive")]
    public void UnhealthyStatusTest()
    {
        var status = PersistenceLivenessStatus.Unhealthy(null, "Test");
        status.IsLive.Should().BeFalse();
        status.StatusMessage.Should().Be("Test");
        status.Failure.Should().BeNull();
        status.Status.Should().Be(AkkaHealthStatus.Unhealthy);

        status = PersistenceLivenessStatus.Unhealthy(new TestJournalFailureException(), "Test");
        status.IsLive.Should().BeFalse();
        status.StatusMessage.Should().Be("Test");
        status.Failure.Should().BeOfType<TestJournalFailureException>();
        status.Status.Should().Be(AkkaHealthStatus.Unhealthy);
    }
}