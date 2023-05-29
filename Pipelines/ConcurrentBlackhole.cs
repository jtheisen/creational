using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Pipelines.Internals;

public class ConcurrentBlackhole<T> : IProducerConsumerCollection<T>
{
    public Int32 Count => 0;

    public Boolean IsSynchronized => true;
    public Object SyncRoot => this;

    public void CopyTo(T[] array, Int32 index) { }
    public void CopyTo(Array array, Int32 index) { }

    public T[] ToArray() => new T[0];

    public Boolean TryAdd(T item) => true;
    public Boolean TryTake([MaybeNullWhen(false)] out T item)
    {
        item = default;
        return false;
    }

    public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
