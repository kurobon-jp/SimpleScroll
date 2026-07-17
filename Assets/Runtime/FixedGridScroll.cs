using UnityEngine;

namespace SimpleScroll
{
    public class FixedGridScroll : BaseScroll<IDataSource>
    {
        [SerializeField] private Vector2 _cellSize = new(100f, 100f);
        [SerializeField] private Vector2 _space = new(0f, 0f);
        [SerializeField] private int _column = 3;
        [SerializeField] private ContentPadding _contentPadding;

        private float _targetPosition;

        private Vector2 CellStride => _cellSize + _space;

        public Vector2 CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                SetDirty();
            }
        }

        public Vector2 Space
        {
            get => _space;
            set
            {
                _space = value;
                SetDirty();
            }
        }

        public int Column
        {
            get => _column;
            set
            {
                _column = value;
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
            var axis = Scroller.Axis;
            return Mathf.Max(0f,
                       CellStride[axis] * Mathf.CeilToInt(dataCount / (float)_column) - ViewportSize - _space[axis]) +
                   _contentPadding.Size;
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
            var rowStride = CellStride[axis];
            var colStride = CellStride[axis == 0 ? 1 : 0];
            var padding = _contentPadding.Start;
            var scrollPosition = Scroller.ScrollPosition;
            Content.SetLocalPosition(scrollPosition, axis);
            scrollPosition += padding * -direction;
            var startRow = Mathf.Max(0, Mathf.FloorToInt(scrollPosition * direction / rowStride));
            var endRow = Mathf.FloorToInt((scrollPosition * direction + ViewportSize - 1) / rowStride);
            var start = startRow * _column;
            var end = Mathf.Min((endRow + 1) * _column - 1, dataCount - 1);
            CellViewPool.ReleaseOutOfRange(start, end);
            visibleRange = new Range(start, end);
            for (var row = startRow; row <= endRow; row++)
            {
                var rowPos = (row * rowStride - ViewportHalf + _cellSize[axis] * 0.5f + padding) * -direction;
                for (var col = 0; col < _column; col++)
                {
                    var i = row * _column + col;
                    if (i >= dataCount) break;
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

                    var colPos = colStride * col - colStride * (_column - 1) * 0.5f;
                    cell.SetCellPosition(
                        axis == 0 ? new Vector2(rowPos, colPos * direction) : new Vector2(colPos, rowPos));
                }
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

        public void SetPositionIndex(int positionIndex, float pivot = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            var axis = Scroller.Axis;
            var direction = Scroller.Direction;
            var rowStride = CellStride[axis];
            positionIndex = Mathf.Clamp(positionIndex, 0, dataCount - 1) / _column;
            var offset = (ViewportSize - rowStride) * (Mathf.Clamp01(pivot) * direction + 0.5f);
            var position = rowStride * positionIndex * direction + ViewportHalf - rowStride * 0.5f - offset;
            SetScrollPosition(position, smooth);
        }
        
        public void SetScrollPosition(float position, bool smooth = true)
        {
            if (DataSource == null) return;
            Scroller.Stop();
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