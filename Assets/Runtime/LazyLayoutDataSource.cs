using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleScroll
{
    public interface ILazyLayoutDataSource : ISizedDataSource
    {
        float GetTotalContentSize(ContentPadding padding, float defaultSize, float space);
        int FindStartIndex(float position);
        float GetCellViewOffset(int index);
        void SetCellViewSize(int index, float size);
    }

    public abstract class LazyLayoutDataSource<T> : ILazyLayoutDataSource, IEnumerable<T>
    {
        private readonly List<T> _data;
        private readonly List<float> _sizes = new();
        private readonly List<float> _offsets = new();

        public T this[int index]
        {
            get => _data[index];
            set
            {
                ValidateIndex(index);
                _data[index] = value;
                _sizes[index] = float.NaN;
                _offsets[index] = 0f;
            }
        }

        protected LazyLayoutDataSource()
        {
            _data = new List<T>();
        }

        protected LazyLayoutDataSource(ICollection<T> data)
        {
            _data = data?.ToList() ?? new List<T>();
            for (var i = 0; i < _data.Count; i++)
            {
                _sizes.Add(float.NaN);
                _offsets.Add(0f);
            }
        }

        public float GetTotalContentSize(ContentPadding padding, float defaultSize, float space)
        {
            var dataCount = GetDataCount();
            // EnsureInternalListsInitialized(dataCount);
            var offset = padding.Start;
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
            // EnsureInternalListsInitialized(GetDataCount());
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
            ValidateIndex(index);
            return _sizes[index];
        }

        public float GetCellViewOffset(int index)
        {
            ValidateIndex(index);
            return _offsets[index];
        }

        public void SetCellViewSize(int index, float size)
        {
            ValidateIndex(index);
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

        // Helpers
        private void EnsureInternalListsInitialized(int dataCount)
        {
            while (_sizes.Count < dataCount)
                _sizes.Add(float.NaN);
            while (_offsets.Count < dataCount)
                _offsets.Add(0f);
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= _data.Count)
                throw new System.ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range. Count={_data.Count}");
        }
    }
}