namespace SimpleScroll.Utils
{
    using System.Collections;
    using System.Collections.Generic;

    public class IndexedDictionary<TK, TV> : IEnumerable<TK>
    {
        private readonly List<TK> _keys = new();
        private readonly Dictionary<TK, TV> _dict = new();

        public int Count => _keys.Count;

        public TK this[int index] => _keys[index];

        public TV Get(TK key)
        {
            return _dict[key];
        }

        public bool TryGetValue(TK key, out TV value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public bool Add(TK key, TV value)
        {
            if (!_dict.TryAdd(key, value))
                return false;

            _keys.Add(key);
            return true;
        }

        public bool Remove(TK key)
        {
            if (!_dict.ContainsKey(key))
                return false;

            _keys.Remove(key);
            _dict.Remove(key);
            return true;
        }
        
        public bool Remove(TK key, out TV value)
        {
            if (!_dict.TryGetValue(key, out value))
                return false;

            _keys.Remove(key);
            _dict.Remove(key);
            return true;
        }

        public bool ContainsKey(TK key) => _dict.ContainsKey(key);

        public void Clear()
        {
            _keys.Clear();
            _dict.Clear();
        }

        public IEnumerator<TK> GetEnumerator() => _keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}