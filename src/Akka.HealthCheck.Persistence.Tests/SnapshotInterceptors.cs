// -----------------------------------------------------------------------
// <copyright file="FailingTestSnapshotStore.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2024 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Persistence;
using Akka.Persistence.TestKit;

namespace Akka.HealthCheck.Persistence.Tests;

public static class SnapshotInterceptors
{
    public class Noop : ISnapshotStoreInterceptor
    {
        public static readonly Noop Instance = new ();

        private Noop()
        {
        }
    
        public Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria) => Task.FromResult(true);
    }

    public class CancelableDelay: ISnapshotStoreInterceptor
    {
        public CancelableDelay(TimeSpan delay, ISnapshotStoreInterceptor next, CancellationToken cancellationToken)
        {
            _delay = delay;
            _next = next;
            _cancellationToken = cancellationToken;
        }

        private readonly TimeSpan _delay;
        private readonly ISnapshotStoreInterceptor _next;
        private readonly CancellationToken _cancellationToken;

        public async Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
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
            await _next.InterceptAsync(persistenceId, criteria);
        }
    }

    public class DelayOnce: ISnapshotStoreInterceptor
    {
        public DelayOnce(TimeSpan delay, ISnapshotStoreInterceptor next)
        {
            _delay = delay;
            _next = next;
        }

        private readonly TimeSpan _delay;
        private readonly ISnapshotStoreInterceptor _next;
        private bool _delayed;

        public async Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            if (!_delayed)
            {
                _delayed = true;
                await Task.Delay(_delay);
            }
            await _next.InterceptAsync(persistenceId, criteria);
        }
    }
    
    
    public class Failure : ISnapshotStoreInterceptor
    {
        public Failure(int times, ISnapshotStoreInterceptor? next = null)
        {
            _times = times;
            _next = next ?? Noop.Instance;
        }

        private readonly int _times;
        private readonly ISnapshotStoreInterceptor _next;
        private int _count;
        
        public Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            if (_count >= _times)
            {
                _next.InterceptAsync(persistenceId, criteria);
                return Task.CompletedTask;
            }

            _count++;
            throw new TestSnapshotStoreFailureException($"Failing snapshot {_count}/{_times}");
        }
    }
}
