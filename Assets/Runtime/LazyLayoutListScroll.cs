using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SimpleScroll
{
    public class LazyLayoutListScroll : BaseScroll<ILazyLayoutDataSource>
    {
        [FormerlySerializedAs("_defaultCellSize")]
        [SerializeField] private float _estimatedCellSize = 100f;
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

        protected override float GetScrollSize()
        {
            var contentSize = DataSource.GetTotalContentSize(_contentPadding, _estimatedCellSize, _space);
            return Mathf.Max(0f, contentSize - ViewportSize);
        }

        protected override void OnDrag(float targetPosition)
        {
            _targetPosition = targetPosition;
            _targetIndex = -1;
        }

        protected override void OnScroll(float scrollDelta)
        {
            _targetPosition = Scroller.ScrollPosition + scrollDelta * Scroller.ScrollSensitivity;
            _targetIndex = -1;
        }

        protected override void OnStopScroll(float velocity)
        {
        }

        protected override Range Reposition(float scrollDelta, bool isResized)
        {
            var visibleRange = Range.Empty;
            var dataCount = DataSource.GetDataCount();
            if (dataCount == 0)
            {
                CellViewPool.ReleaseAll();
                return visibleRange;
            }

            var axis = Scroller.Axis;
            var scrollPosition = ScrollOffset;

            var start = DataSource.FindDataIndex(scrollPosition);
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
                sizeDelta += LayoutCell(i, isResized, axis, 0f);
                if (end != i || end >= dataCount - 1 ||
                    DataSource.GetCellViewOffset(end + 1) >= scrollPosition + ViewportSize) continue;
                end++;
            }

            if (sizeDelta != 0f && ToScrollOffset(scrollDelta) < 0f)
            {
                sizeDelta = ToScrollPosition(-sizeDelta);
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
                var targetPosition = GetAlignedScrollPosition(offset, size, _targetAnchor);
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
                    else
                    {
                        Scroller.OnSnap();
                    }
                }
            }

            scrollPosition = ScrollOffset;
            sizeDelta = 0f;
            if (start > 0 && DataSource.GetCellViewOffset(start) > scrollPosition)
            {
                do
                {
                    sizeDelta = LayoutCell(--start, isResized, axis, sizeDelta, true);
                } while (DataSource.GetCellViewOffset(start) + sizeDelta > scrollPosition);
            }

            var visibleStart = start;
            var visibleEnd = end;
            scrollPosition -= sizeDelta;
            if (sizeDelta != 0f)
            {
                visibleStart = DataSource.FindDataIndex(scrollPosition);
                visibleEnd = DataSource.FindDataIndex(scrollPosition + ViewportSize);
            }

            visibleRange = new Range(visibleStart, visibleEnd);
            Content.SetLocalPosition(Scroller.ScrollPosition, axis);
            _targetPosition -= sizeDelta;
            Scroller.ScrollPosition -= sizeDelta;
            if (!float.IsNaN(_normalizedPosition))
            {
                base.Refresh(_normalizedPosition, false);
                _normalizedPosition = float.NaN;
            }
            
            return visibleRange;
        }

        private float LayoutCell(int index, bool isResized, int axis, float sizeDelta,
            bool hasDeltaOffset = false)
        {
            var needRebuild = false;
            if (CellViewPool.TryGetVisibleCell(index, out var cell))
            {
                needRebuild |= isResized;
            }
            else
            {
                cell = CellViewPool.Get(index, Content);
                var go = cell.gameObject;
                go.SetActive(true);
                DataSource.SetData(index, go);
                needRebuild = true;
            }

            if (needRebuild)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(cell);
            }

            var actualSize = Mathf.Max(0, cell.rect.size[axis]);
            var size = DataSource.GetCellViewSize(index);
            if (!Mathf.Approximately(size, actualSize))
            {
                sizeDelta += size - actualSize;
                size = actualSize;
                DataSource.SetCellViewSize(index, size);
                var contentSize = DataSource.GetTotalContentSize(_contentPadding, _estimatedCellSize, _space);
                Scroller.ScrollSize = Mathf.Max(0, contentSize - ViewportSize);
            }

            var offset = DataSource.GetCellViewOffset(index);
            var pos = GetCellPosition(offset, size);
            if (hasDeltaOffset)
            {
                pos -= sizeDelta;
            }

            cell.SetCellPosition(pos, axis);
            return sizeDelta;
        }

        private void LateUpdate()
        {
            if (Scroller.IsIdling || Scroller.IsSnapping)
            {
                _targetPosition = ClampPosition(_targetPosition);
            }

            UpdatePosition(_targetPosition);
        }

        protected override void OnNormalizePositionChanged(float _)
        {
            _targetPosition = Scroller.ScrollPosition;
        }

        public override void Refresh(float normalizedPosition, bool isRefreshVisibleCells = true)
        {
            base.Refresh(normalizedPosition, isRefreshVisibleCells);
            _normalizedPosition = normalizedPosition;
        }

        public void SetPositionIndex(int positionIndex, float anchor = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            Scroller.Stop();
            var dataCount = DataSource.GetDataCount();
            positionIndex = Mathf.Clamp(positionIndex, 0, dataCount - 1);
            _targetIndex = positionIndex;
            _targetAnchor = anchor;
            _targetSmooth = smooth;
        }
    }
}
