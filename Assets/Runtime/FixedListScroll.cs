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
            var dataCount = DataSource.GetDataCount();
            return Mathf.Max(0f, CellStride * dataCount - ViewportSize - _space + _contentPadding.Size);
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
            var stride = CellStride;
            var padding = _contentPadding.Start;
            var scrollPosition = Scroller.ScrollPosition;
            var contentPosition = Content.localPosition;
            contentPosition[axis] = scrollPosition;
            Content.localPosition = contentPosition;
            scrollPosition += padding * -direction;
            var start = Mathf.Max(0, Mathf.FloorToInt(scrollPosition * direction / stride));
            var end = Mathf.Clamp(Mathf.FloorToInt((scrollPosition * direction + ViewportSize) / stride), start,
                dataCount - 1);
            CellViewPool.ReleaseOutOfRange(start, end);
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
                cell.anchoredPosition = axis == 0 ? new Vector2(pos, 0f) : new Vector2(0f, pos);
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

        public void SetPositionIndex(int index, float anchor = 0.5f, bool smooth = true)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            var direction = Scroller.Direction;
            index = Mathf.Clamp(index, 0, dataCount - 1);
            var offset = (ViewportSize - CellStride) * (Mathf.Clamp01(anchor) * direction + 0.5f);
            var position = CellStride * index * direction + ViewportHalf - CellStride * 0.5f - offset;
            _targetPosition = ClampPosition(position);
            if (!smooth)
            {
                Scroller.ScrollPosition = _targetPosition;
            }
        }
    }
}