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

        public float Spase
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
            _targetPosition = targetPosition;
            var scrollPos = Scroller.ScrollPosition;
            var direction = Scroller.Direction;
            var stride = CellStride * direction;
            var positionIndex = Mathf.RoundToInt(scrollPos / stride);
            if (Scroller.IsInertia)
            {
                positionIndex = Mathf.RoundToInt(targetPosition / stride);
                _targetPosition = positionIndex * stride;
            }
            else
            {
                var v = Scroller.Velocity;
                var axis = Scroller.Axis;
                var delta = Scroller.DragDelta;
                if (positionIndex == _positionIndex && Mathf.Abs(v) > 0 &&
                    Mathf.Abs(Vector2.Dot(delta.normalized, new Vector2(axis == 0 ? 1 : 0, axis))) > 0.5f)
                {
                    positionIndex -= Math.Sign(v);
                }
            }

            SetPositionIndex(positionIndex);
        }

        protected override void OnScroll(float delta)
        {
            var stopPos = Scroller.ScrollPosition / CellStride * Scroller.Direction - Math.Sign(delta);
            var positionIndex = delta < 0 ? Mathf.CeilToInt(stopPos) : Mathf.FloorToInt(stopPos);
            SetPositionIndex(positionIndex);
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
                _positionIndex = Mathf.Clamp(_positionIndex, 0, dataCount - 1);
            }

            CellViewPool.ReleaseOutOfRange(start, end);
            var scrollPos = axis == 0
                ? new Vector2(scrollPosition, 0f)
                : new Vector2(0f, scrollPosition);
            Content.localPosition = scrollPos;
            for (var i = start; i <= end; i++)
            {
                var cellPos = i * CellStride * -direction;
                var needReposition = false;
                if (!CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    cell = CellViewPool.Get(i, Content);
                    var go = cell.gameObject;
                    go.SetActive(true);
                    DataSource.SetData(i, go);
                    needReposition = true;
                }

                if (isResized || needReposition)
                {
                    cell.anchoredPosition = axis == 0 ? new Vector2(cellPos, 0f) : new Vector2(0f, cellPos);
                }

                OnReposition?.Invoke(cell, i, Mathf.Abs(cellPos + scrollPosition) / ViewportHalf);
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

        public int GetDataIndex(int index)
        {
            if (DataSource == null) return -1;
            var count = DataSource.GetDataCount();
            if (count == 0) return 0;
            return (index % count + count) % count;
        }

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
            UpdateIndicator(GetDataIndex(index));
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