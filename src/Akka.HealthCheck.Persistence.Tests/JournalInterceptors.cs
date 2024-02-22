// -----------------------------------------------------------------------
// <copyright file="JournalInterceptors.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2024 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Persistence;
using Akka.Persistence.TestKit;

namespace Akka.HealthCheck.Persistence.Tests;

public static class JournalInterceptors
{
    internal class Noop : IJournalInterceptor
    {
        public static readonly Noop Instance = new ();

        private Noop()
        {}
        
        public Task InterceptAsync(IPersistentRepresentation message) => Task.FromResult(true);
    }

    public class CancelableDelay: IJournalInterceptor
    {
        public CancelableDelay(TimeSpan delay, IJournalInterceptor next, CancellationToken cancellationToken)
        {
            _delay = delay;
            _next = next;
            _cancellationToken = cancellationToken;
        }

        private readonly TimeSpan _delay;
        private readonly IJournalInterceptor _next;
        private readonly CancellationToken _cancellationToken;

        public async Task InterceptAsync(IPersistentRepresentation message)
        {
            try
            {
                await Task.Delay(_delay, _cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // no-op
            }
            catch (TimeoutException)
            {
                // no-op
            }
            await _next.InterceptAsync(message);
        }
    }    

}