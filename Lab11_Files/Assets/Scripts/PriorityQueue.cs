using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority>
{
    private readonly List<(TElement element, TPriority priority)> items =
        new List<(TElement element, TPriority priority)>();

    private readonly IComparer<TPriority> comparer = Comparer<TPriority>.Default;

    public int Count => items.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        items.Add((element, priority));
    }

    public TElement Dequeue()
    {
        if (items.Count == 0)
            throw new System.InvalidOperationException("The queue is empty.");

        // Find the item with the smallest priority value.
        int bestIndex = 0;
        for (int i = 1; i < items.Count; i++)
        {
            if (comparer.Compare(items[i].priority, items[bestIndex].priority) < 0)
                bestIndex = i;
        }

        TElement element = items[bestIndex].element;
        items.RemoveAt(bestIndex);
        return element;
    }
}
