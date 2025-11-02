namespace SimpleScroll.Utils
{
    using System.Collections;
    using System.Collections.Generic;

    public class IndexedDictionary<TK, TV> : IEnumerable<TK>
    {
        private readonly List<TK> _keys = new();
        private readonly Dictionary<TK, TV> _values = new();

        public int Count => _keys.Count;

        public TK this[int index] => _keys[index];

        public TV Get(TK key)
        {
            return _values[key];
        }

        public bool TryGetValue(TK key, out TV value)
        {
            return _values.TryGetValue(key, out value);
        }

        public bool Add(TK key, TV value)
        {
            if (!_values.TryAdd(key, value))
                return false;

            _keys.Add(key);
            return true;
        }

        public bool Remove(TK key)
        {
            if (!_values.ContainsKey(key))
                return false;

            _keys.Remove(key);
            _values.Remove(key);
            return true;
        }
        
        public bool Remove(TK key, out TV value)
        {
            if (!_values.TryGetValue(key, out value))
                return false;

            _keys.Remove(key);
            _values.Remove(key);
            return true;
        }

        public bool ContainsKey(TK key) => _values.ContainsKey(key);

        public void Clear()
        {
            _keys.Clear();
            _values.Clear();
        }

        public IEnumerator<TK> GetEnumerator() => _keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}