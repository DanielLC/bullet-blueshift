using System.Collections.Generic;

public class EmitterHeap
{
    private readonly List<(float time, ScriptVM emitter)> heap = [];
    public int Count => heap.Count;
    public bool IsEmpty => heap.Count == 0;
    public (float time, ScriptVM emitter) Peek() => heap[0];

    public void Push(float time, ScriptVM emitter)
    {
        heap.Add((time, emitter));
        int i = heap.Count - 1;
        HeapifyUp(i);
    }

    public (float time, ScriptVM emitter) Pop()
    {
        var root = heap[0];
        Swap(0, heap.Count - 1);

        heap.RemoveAt(heap.Count - 1);

        if (heap.Count > 0)
            HeapifyDown(0);

        return root;
    }

    public void UpdateTop(float newTime)
    {
        heap[0] = (newTime, heap[0].emitter);
        HeapifyDown(0);
    }

    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[i].time >= heap[parent].time) break;
            Swap(i, parent);
            i = parent;
        }
    }

    private void HeapifyDown(int i)
    {
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < heap.Count && heap[left].time < heap[smallest].time)
                smallest = left;
            if (right < heap.Count && heap[right].time < heap[smallest].time)
                smallest = right;

            if (smallest == i) break;

            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
    }
}
