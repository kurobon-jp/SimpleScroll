using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleScroll
{
    public interface ILazyLayoutDataSource : ISizedDataSource
    {
        float GetTotalSize(ContentPadding padding, float defaultSize, float space);
        int FindStartIndex(float position);
        float　GetCellViewOffset(int index);
        void SetCellViewSize(int index, float size);
    }

    public abstract class LazyLayoutDataSource<T> : ILazyLayoutDataSource, IEnumerable<T>
    {
        private readonly List<T> _data;
        private readonly List<float> _sizes = new();
        private readonly List<float> _offsets = new();

        public T this[int index] => _data[index];

        protected LazyLayoutDataSource()
        {
            _data = new List<T>();
        }

        protected LazyLayoutDataSource(List<T> data)
        {
            _data = data;
        }

        public float GetTotalSize(ContentPadding padding, float defaultSize, float space)
        {
            var offset = padding.Start;
            var dataCount = GetDataCount();
            for (var i = 0; i < dataCount; i++)
            {
                _offsets[i] = offset;
                var size = _sizes[i];
                if (float.IsNaN(size))
                {
                    _sizes[i] = size = defaultSize;
                }

                offset += size;
                if (i < dataCount - 1)
                    offset += space;
            }

            return offset + padding.End;
        }

        public int FindStartIndex(float position)
        {
            var index = _offsets.BinarySearch(position);
            if (index < 0)
                index = Mathf.Max(~index - 1, 0);
            return index;
        }

        public int GetDataCount()
        {
            return _data.Count;
        }

        public float GetCellViewSize(int index)
        {
            return _sizes[index];
        }

        public float GetCellViewOffset(int index)
        {
            return _offsets[index];
        }

        public void SetCellViewSize(int index, float size)
        {
            _sizes[index] = size;
        }

        public abstract void SetData(int index, GameObject go);

        public abstract GameObject GetCellView(int index);

        public abstract int GetCellViewType(int index);

        public void Add(T data)
        {
            _data.Add(data);
            _sizes.Add(float.NaN);
            _offsets.Add(0f);
        }

        public void AddRange(IEnumerable<T> data)
        {
            var count = _data.Count;
            _data.AddRange(data);
            for (var i = count; i < _data.Count; i++)
            {
                _sizes.Add(float.NaN);
                _offsets.Add(0f);
            }
        }

        public void Insert(int index, T data)
        {
            _data.Insert(index, data);
            _sizes.Insert(index, float.NaN);
            _offsets.Insert(index, 0f);
        }

        public bool Remove(T data)
        {
            var index = _data.IndexOf(data);
            if (index < 0) return false;
            _data.RemoveAt(index);
            _sizes.RemoveAt(index);
            _offsets.RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            _data.RemoveAt(index);
            _sizes.RemoveAt(index);
            _offsets.RemoveAt(index);
        }

        public void Clear()
        {
            _data.Clear();
            _sizes.Clear();
            _offsets.Clear();
        }

        public bool Contains(T data) => _data.Contains(data);

        public IEnumerator<T> GetEnumerator() => _data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}