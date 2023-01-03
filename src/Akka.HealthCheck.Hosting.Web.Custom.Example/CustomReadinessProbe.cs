// -----------------------------------------------------------------------
// <copyright file="CustomProbe.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2022 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.HealthCheck.Readiness;

namespace Akka.HealthCheck.Hosting.Web.Custom.Example;

public class CustomReadinessProbe: ReceiveActor, IWithTimers
{
    private readonly string _timerKey = "periodic-timer";
    private readonly string _timerSignal = "do-check";
    
    private ReadinessStatus _readinessStatus;
    private readonly HashSet<IActorRef> _subscribers = new ();

    public CustomReadinessProbe() : this(new ReadinessStatus(false))
    {
    }
    
    public CustomReadinessProbe(ReadinessStatus readinessStatus)
    {
        _readinessStatus = readinessStatus;

        Receive<GetCurrentReadiness>(_ => {
            Sender.Tell(_readinessStatus);
        });

        Receive<SubscribeToReadiness>(s =>
        {
            _subscribers.Add(s.Subscriber);
            Context.Watch(s.Subscriber);
            s.Subscriber.Tell(_readinessStatus);
        });

        Receive<UnsubscribeFromReadiness>(u =>
        {
            _subscribers.Remove(u.Subscriber);
            Context.Unwatch(u.Subscriber);
        });

        Receive<Terminated>(t => {
            _subscribers.Remove(t.ActorRef);
        });

        Receive<string>(
            s => s == "do-check", 
            _ =>
            {
                // TODO: insert probe check here
                _readinessStatus = new ReadinessStatus(true);
            });
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer(_timerKey, _timerSignal, TimeSpan.FromSeconds(1));
    }

    public ITimerScheduler Timers { get; set; } = null!;
}