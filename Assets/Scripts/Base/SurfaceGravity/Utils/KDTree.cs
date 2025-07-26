using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base.SurfaceGravity.Utils
{
    /// <summary>
    /// Simple generic KD-tree for nearest neighbor queries in 3D.
    /// </summary>
    public class KDTree<T>
    {
        private struct HeapItem : IComparable<HeapItem>
        {
            public float Dist;
            public T Item;
            public int CompareTo(HeapItem other) => Dist.CompareTo(other.Dist);
        }

        private class Node
        {
            public T Item;
            public Vector3 Point;
            public Node Left;
            public Node Right;
        }

        private readonly Node _root;
        private readonly Func<T, Vector3> _selector;

        public KDTree(IEnumerable<T> items, Func<T, Vector3> selector)
        {
            _selector = selector;
            var list = new List<T>(items);
            _root = Build(list, 0);
        }

        private Node Build(List<T> items, int depth)
        {
            if (items == null || items.Count == 0)
                return null;
            var axis = depth % 3;
            items.Sort((a, b) => _selector(a)[axis].CompareTo(_selector(b)[axis]));
            var mid = items.Count / 2;

            var node = new Node
            {
                Item = items[mid],
                Point = _selector(items[mid])
            };

            var leftList = items.GetRange(0, mid);
            var rightList = items.GetRange(mid + 1, items.Count - mid - 1);

            node.Left = Build(leftList, depth + 1);
            node.Right = Build(rightList, depth + 1);
            return node;
        }

        /// <summary>
        /// Returns up to k nearest items to the target point.
        /// </summary>
        public List<T> QueryNearest(Vector3 target, int k)
        {
            var heap = new MaxHeap<HeapItem>(k);
            Search(_root, target, k, 0, heap);
            var result = new List<T>(heap.Count);
            foreach (var elem in heap)
                result.Add(elem.Item);
            return result;
        }

        private static void Search(Node node, Vector3 target, int k, int depth, MaxHeap<HeapItem> heap)
        {
            if (node == null)
                return;
            var dist = (node.Point - target).sqrMagnitude;
            heap.Add(new HeapItem { Dist = dist, Item = node.Item });

            var axis = depth % 3;
            var delta = target[axis] - node.Point[axis];
            var first = delta < 0 ? node.Left : node.Right;
            var second = delta < 0 ? node.Right : node.Left;

            Search(first, target, k, depth + 1, heap);

            if (heap.Count < k || delta * delta < heap.Max.Dist)
                Search(second, target, k, depth + 1, heap);
        }
    }

    /// <summary>
    /// Simple fixed-size max-heap for keeping k smallest elements.
    /// </summary>
    internal class MaxHeap<T> : IEnumerable<T> where T : IComparable<T>
    {
        private readonly T[] _data;

        public MaxHeap(int capacity)
        {
            _data = new T[capacity];
            Count = 0;
        }

        public int Count { get; private set; }

        public T Max => Count > 0 ? _data[0] : default;

        public void Add(T value)
        {
            if (Count < _data.Length)
            {
                _data[Count] = value;
                HeapifyUp(Count);
                Count++;
            }
            else if (value.CompareTo(_data[0]) < 0)
            {
                _data[0] = value;
                HeapifyDown(0);
            }
        }

        private void HeapifyUp(int i)
        {
            var parent = (i - 1) / 2;
            while (i > 0 && _data[i].CompareTo(_data[parent]) > 0)
            {
                Swap(i, parent);
                i = parent;
                parent = (i - 1) / 2;
            }
        }

        private void HeapifyDown(int i)
        {
            while (true)
            {
                var left = 2 * i + 1;
                var right = 2 * i + 2;
                var largest = i;
                if (left < Count && _data[left].CompareTo(_data[largest]) > 0)
                    largest = left;
                if (right < Count && _data[right].CompareTo(_data[largest]) > 0)
                    largest = right;
                if (largest == i) break;
                Swap(i, largest);
                i = largest;
            }
        }

        private void Swap(int i, int j)
        {
            (_data[i], _data[j]) = (_data[j], _data[i]);
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return _data[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}