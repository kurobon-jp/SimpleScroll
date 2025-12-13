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

        public ContentPadding ContentPadding
        {
            get => _contentPadding;
            set
            {
                _contentPadding = value;
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

        protected override void OnScroll(float delta)
        {
            _targetPosition = Scroller.ScrollPosition + delta * 100f * -Scroller.Direction;
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
                return;
            }

            var axis = Scroller.Axis;
            var direction = Scroller.Direction;
            var rowStride = CellStride[axis];
            var colStride = CellStride[axis == 0 ? 1 : 0];
            var padding = _contentPadding.Start;
            var scrollPosition = Scroller.ScrollPosition;
            var scrollPos = axis == 0
                ? new Vector2(scrollPosition, 0f)
                : new Vector2(0f, scrollPosition);
            Content.localPosition = scrollPos;
            scrollPosition += padding * -direction;
            var startRow = Mathf.Max(0, Mathf.FloorToInt(scrollPosition * direction / rowStride));
            var endRow = Mathf.FloorToInt((scrollPosition * direction + ViewportSize - 1) / rowStride);
            CellViewPool.ReleaseOutOfRange(startRow * _column, (endRow + 1) * _column);

            for (var row = startRow; row <= endRow; row++)
            {
                var rowPos = (row * rowStride - ViewportHalf + _cellSize[axis] * 0.5f + padding) * -direction;
                for (var col = 0; col < _column; col++)
                {
                    var i = row * _column + col;
                    if (i >= dataCount) return;
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
                    cell.localPosition =
                        axis == 0 ? new Vector2(rowPos, colPos * direction) : new Vector2(colPos, rowPos);
                }
            }
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
            OnScrollbarValueChanged(normalizedPosition);
        }
        
        public void SetPositionIndex(int index, float pivot = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            var axis = Scroller.Axis;
            var direction = Scroller.Direction;
            var rowStride = CellStride[axis];
            index = Mathf.Clamp(index, 0, dataCount - 1) / _column;
            var offset = (ViewportSize - rowStride) * (Mathf.Clamp01(pivot) * direction + 0.5f);
            var position = rowStride * index * direction + ViewportHalf - rowStride * 0.5f - offset;
            _targetPosition = ClampPosition(position);
            if (!smooth)
            {
                Scroller.ScrollPosition = _targetPosition;
            }
        }
    }
}