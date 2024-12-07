using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ConcurrentPriorityQueue;

public class ConcurrentPriorityQueue<TElement, TPriority> : IConcurrentPriorityQueue<TElement, TPriority>
{
    private readonly PriorityQueue<TElement, TPriority> _innerPriorityQueue;
    private readonly ConcurrentQueue<TElement> _instantQueue = new();
    private readonly object _queueLock = new();
    private int _swapCount;

    public ConcurrentPriorityQueue()
    {
        _innerPriorityQueue = new PriorityQueue<TElement, TPriority>();
    }

    public ConcurrentPriorityQueue(int capacity)
    {
        _innerPriorityQueue = new PriorityQueue<TElement, TPriority>(capacity);
    }

    public ConcurrentPriorityQueue(IComparer<TPriority>? comparer)
    {
        _innerPriorityQueue = new PriorityQueue<TElement, TPriority>(comparer);
    }

    public ConcurrentPriorityQueue(int capacity, IComparer<TPriority>? comparer)
    {
        _innerPriorityQueue = new PriorityQueue<TElement, TPriority>(capacity, comparer);
    }

    public void Enqueue(TElement element, TPriority priority, bool instant = false)
    {
        EnqueueCore(() =>
        {
            if (instant)
            {
                _instantQueue.Enqueue(element);
            }
                
            lock (_queueLock)
            {
                _innerPriorityQueue.Enqueue(element, priority);
                return true;
            }
        });
    }


    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority, bool instant = false)
    {
        EnqueueCore(() =>
        {
            if (instant)
            {
                foreach (var element in elements)
                {
                    _instantQueue.Enqueue(element);
                }
                return true;
            }
                
            lock (_queueLock)
            {
                _innerPriorityQueue.EnqueueRange(elements, priority);
                return true;
            }
        });
    }

    public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items, bool instant = false)
    {
        EnqueueCore(() =>
        {
            if (instant)
            {
                foreach (var element in items.Select(x => x.Element))
                {
                    _instantQueue.Enqueue(element);
                }
                return true;
            }
                
            lock (_queueLock)
            {
                _innerPriorityQueue.EnqueueRange(items);
                return true;
            }
        });
    }

    private void EnqueueCore(Func<bool> coreFunc)
    {
        SpinWait.SpinUntil(() =>
        {
            if (Interlocked.CompareExchange(ref _swapCount, 1, 0) != 0)
            {
                return false;
            }

            try
            {
                return coreFunc();
            }
            finally
            {
                Interlocked.Decrement(ref _swapCount);
            }
        });
    }

    public bool TryPeek(Predicate<TPriority> condition, [MaybeNullWhen(false)] out TElement element)
    {
        SpinWait.SpinUntil(() => Volatile.Read(ref _swapCount) == 0);
        if (_instantQueue.TryPeek(out element))
        {
            return true;
        }

        lock (_queueLock)
        {
            return _innerPriorityQueue.TryPeek(out element, out var priority) && condition(priority);
        }
    }

    public bool TryDequeue([MaybeNullWhen(false)] out TElement element)
    {
        SpinWait.SpinUntil(() => Volatile.Read(ref _swapCount) == 0);
        if (_instantQueue.TryDequeue(out element))
        {
            return true;
        }

        lock (_queueLock)
        {
            return _innerPriorityQueue.TryDequeue(out element, out _);
        }
    }
}