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
        private float _targetPosition;
        private float _scrollDelta;
        private float _scrollTime;

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

        public Range VisibleRange { get; private set; } = new(-1);

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
            _targetPosition = targetPosition;
            var scrollPos = Scroller.ScrollPosition;
            var velocity = Scroller.Velocity;
            var direction = Scroller.Direction;
            var stride = CellStride * direction;
            var positionIndex = Mathf.RoundToInt(scrollPos / stride);
            if (Scroller.IsInertia)
            {
                positionIndex = Mathf.RoundToInt(targetPosition / stride);
                _targetPosition = positionIndex * stride;
            }
            else if (positionIndex == _positionIndex && Mathf.Abs(velocity) >= 10f)
            {
                positionIndex -= Math.Sign(velocity);
            }

            SetPositionIndex(positionIndex);
        }

        protected override void OnScroll(float delta)
        {
            _scrollDelta += delta;
            var now = Time.unscaledTime;
            if (now - _scrollTime > 0.1f && _scrollDelta != 0f)
            {
                SetPositionIndex(_positionIndex - Math.Sign(_scrollDelta));
                _scrollTime = now;
                _scrollDelta = 0f;
            }
        }

        protected override void OnStopScroll(float velocity)
        {
            var stopPos = Scroller.ScrollPosition / CellStride * Scroller.Direction;
            var positionIndex = velocity < 0 ? Mathf.CeilToInt(stopPos) : Mathf.FloorToInt(stopPos);
            SetPositionIndex(positionIndex);
        }

        protected override void Reposition(float scrollDelta, bool isResized)
        {
            var dataCount = DataSource.GetDataCount();
            if (dataCount == 0)
            {
                CellViewPool.ReleaseAll();
                VisibleRange = new Range(-1);
                UpdateIndicator(0);
                return;
            }

            var axis = Scroller.Axis;
            var direction = Scroller.Direction;
            var scrollPosition = Scroller.ScrollPosition;
            var start = Mathf.FloorToInt((scrollPosition * direction - ViewportHalf) / CellStride);
            var end = start + _cellCount;
            if (!_loop)
            {
                start = Mathf.Max(0, start);
                end = Mathf.Min(start + _cellCount, dataCount - 1);
                if (isResized)
                {
                    SetPositionIndex(_positionIndex, false);
                }
            }

            CellViewPool.ReleaseOutOfRange(start, end);
            VisibleRange = new Range(start, end);
            Content.SetLocalPosition(scrollPosition, axis);
            for (var i = start; i <= end; i++)
            {
                var needReposition = false;
                if (!CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    cell = CellViewPool.Get(i, Content);
                    cell.SetPivot(0.5f, axis);
                    var go = cell.gameObject;
                    go.SetActive(true);
                    DataSource.SetData(i, go);
                    needReposition = true;
                }

                var pos = i * CellStride * -direction;
                if (isResized || needReposition)
                {
                    cell.SetAnchoredPosition(pos, axis);
                }

                OnReposition?.Invoke(cell, i, Mathf.Abs(pos + scrollPosition) / ViewportHalf);
            }

            if (isResized)
            {
                UpdateIndicator(_positionIndex);
            }
        }

        private void LateUpdate()
        {
            if (Scroller.IsIdling)
            {
                _targetPosition = _positionIndex * CellStride * Scroller.Direction;
            }

            UpdatePosition(_targetPosition);
        }

        public void Next() => SetPositionIndex(_positionIndex + 1);
        public void Prev() => SetPositionIndex(_positionIndex - 1);

        public void SetPositionIndex(int index, bool smooth = true)
        {
            if (DataSource == null) return;
            var count = DataSource.GetDataCount();
            if (count == 0) return;
            var prev = _positionIndex;
            if (!_loop)
            {
                index = Mathf.Clamp(index, 0, count - 1);
            }

            if (smooth && prev == index) return;
            _positionIndex = index;
            UpdateIndicator(DataSource.GetDataIndex(index));
            OnSelected?.Invoke(index);
            if (!smooth)
                Scroller.ScrollPosition = -index * CellStride;
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