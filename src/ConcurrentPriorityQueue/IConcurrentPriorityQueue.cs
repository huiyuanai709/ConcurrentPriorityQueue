using System.Diagnostics.CodeAnalysis;

namespace ConcurrentPriorityQueue;

public interface IConcurrentPriorityQueue<TElement, TPriority>
{
    void Enqueue(TElement element, TPriority priority, bool instant = false);

    void EnqueueRange(IEnumerable<TElement> elements, TPriority priority, bool instant = false);

    void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items, bool instant = false);

    bool TryPeek(Predicate<TPriority> condition, [MaybeNullWhen(false)] out TElement element);

    bool TryDequeue([MaybeNullWhen(false)] out TElement element);
}