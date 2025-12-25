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
            var contentSize = DataSource.GetTotalContentSize(_contentPadding, _defaultCellSize, _space);
            return Mathf.Max(0f, contentSize - ViewportSize);
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
            for (var i = start; i <= end; i++)
            {
                sizeDelta += LayoutCell(i, isResized, axis, direction, 0f);
                if (end != i || end >= dataCount - 1 ||
                    DataSource.GetCellViewOffset(end + 1) >= scrollPosition + ViewportSize) continue;
                end++;
            }

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
                    _targetPosition = ClampPosition(targetPosition);
                    if (!_targetSmooth)
                    {
                        Scroller.ScrollPosition = _targetPosition;
                    }
                }
            }

            scrollPosition = Scroller.ScrollPosition * direction;
            sizeDelta = 0f;
            if (start > 0 && DataSource.GetCellViewOffset(start) > scrollPosition)
            {
                do
                {
                    sizeDelta = LayoutCell(--start, isResized, axis, direction, sizeDelta, true);
                } while (DataSource.GetCellViewOffset(start) + sizeDelta > scrollPosition);
            }

            var visibleStart = start;
            var visibleEnd = end;
            scrollPosition -= sizeDelta;
            if (sizeDelta != 0f)
            {
                visibleStart = DataSource.FindStartIndex(scrollPosition);
                visibleEnd = DataSource.FindStartIndex(scrollPosition + ViewportSize);
            }

            VisibleRange = new Range(visibleStart, visibleEnd);
            if (!float.IsNaN(_normalizedPosition))
            {
                OnScrollbarValueChanged(_normalizedPosition);
                _normalizedPosition = float.NaN;
            }

            Content.SetLocalPosition(Scroller.ScrollPosition, axis);

            _targetPosition -= sizeDelta;
            Scroller.ScrollPosition -= sizeDelta;
        }

        private float LayoutCell(int index, bool isResized, int axis, int direction, float sizeDelta,
            bool isDeltaOffset = false)
        {
            var needRebuild = false;
            if (CellViewPool.TryGetVisibleCell(index, out var cell))
            {
                needRebuild |= isResized;
            }
            else
            {
                cell = CellViewPool.Get(index, Content);
                cell.SetPivot(0.5f, axis);
                cell.SetAnchor(0.5f, axis);
                var go = cell.gameObject;
                go.SetActive(true);
                DataSource.SetData(index, go);
                needRebuild = true;
            }

            if (needRebuild)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cell);
            }

            var actualSize = cell.rect.size[axis];
            var size = DataSource.GetCellViewSize(index);
            if (!Mathf.Approximately(size, actualSize))
            {
                sizeDelta += size - actualSize;
                size = actualSize;
                DataSource.SetCellViewSize(index, size);
                var contentSize = DataSource.GetTotalContentSize(_contentPadding, _defaultCellSize, _space);
                Scroller.ScrollSize = Mathf.Max(0, contentSize - ViewportSize);
            }

            var offset = DataSource.GetCellViewOffset(index);
            var pos = (offset - ViewportHalf + size * 0.5f) * -direction;
            if (isDeltaOffset)
            {
                pos -= sizeDelta;
            }

            cell.SetAnchoredPosition(pos, axis);
            return sizeDelta;
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

        public override void Refresh(float normalizedPosition)
        {
            base.Refresh(normalizedPosition);
            _normalizedPosition = normalizedPosition;
        }

        public void SetScrollPosition(float scrollPosition)
        {
            _targetPosition = Scroller.ScrollPosition = scrollPosition;
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