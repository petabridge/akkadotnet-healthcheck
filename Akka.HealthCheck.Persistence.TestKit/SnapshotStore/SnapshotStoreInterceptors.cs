//-----------------------------------------------------------------------
// <copyright file="SnapshotStoreInterceptors.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2023 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2023 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Persistence;

namespace Akka.HealthCheck.Persistence.TestKit.SnapshotStore;

public static class SnapshotStoreInterceptors
{
    public class Noop : ISnapshotStoreInterceptor
    {
        public static readonly ISnapshotStoreInterceptor Instance = new Noop();

        public Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria) => Task.FromResult(true);
    }

    public class Failure : ISnapshotStoreInterceptor
    {
        public static readonly ISnapshotStoreInterceptor Instance = new Failure();

        public Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria) => throw new TestSnapshotStoreFailureException(); 
    }

    public class MultiFailure : ISnapshotStoreInterceptor
    {
        public MultiFailure(int times, ISnapshotStoreInterceptor? next = null)
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
    
    public class Delay : ISnapshotStoreInterceptor
    {
        public Delay(TimeSpan delay, ISnapshotStoreInterceptor next)
        {
            _delay = delay;
            _next = next;
        }

        private readonly TimeSpan _delay;
        private readonly ISnapshotStoreInterceptor _next;

        public async Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            await Task.Delay(_delay);
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

    public sealed class OnCondition : ISnapshotStoreInterceptor
    {
        public OnCondition(Func<string, SnapshotSelectionCriteria, Task<bool>> predicate, ISnapshotStoreInterceptor next, bool negate = false)
        {
            _predicate = predicate;
            _next = next;
            _negate = negate;
        }

        public OnCondition(Func<string, SnapshotSelectionCriteria, bool> predicate, ISnapshotStoreInterceptor next, bool negate = false)
        {
            _predicate = (persistenceId, criteria) => Task.FromResult(predicate(persistenceId, criteria));
            _next = next;
            _negate = negate;
        }

        private readonly Func<string, SnapshotSelectionCriteria, Task<bool>> _predicate;
        private readonly ISnapshotStoreInterceptor _next;
        private readonly bool _negate;

        public async Task InterceptAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            var result = await _predicate(persistenceId, criteria);
            if ((_negate && !result) || (!_negate && result))
            {
                await _next.InterceptAsync(persistenceId, criteria);
            }
        }
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

}