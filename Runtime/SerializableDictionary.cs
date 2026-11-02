using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        private Dictionary<TKey, TValue> dictionary;

        public void Clear()
        {
            keys.Clear();
            values.Clear();
            dictionary = null;
        }

        public void Add(TKey key, TValue value)
        {
            if (keys.Contains(key))
            {
                int index = keys.IndexOf(key);
                values[index] = value;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
            }

            dictionary = null; // Invalidate cache
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            EnsureDictionaryBuilt();
            return dictionary.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            EnsureDictionaryBuilt();
            return dictionary.ContainsKey(key);
        }

        public int Count => keys.Count;

        private void EnsureDictionaryBuilt()
        {
            if (dictionary != null)
                return;

            dictionary = new Dictionary<TKey, TValue>();
            for (int i = 0; i < keys.Count && i < values.Count; i++)
            {
                if (!dictionary.ContainsKey(keys[i]))
                    dictionary[keys[i]] = values[i];
            }
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            EnsureDictionaryBuilt();
            return new Dictionary<TKey, TValue>(dictionary);
        }
    }
}
