using Algorithms;
using Algorithms.DataStructures;

namespace UnitTests;

public class HeapTest
{
    [Test]
    public void MinHeapAddIsValid()
    {
        var items = new[] { 5, 4, 7, 8, 6, 3, 9, 1, 2, };
        var expected = new[] { 5, 4, 4, 4, 4, 3, 3, 1, 1 };
        
        var minHeap = new Heap<int>(9);
        for (var i = 0; i < items.Length; ++i)
        {
            minHeap.Add(items[i]);
            Assert.That(minHeap.PeekTop(), Is.EqualTo(expected[i]));
        }
    }
    
    [Test]
    public void MinHeapPopIsValid()
    {
        var items = new[] { 5, 4, 7, 8, 6, 3, 9, 1, 2, };
        var minHeap = new Heap<int>(9);
        for (var i = 0; i < items.Length; ++i)
            minHeap.Add(items[i]);
        
        var expected = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        for (var i = 0; i < items.Length; ++i)
            Assert.That(minHeap.PopTop(), Is.EqualTo(expected[i]));
    }
}