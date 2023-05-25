namespace Algorithms.DataStructures;

public class Heap<T> where T: IComparable<T>
{
    private readonly List<T> _nodes;

    public Heap(int capacity)
    {
        _nodes = new(capacity);
    }
    public void Add(T value)
    {
        _nodes.Add(value);
        HeapifyUp(_nodes.Count - 1);
    }

    public T PeekTop()
    {
        return _nodes[0];
    }

    public T PopTop()
    {
        var result = PeekTop();
        (_nodes[0], _nodes[^1]) = (_nodes[^1], _nodes[0]);
        _nodes.RemoveAt(_nodes.Count - 1);
        HeapifyDown(0);
        return result;
    }

    public void Remove(T value)
    {
        if (_nodes[^1].Equals(value))
        {
            _nodes.RemoveAt(_nodes.Count - 1);
            return;
        }

        var index = _nodes.IndexOf(value);
        (_nodes[index], _nodes[^1]) = (_nodes[^1], _nodes[index]);
        _nodes.RemoveAt(_nodes.Count - 1);

        var parentIndex = (index - 1) / 2;
        if (_nodes[index].CompareTo(_nodes[parentIndex]) < 0)
        {
            HeapifyUp(index);
        }
        else
        {
            HeapifyDown(index);
        }
    }

    public int Count => _nodes.Count;

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            var parentIndex = (index - 1) / 2;
            if (_nodes[index].CompareTo(_nodes[parentIndex]) < 0)
            {
                
                (_nodes[index], _nodes[parentIndex]) = (_nodes[parentIndex], _nodes[index]);
                index = parentIndex;
            }
            else
                break;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            var leftChildIndex = index * 2 + 1;
            if (leftChildIndex >= _nodes.Count)
                return;

            var bestChildIndex = leftChildIndex;

            var rightChildIndex = index * 2 + 2;
            if (rightChildIndex < _nodes.Count)
                if (_nodes[rightChildIndex].CompareTo(_nodes[leftChildIndex]) < 0)
                    bestChildIndex = rightChildIndex;

            if (_nodes[bestChildIndex].CompareTo(_nodes[index]) < 0)
            {
                (_nodes[bestChildIndex], _nodes[index]) = (_nodes[index], _nodes[bestChildIndex]);
                index = bestChildIndex;
            }
            else
                break;
        }
    }
}