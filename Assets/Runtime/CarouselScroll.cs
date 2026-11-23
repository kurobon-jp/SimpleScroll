using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleScroll
{
    public class CarouselScroll : BaseScroll<IDataSource>
    {
        [SerializeField] private float _cellSize = 100f;
        [SerializeField] private float _space = 0f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private Toggle _indicator;

        private readonly List<Toggle> _indicators = new();

        private int _cellCount;
        private int _positionIndex;
        private float _scrollDelta;

        private float CellStride => _cellSize + _space;

        public float CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                SetDirty();
            }
        }

        public float Space
        {
            get => _space;
            set
            {
                _space = value;
                SetDirty();
            }
        }

        public bool Loop
        {
            get => _loop;
            set
            {
                _loop = value;
                SetDirty();
            }
        }

        public event Action<int> OnSelected;
        public event Action<RectTransform, int, float> OnReposition;

        protected override float GetScrollSize()
        {
            var dataCount = DataSource.GetDataCount();
            _cellCount = Mathf.CeilToInt(ViewportSize / CellStride) + 1;
            var scrollSize = float.PositiveInfinity;
            if (!_loop)
            {
                _cellCount = Mathf.Min(_cellCount, dataCount);
                scrollSize = Mathf.Max(0f, CellStride * (dataCount - 1));
            }

            return scrollSize;
        }

        protected override void OnDrag(float targetPosition)
        {
            var scrollPos = Scroller.ScrollPosition;
            var velocity = Scroller.Velocity;
            var positionIndex = Mathf.RoundToInt(ToScrollOffset(scrollPos) / CellStride);
            if (Scroller.IsInertia)
            {
                positionIndex = Mathf.RoundToInt(ToScrollOffset(targetPosition) / CellStride);
            }
            else if (positionIndex == _positionIndex && Mathf.Abs(velocity) >= 10f)
            {
                positionIndex -= Math.Sign(velocity);
            }

            targetPosition = ToScrollPosition(positionIndex * CellStride);
            SetPositionIndex(positionIndex, true, false);
            Scroller.SetTargetPosition(targetPosition);
        }

        protected override void OnScroll(float scrollDelta)
        {
            var direction = Math.Sign(scrollDelta);
            if (direction != Math.Sign(_scrollDelta))
            {
                _scrollDelta = 0f;
            }

            _scrollDelta += scrollDelta * Scroller.ScrollSensitivity * 0.01f;
            SetPositionIndex(_positionIndex - (int)_scrollDelta);
            Scroller.MoveTo(ToScrollPosition(_positionIndex * CellStride), true);
            _scrollDelta %= 1f;
        }

        protected override void OnStopScroll(float velocity)
        {
            var stopPos = ScrollOffset / CellStride;
            var positionIndex = velocity < 0 ? Mathf.CeilToInt(stopPos) : Mathf.FloorToInt(stopPos);
            SetPositionIndex(positionIndex, true, false);
            Scroller.MoveTo(ToScrollPosition(_positionIndex * CellStride), true);
        }

        protected override Range Reposition(float scrollDelta, bool isResized)
        {
            var visibleRange = Range.Empty;
            var dataCount = DataSource.GetDataCount();
            if (dataCount == 0)
            {
                CellViewPool.ReleaseAll();
                UpdateIndicator(0);
                return visibleRange;
            }

            var axis = Scroller.Axis;
            var scrollPosition = Scroller.ScrollPosition;
            var stride = CellStride;
            var offset = ToScrollOffset(scrollPosition - stride * 0.5f);
            var start = Mathf.FloorToInt((offset - ViewportHalf) / stride);
            var end = Mathf.FloorToInt((offset + ViewportHalf) / stride);
            if (!_loop)
            {
                start = Mathf.Max(0, start);
                end = Mathf.Min(end, dataCount - 1);
                if (isResized)
                {
                    SetPositionIndex(_positionIndex, false);
                }
            }

            CellViewPool.ReleaseOutOfRange(start, end);
            visibleRange = new Range(start, end);
            Content.SetLocalPosition(scrollPosition, axis);
            for (var i = start; i <= end; i++)
            {
                var needReposition = false;
                if (!CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    cell = CellViewPool.Get(i, Content);
                    var go = cell.gameObject;
                    go.SetActive(true);
                    DataSource.SetData(i, go);
                    needReposition = true;
                }

                var pos = ToScrollPosition(-i * stride);
                if (isResized || needReposition)
                {
                    cell.SetCellPosition(pos, axis);
                }

                OnReposition?.Invoke(cell, i, (pos + scrollPosition) / ViewportHalf);
            }

            return visibleRange;
        }

        protected override void LateUpdate()
        {
            if (Scroller.IsSnapping)
            {
                Scroller.SetTargetPosition(ToScrollPosition(_positionIndex * CellStride));
            }

            UpdatePosition();
        }

        public void Next() => SetPositionIndex(_positionIndex + 1);
        public void Prev() => SetPositionIndex(_positionIndex - 1);

        /// <summary>Moves to the cell nearest to the current position, independently of stopping.</summary>
        public void SnapToNearest(bool smooth = true)
        {
            if (DataSource == null || DataSource.GetDataCount() == 0) return;
            var index = Mathf.RoundToInt(ScrollOffset / CellStride);
            SetPositionIndex(index, smooth, false);
            MoveToPosition(ToScrollPosition(_positionIndex * CellStride), smooth);
        }

        public void SetPositionIndex(int positionIndex, bool smooth = true)
        {
            SetPositionIndex(positionIndex, smooth, smooth);
        }

        private void SetPositionIndex(int positionIndex, bool smooth, bool snap)
        {
            if (DataSource == null) return;
            var count = DataSource.GetDataCount();
            if (count == 0) return;
            var prev = _positionIndex;
            if (!_loop)
            {
                positionIndex = Mathf.Clamp(positionIndex, 0, count - 1);
            }

            if (smooth && prev == positionIndex) return;
            _positionIndex = positionIndex;
            UpdateIndicator(DataSource.GetDataIndex(positionIndex));
            OnSelected?.Invoke(positionIndex);
            if (!smooth)
            {
                Scroller.MoveTo(ToScrollPosition(positionIndex * CellStride), false);
                Stop();
            }
            else if (snap)
            {
                Scroller.MoveTo(ToScrollPosition(positionIndex * CellStride), true);
            }
        }

        private void UpdateIndicator(int index)
        {
            if (_indicator == null || DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            for (var i = _indicators.Count; i < dataCount; i++)
            {
                var go = Instantiate(_indicator.gameObject, _indicator.transform.parent);
                _indicators.Add(go.GetComponent<Toggle>());
                go.SetActive(false);
            }

            _indicator.gameObject.SetActive(false);
            for (var i = 0; i < _indicators.Count; i++)
            {
                var toggle = _indicators[i];
                toggle.gameObject.SetActive(i < dataCount);
                toggle.isOn = i == index;
            }
        }
    }
}
