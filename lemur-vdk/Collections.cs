using System;
using System.Collections.Generic;

namespace Lemur.Types
{

    /// <summary>
    /// A bounds checked list, which returns null when
    /// accessed out of bounds. this is to encourage the use of pattern matching to 
    /// combine length, existence, and type checks
    /// </summary>
    /// <typeparam name="T">The type to be stored</typeparam>
    /// <param name="array">The array to copy</param>
    public class SafeList<T>(IEnumerable<T?> array) : List<T?>(array)
    {
        public static implicit operator T?[](SafeList<T> list) => list.ToArray();
        public static implicit operator SafeList<T>(T?[] array) => new(array);
        public int Length => Count; // should be ambiguous :D
        public new T? this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    return default;
                return base[index];
            }
        }
    }
    public class Deque<T>
    {
        private readonly List<T> items = new();

        public int Count
        {
            get { return items.Count; }
        }

        public void Push(T item)
        {
            items.Insert(0, item);
        }

        public void Enqueue(T item)
        {
            items.Add(item);
        }

        public T Pop()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Deque is empty.");

            T frontItem = items[0];
            items.RemoveAt(0);
            return frontItem;
        }

        public T Dequeue()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Deque is empty.");

            T backItem = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            return backItem;
        }

        public T Peek(int lookahead = 0)
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Deque is empty.");

            if (lookahead >= items.Count)
                throw new ArgumentOutOfRangeException("lookahead", "Lookahead value exceeds deque size.");

            return items[lookahead];
        }

        public T PeekFromBottom(int lookahead = 0)
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Deque is empty.");

            if (lookahead >= items.Count)
                throw new ArgumentOutOfRangeException("lookahead", "Lookahead value exceeds deque size.");

            return items[items.Count - 1 - lookahead];
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
