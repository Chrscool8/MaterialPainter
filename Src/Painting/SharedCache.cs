using System;
using System.Collections.Generic;

namespace MaterialPainter2
{
    /// <summary>Reference-counted cache: the value is created on first acquire and destroyed on last release.</summary>
    public sealed class SharedCache<TKey, TValue> where TValue : class
    {
        private sealed class Entry
        {
            public TValue value;
            public int references;
        }

        private readonly Dictionary<TKey, Entry> entries = new Dictionary<TKey, Entry>();
        private readonly Action<TValue> destroy;

        public SharedCache(Action<TValue> destroy)
        {
            this.destroy = destroy;
        }

        public TValue Acquire(TKey key, Func<TValue> create)
        {
            if (entries.TryGetValue(key, out Entry entry))
            {
                entry.references++;
                return entry.value;
            }

            TValue value = create();
            if (value != null)
                entries[key] = new Entry { value = value, references = 1 };

            return value;
        }

        public void Release(TKey key)
        {
            if (!entries.TryGetValue(key, out Entry entry) || --entry.references > 0)
                return;

            entries.Remove(key);
            destroy(entry.value);
        }

        public void Clear()
        {
            foreach (Entry entry in entries.Values)
                destroy(entry.value);

            entries.Clear();
        }
    }
}
