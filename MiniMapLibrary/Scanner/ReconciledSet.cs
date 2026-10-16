using System;
using System.Collections.Generic;

namespace MiniMapLibrary.Scanner
{
    // Keeps markers alive between registry snapshots: anything not marked between
    // BeginScan and EndScan is released. Removal is O(1) swap-remove.
    public sealed class ReconciledSet<T> where T : class
    {
        private sealed class Entry
        {
            public int Key;
            public int Generation;
            public T Value;
        }

        private readonly Dictionary<int, Entry> entries = new();
        private readonly List<Entry> values = new();
        private int generation;

        public int Count => values.Count;
        public T this[int index] => values[index].Value;
        public int GetKey(int index) => values[index].Key;

        public void BeginScan() => generation = unchecked(generation + 1);

        public bool TryGetAndMark(int key, out T value)
        {
            if (entries.TryGetValue(key, out Entry entry))
            {
                entry.Generation = generation;
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetValue(int key, out T value)
        {
            if (entries.TryGetValue(key, out Entry entry))
            {
                value = entry.Value;
                return true;
            }
            value = null;
            return false;
        }

        public void Add(int key, T value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var entry = new Entry { Key = key, Generation = generation, Value = value };
            entries.Add(key, entry);
            values.Add(entry);
        }

        public void EndScan(Action<T> release)
        {
            for (int i = values.Count - 1; i >= 0; i--)
            {
                if (values[i].Generation != generation)
                    RemoveAt(i, release);
            }
        }

        public void Replace(int key, T value, Action<T> release)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (!entries.TryGetValue(key, out Entry entry))
            {
                Add(key, value);
                return;
            }

            T previous = entry.Value;
            entry.Value = value;
            entry.Generation = generation;
            if (!ReferenceEquals(previous, value))
                release?.Invoke(previous);
        }

        public void RemoveAt(int index, Action<T> release)
        {
            Entry removed = values[index];
            int lastIndex = values.Count - 1;
            values[index] = values[lastIndex];
            values.RemoveAt(lastIndex);
            entries.Remove(removed.Key);
            release?.Invoke(removed.Value);
        }

        public void Clear(Action<T> release)
        {
            for (int i = values.Count - 1; i >= 0; i--)
                release?.Invoke(values[i].Value);

            values.Clear();
            entries.Clear();
        }
    }
}
