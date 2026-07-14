using UnityEngine;

namespace SimpleScroll
{
    public class FixedListScroll : BaseScroll<IDataSource>
    {
        [SerializeField] private float _cellSize = 100f;
        [SerializeField] private float _space;
        [SerializeField] private ContentPadding _contentPadding;

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

        public float Space
        {
            get => _space;
            set
            {
                _space = value;
                SetDirty();
            }
        }

        public ContentPadding ContentPadding
        {
            get => _contentPadding;
            set
            {
                _contentPadding = value;
                SetDirty();
            }
        }

        protected override float GetScrollSize()
        {
            var dataCount = DataSource.GetDataCount();
            return Mathf.Max(0f, CellStride * dataCount - ViewportSize - _space + _contentPadding.Size);
        }

        protected override void OnDrag(float targetPosition)
        {
            _targetPosition = targetPosition;
        }

        protected override void OnScroll(float scrollDelta)
        {
            _targetPosition = Scroller.ScrollPosition + scrollDelta * Scroller.ScrollSensitivity;
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
            var direction = Scroller.Direction;
            var stride = CellStride;
            var padding = _contentPadding.Start;
            var scrollPosition = Scroller.ScrollPosition;
            Content.SetLocalPosition(scrollPosition, axis);
            scrollPosition += padding * -direction;
            var start = Mathf.Max(0, Mathf.FloorToInt(scrollPosition * direction / stride));
            var end = Mathf.Clamp(Mathf.FloorToInt((scrollPosition * direction + ViewportSize) / stride), start,
                dataCount - 1);
            CellViewPool.ReleaseOutOfRange(start, end);
            visibleRange = new Range(start, end);
            for (var i = start; i <= end; i++)
            {
                if (CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    if (!isResized) continue;
                }
                else
                {
                    cell = CellViewPool.Get(i, Content);
                    var go = cell.gameObject;
                    go.SetActive(true);
                    DataSource.SetData(i, go);
                }

                var pos = (i * CellStride - ViewportHalf + _cellSize * 0.5f + padding) * -direction;
                cell.SetCellPosition(pos, axis);
            }

            return visibleRange;
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

        public void SetPositionIndex(int positionIndex, float anchor = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            var direction = Scroller.Direction;
            positionIndex = Mathf.Clamp(positionIndex, 0, dataCount - 1);
            var offset = (ViewportSize - CellStride) * (Mathf.Clamp01(anchor) * direction + 0.5f);
            var position = CellStride * positionIndex * direction + ViewportHalf - CellStride * 0.5f - offset;
            _targetPosition = ClampPosition(position);
            if (!smooth)
            {
                Scroller.ScrollPosition = _targetPosition;
            }
            else
            {
                Scroller.OnSnap();
            }
        }
    }
}