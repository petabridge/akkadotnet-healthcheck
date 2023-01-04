// -----------------------------------------------------------------------
// <copyright file="StopTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2023 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

namespace Akka.HealthCheck.Transports;

/// <summary>
/// Used to signal the transport actor to stop itself, used to gracefully stop the actor.
/// </summary>
internal sealed class StopTransport
{
    public static readonly StopTransport Instance = new();
    
    private StopTransport()
    {
    }
}