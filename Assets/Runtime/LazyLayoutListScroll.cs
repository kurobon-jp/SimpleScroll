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
            var sizesChanged = false;
            // Use the measured lengths locally while the cached offsets are still unchanged.
            var nextOffset = offset;
            for (var i = start; i <= end; i++)
            {
                sizeDelta += MeasureCell(i, isResized, axis, ref sizesChanged);
                nextOffset += DataSource.GetCellViewSize(i) + _space;
                if (end != i || end >= dataCount - 1 ||
                    nextOffset >= scrollPosition + ViewportSize) continue;
                end++;
            }

            if (sizesChanged)
            {
                Scroller.ScrollSize = GetScrollSize();
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
            sizesChanged = false;
            if (start > 0 && DataSource.GetCellViewOffset(start) > scrollPosition)
            {
                do
                {
                    sizeDelta += MeasureCell(--start, isResized, axis, ref sizesChanged);
                } while (start > 0 && DataSource.GetCellViewOffset(start) + sizeDelta > scrollPosition);
            }

            if (sizesChanged)
            {
                Scroller.ScrollSize = GetScrollSize();
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
            // Measuring preceding cells moves the existing cells' offsets; move the content
            // by the same amount to keep their screen positions unchanged on either axis.
            var positionDelta = ToScrollPosition(sizeDelta);
            _targetPosition -= positionDelta;
            Scroller.ScrollPosition -= positionDelta;
            if (!float.IsNaN(_normalizedPosition))
            {
                base.Refresh(_normalizedPosition, false);
                _normalizedPosition = float.NaN;
            }

            // All offsets are now current; placement does not trigger measurement or recalculation.
            for (var i = start; i <= end; i++)
            {
                PositionCell(i, axis);
            }
            Content.SetLocalPosition(Scroller.ScrollPosition, axis);
            
            return visibleRange;
        }

        private float MeasureCell(int index, bool isResized, int axis, ref bool sizesChanged)
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
            if (Mathf.Approximately(size, actualSize)) return 0f;
            DataSource.SetCellViewSize(index, actualSize);
            sizesChanged = true;
            return size - actualSize;
        }

        private void PositionCell(int index, int axis)
        {
            if (!CellViewPool.TryGetVisibleCell(index, out var cell)) return;
            var offset = DataSource.GetCellViewOffset(index);
            var size = DataSource.GetCellViewSize(index);
            var pos = GetCellPosition(offset, size);
            cell.SetCellPosition(pos, axis);
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
