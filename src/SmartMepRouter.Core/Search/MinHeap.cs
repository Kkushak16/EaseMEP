using System;
using System.Collections.Generic;

namespace SmartMepRouter.Core.Search
{
    /// <summary>
    /// A high-performance generic binary min-heap.
    /// Compatible with netstandard2.0 and net48 where PriorityQueue is unavailable.
    /// Performs zero allocations in Push/Pop once internal capacity is reached.
    /// </summary>
    /// <typeparam name="T">Type of element stored in the heap.</typeparam>
    public class MinHeap<T>
    {
        private T[] _items;
        private readonly IComparer<T> _comparer;

        public int Count { get; private set; }

        public int Capacity => _items.Length;

        public MinHeap(int initialCapacity = 64, IComparer<T>? comparer = null)
        {
            if (initialCapacity < 1)
                initialCapacity = 1;

            _items = new T[initialCapacity];
            _comparer = comparer ?? Comparer<T>.Default;
            Count = 0;
        }

        public void Push(T item)
        {
            if (Count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[Count] = item;
            SiftUp(Count);
            Count++;
        }

        public T Pop()
        {
            if (Count == 0)
                throw new InvalidOperationException("The heap is empty.");

            T root = _items[0];
            Count--;

            if (Count > 0)
            {
                _items[0] = _items[Count];
                _items[Count] = default!;
                SiftDown(0);
            }
            else
            {
                _items[0] = default!;
            }

            return root;
        }

        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("The heap is empty.");

            return _items[0];
        }

        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
        }

        private void SiftUp(int index)
        {
            T item = _items[index];
            while (index > 0)
            {
                int parentIndex = (index - 1) >> 1;
                T parent = _items[parentIndex];

                if (_comparer.Compare(item, parent) >= 0)
                    break;

                _items[index] = parent;
                index = parentIndex;
            }
            _items[index] = item;
        }

        private void SiftDown(int index)
        {
            T item = _items[index];
            int half = Count >> 1;

            while (index < half)
            {
                int childIndex = (index << 1) + 1;
                T child = _items[childIndex];
                int rightIndex = childIndex + 1;

                if (rightIndex < Count && _comparer.Compare(_items[rightIndex], child) < 0)
                {
                    childIndex = rightIndex;
                    child = _items[rightIndex];
                }

                if (_comparer.Compare(item, child) <= 0)
                    break;

                _items[index] = child;
                index = childIndex;
            }
            _items[index] = item;
        }
    }
}
