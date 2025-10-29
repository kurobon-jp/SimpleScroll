using System.Collections.Generic;
using UnityEngine;

namespace SimpleScroll.Utils
{
    internal class CellViewPool
    {
        private readonly IndexedDictionary<int, RectTransform> _visibleCells = new();
        private readonly Dictionary<int, int> _visibleCellTypes = new();
        private readonly Dictionary<int, Stack<RectTransform>> _invisibleCells = new();
        private IDataSource _dataSource;

        internal void SetDataSource(IDataSource dataSource)
        {
            ReleaseAll();
            _dataSource = dataSource;
        }

        private bool IsVisible(int index)
        {
            return _visibleCells.ContainsKey(index) &&
                   _visibleCellTypes.TryGetValue(index, out var type) && 
                   type == _dataSource.GetCellViewType(index);
        }

        internal bool TryGetVisibleCell(int index, out RectTransform cellView)
        {
            if (IsVisible(index) && _visibleCells.TryGetValue(index, out cellView)) return true;
            cellView = null;
            return false;
        }

        internal RectTransform Get(int index, Transform parent)
        {
            Release(index);
            var type = _dataSource.GetCellViewType(index);
            RectTransform cellView = null;
            if (_invisibleCells.TryGetValue(type, out var invisibleCells))
            {
                if (invisibleCells.Count > 0)
                {
                    cellView = invisibleCells.Pop();
                }
            }
            else
            {
                invisibleCells = new Stack<RectTransform>();
                _invisibleCells[type] = invisibleCells;
            }

            if (cellView == null)
            {
                var prefab = _dataSource.GetCellView(index);
                var go = Object.Instantiate(prefab, parent, false);
                cellView = go.GetComponent<RectTransform>();
            }

            _visibleCells.Add(index, cellView);
            _visibleCellTypes.Add(index, type);
            return cellView;
        }

        internal void Release(int index)
        {
            if (!_visibleCells.Remove(index, out var cellView) ||
                !_visibleCellTypes.Remove(index, out var type) ||
                !_invisibleCells.TryGetValue(type, out var invisibleCells)) return;
            invisibleCells.Push(cellView);
            cellView.gameObject.SetActive(false);
        }

        internal void ReleaseOutOfRange(int start, int end)
        {
            for (var i = _visibleCells.Count - 1; i >= 0; i--)
            {
                var index = _visibleCells[i];
                if (index >= start && index <= end) continue;
                Release(index);
            }
        }

        internal void ReleaseAll()
        {
            for (var i = _visibleCells.Count - 1; i >= 0; i--)
            {
                Release(_visibleCells[i]);
            }
        }

        internal void Clear()
        {
            foreach (var index in _visibleCells)
            {
                var cellView = _visibleCells.Get(index);
                Object.Destroy(cellView.gameObject);
            }

            foreach (var stack in _invisibleCells.Values)
            {
                foreach (var cellView in stack)
                {
                    Object.Destroy(cellView.gameObject);
                }
            }

            _visibleCells.Clear();
            _visibleCellTypes.Clear();
            _invisibleCells.Clear();
        }
    }
}