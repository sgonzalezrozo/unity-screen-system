using System.Collections.Generic;

namespace ScreenSystem
{
    public class LoggingDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _innerDict = new();
        private readonly string _contextName;

        public LoggingDictionary(string contextName = "LoggingDictionary")
        {
            _contextName = contextName;
        }

        public int Count => _innerDict.Count;

        public TValue this[TKey key]
        {
            get => _innerDict[key];
            set
            {
                bool isAdd = !_innerDict.ContainsKey(key);
                _innerDict[key] = value;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _innerDict.TryGetValue(key, out value);
        }

        public bool ContainsKey(TKey key)
        {
            return _innerDict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            bool exists = _innerDict.ContainsKey(key);
            if (exists)
            {
                _innerDict.Remove(key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            int count = _innerDict.Count;
            _innerDict.Clear();
        }

        public ICollection<TKey> Keys => _innerDict.Keys;

        public ICollection<TValue> Values => _innerDict.Values;

        public IEnumerable<KeyValuePair<TKey, TValue>> Pairs
        {
            get { return _innerDict; }
        }

        /// <summary>The values, enumerable without allocating.</summary>
        public Dictionary<TKey, TValue>.ValueCollection ValuesView
            => _innerDict.Values;

        public Dictionary<TKey, TValue> ToDictionary()
        {
            return new Dictionary<TKey, TValue>(_innerDict);
        }
    }
}
