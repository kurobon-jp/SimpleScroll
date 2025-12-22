using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleScroll
{
    public class LazyLayoutListScroll : BaseScroll<ILazyLayoutDataSource>
    {
        [SerializeField] private float _defaultCellSize = 100f;
        [SerializeField] private float _space;
        [SerializeField] private ContentPadding _contentPadding;

        private float _targetPosition;
        private int _targetIndex = -1;
        private float _targetAnchor;
        private bool _targetSmooth;
        private float _normalizedPosition = float.NaN;

        public ContentPadding ContentPadding
        {
            get => _contentPadding;
            set
            {
                _contentPadding = value;
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

        public Range VisibleRange { get; private set; } = new(-1);

        protected override float GetScrollSize()
        {
            var totalSize = DataSource.GetTotalSize(_contentPadding, _defaultCellSize, _space);
            return Mathf.Max(0f, totalSize - ViewportSize);
        }

        protected override void OnDrag(float targetPosition)
        {
            _targetPosition = targetPosition;
            _targetIndex = -1;
        }

        protected override void OnScroll(float delta)
        {
            _targetPosition = Scroller.ScrollPosition + delta * 100f * -Scroller.Direction;
            _targetIndex = -1;
        }

        protected override void OnStopScroll(float velocity)
        {
        }

        protected override void Reposition(float scrollDelta, bool isResized)
        {
            var dataCount = DataSource.GetDataCount();
            if (dataCount == 0)
            {
                CellViewPool.ReleaseAll();
                VisibleRange = new Range(-1);
                return;
            }

            var axis = Scroller.Axis;
            var direction = Scroller.Direction;
            var scrollPosition = Scroller.ScrollPosition;

            scrollPosition *= direction;

            // 表示範囲を求める
            var start = DataSource.FindStartIndex(scrollPosition);
            var end = start;
            var offset = DataSource.GetCellViewOffset(start);
            var viewportSize = ViewportSize + scrollPosition - offset;
            for (var i = start; i < dataCount; i++)
            {
                viewportSize -= DataSource.GetCellViewSize(i) + _space;
                end = i;
                if (viewportSize <= 0) break;
            }

            CellViewPool.ReleaseOutOfRange(start, end);
            var sizeDelta = 0f;
            var fillSize = offset - ClampPosition(scrollPosition);
            for (var i = start; i <= end; i++)
            {
                var needRebuild = false;
                if (CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    needRebuild |= isResized;
                }
                else
                {
                    cell = CellViewPool.Get(i, Content);
                    cell.SetPivot(0.5f, axis);
                    var go = cell.gameObject;
                    go.SetActive(true);
                    DataSource.SetData(i, go);
                    needRebuild = true;
                }

                if (needRebuild)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cell);
                }

                var actualSize = cell.rect.size[axis];
                var size = DataSource.GetCellViewSize(i);
                if (!Mathf.Approximately(size, actualSize))
                {
                    sizeDelta = size - actualSize;
                    size = actualSize;
                    DataSource.SetCellViewSize(i, size);
                    var totalSize = DataSource.GetTotalSize(_contentPadding, _defaultCellSize, _space);
                    Scroller.ScrollSize = Mathf.Max(0, totalSize - ViewportSize);
                }

                offset = DataSource.GetCellViewOffset(i);
                var pos = (offset - ViewportHalf + size * 0.5f) * -direction;
                cell.SetAnchoredPosition(pos, axis);
                fillSize += size + _space;
                if (end != i || !(fillSize < ViewportSize)) continue;
                if (end < dataCount - 1)
                {
                    end++;
                }
                else if (start > 0)
                {
                    start--;
                    i = start - 1;
                }
            }

            VisibleRange = new Range(start, end);
            if (sizeDelta != 0f && Math.Sign(scrollDelta) == -direction)
            {
                sizeDelta *= -direction;
                _targetPosition += sizeDelta;
                Scroller.ScrollPosition += sizeDelta;
            }

            if (isResized)
            {
                _targetPosition = Scroller.ScrollPosition = ClampPosition(Scroller.ScrollPosition);
            }

            if (_targetIndex >= 0)
            {
                var size = DataSource.GetCellViewSize(_targetIndex);
                offset = DataSource.GetCellViewOffset(_targetIndex);
                var targetPosition = (offset - (ViewportSize - size) * _targetAnchor) * direction;
                if (Mathf.Abs(targetPosition - Scroller.ScrollPosition) < 1f)
                {
                    _targetIndex = -1;
                }
                else
                {
                    _targetPosition = targetPosition;
                    if (!_targetSmooth)
                    {
                        Scroller.ScrollPosition = _targetPosition;
                    }
                }
            }

            if (!float.IsNaN(_normalizedPosition))
            {
                OnScrollbarValueChanged(_normalizedPosition);
                _normalizedPosition = float.NaN;
            }

            Content.SetLocalPosition(Scroller.ScrollPosition, axis);
        }

        private void LateUpdate()
        {
            if (Scroller.IsIdling)
            {
                _targetPosition = ClampPosition(_targetPosition);
            }

            UpdatePosition(_targetPosition);
        }

        protected override void OnScrollbarValueChanged(float value)
        {
            base.OnScrollbarValueChanged(value);
            _targetPosition = Scroller.ScrollPosition;
        }

        public void SetNormalizedPosition(float normalizedPosition)
        {
            Refresh(normalizedPosition);
            _normalizedPosition = normalizedPosition;
        }

        public void SetPositionIndex(int index, float anchor = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            Scroller.Stop();
            var dataCount = DataSource.GetDataCount();
            index = Mathf.Clamp(index, 0, dataCount - 1);
            _targetIndex = index;
            _targetAnchor = anchor;
            _targetSmooth = smooth;
        }
    }
}