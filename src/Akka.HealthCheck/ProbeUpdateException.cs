// -----------------------------------------------------------------------
// <copyright file="ProbeUpdateException.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using Akka.HealthCheck.Transports;

namespace Akka.HealthCheck
{
    /// <summary>
    ///     Thrown when a probe fails to update its underlying <see cref="IStatusTransport" />
    /// </summary>
    public sealed class ProbeUpdateException : Exception
    {
        public ProbeUpdateException(ProbeKind probeKind, string message, Exception? innerException)
            : base(message, innerException)
        {
            ProbeKind = probeKind;
        }

        public ProbeKind ProbeKind { get; }
    }
}