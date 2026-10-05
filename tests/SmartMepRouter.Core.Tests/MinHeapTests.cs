using System;
using System.Collections.Generic;
using SmartMepRouter.Core.Search;
using Xunit;

namespace SmartMepRouter.Core.Tests
{
    public class MinHeapTests
    {
        [Fact]
        public void PushAndPop_ReturnsItemsInAscendingOrder()
        {
            var heap = new MinHeap<int>();
            int[] values = { 15, 3, 22, 1, 9, 8, 4, 1, 0, 99 };

            foreach (int v in values)
            {
                heap.Push(v);
            }

            Assert.Equal(values.Length, heap.Count);

            var sorted = new List<int>();
            while (heap.Count > 0)
            {
                sorted.Add(heap.Pop());
            }

            Array.Sort(values);
            Assert.Equal(values, sorted.ToArray());
        }

        [Fact]
        public void Peek_ReturnsSmallestWithoutRemoving()
        {
            var heap = new MinHeap<int>();
            heap.Push(10);
            heap.Push(2);
            heap.Push(5);

            Assert.Equal(2, heap.Peek());
            Assert.Equal(3, heap.Count);
        }

        [Fact]
        public void Pop_EmptyHeap_ThrowsInvalidOperationException()
        {
            var heap = new MinHeap<int>();
            Assert.Throws<InvalidOperationException>(() => heap.Pop());
        }

        [Fact]
        public void Clear_EmptiesTheHeap()
        {
            var heap = new MinHeap<int>();
            heap.Push(1);
            heap.Push(2);
            heap.Clear();

            Assert.Equal(0, heap.Count);
        }

        [Fact]
        public void CustomComparer_WorksCorrectly()
        {
            // Max-heap using reverse comparer
            var heap = new MinHeap<int>(comparer: Comparer<int>.Create((a, b) => b.CompareTo(a)));
            heap.Push(1);
            heap.Push(10);
            heap.Push(5);

            Assert.Equal(10, heap.Pop());
            Assert.Equal(5, heap.Pop());
            Assert.Equal(1, heap.Pop());
        }
    }
}
