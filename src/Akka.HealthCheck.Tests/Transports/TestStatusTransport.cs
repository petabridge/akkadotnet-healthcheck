// -----------------------------------------------------------------------
// <copyright file="TestStatusTransport.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.HealthCheck.Transports;

namespace Akka.HealthCheck.Tests.Transports
{
    public class TestStatusTransportSettings : ITransportSettings
    {
        public TestStatusTransportSettings(bool canGo, bool canStop, TimeSpan delayTime)
        {
            CanGo = canGo;
            CanStop = canStop;
            DelayTime = delayTime;
        }

        public bool CanGo { get; set; }

        public bool CanStop { get; set; }

        public TimeSpan DelayTime { get; set; }

        public ProbeTransport TransportType => ProbeTransport.Custom;
        public string StartupMessage => $"CanGo: {CanGo}, CanStop: {CanStop}, DelayTime: {DelayTime}";
    }

    public class TestStatusTransport : IStatusTransport
    {
        public enum TransportCall
        {
            Go,
            Stop
        }

        public TestStatusTransport(TestStatusTransportSettings settings)
        {
            Settings = settings;
            SystemCalls = new List<TransportCall>();
        }

        public TestStatusTransportSettings Settings { get; }

        public List<TransportCall> SystemCalls { get; }

        public async Task<TransportWriteStatus> Go(string statusMessage, CancellationToken token)
        {
            SystemCalls.Add(TransportCall.Go);
            if (Settings.DelayTime > TimeSpan.Zero)
                await Task.Delay(Settings.DelayTime, token);

            token.ThrowIfCancellationRequested();

            return new TransportWriteStatus(Settings.CanGo);
        }

        public async Task<TransportWriteStatus> Stop(string statusMessage, CancellationToken token)
        {
            SystemCalls.Add(TransportCall.Stop);
            if (Settings.DelayTime > TimeSpan.Zero)
                await Task.Delay(Settings.DelayTime, token);

            token.ThrowIfCancellationRequested();

            return new TransportWriteStatus(Settings.CanStop);
        }
    }
}