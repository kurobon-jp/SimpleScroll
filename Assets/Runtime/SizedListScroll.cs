using System;
using UnityEngine;

namespace SimpleScroll
{
    public class SizedListScroll : BaseScroll<ISizedDataSource>
    {
        [SerializeField] private float _space;
        [SerializeField] private ContentPadding _contentPadding;

        private float[] _offsets = Array.Empty<float>();
        private float _targetPosition;

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
            if (dataCount != _offsets.Length)
            {
                Array.Resize(ref _offsets, dataCount);
            }

            var offset = _contentPadding.Start;
            for (var i = 0; i < dataCount; i++)
            {
                _offsets[i] = offset;
                offset += DataSource.GetCellViewSize(i);
                if (i < dataCount - 1)
                {
                    offset += _space;
                }
            }

            offset += _contentPadding.End;
            return Mathf.Max(0f, offset - ViewportSize);
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
            var scrollPosition = Scroller.ScrollPosition;
            var scrollPos = axis == 0
                ? new Vector2(scrollPosition, 0f)
                : new Vector2(0f, scrollPosition);
            Content.localPosition = scrollPos;
            scrollPosition *= direction;
            var start = Array.BinarySearch(_offsets, scrollPosition);
            if (start < 0)
            {
                start = Mathf.Max(~start - 1, 0);
            }

            var end = start;
            var viewportSize = ViewportSize + scrollPosition - _offsets[start];
            for (var i = start; i < dataCount; i++)
            {
                viewportSize -= DataSource.GetCellViewSize(i);
                end = i;
                if (viewportSize < 0) break;
            }

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

                var offset = _offsets[i];
                var size = DataSource.GetCellViewSize(i);
                var pos = (offset - ViewportHalf + size * 0.5f) * -direction;
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
            Scroller.Stop();
            var dataCount = DataSource.GetDataCount();
            index = Mathf.Clamp(index, 0, dataCount - 1);
            var size = DataSource.GetCellViewSize(index);
            var position = (_offsets[index] - (ViewportSize - size) * anchor) * Scroller.Direction;
            _targetPosition = ClampPosition(position);
            if (!smooth)
            {
                Scroller.ScrollPosition = _targetPosition;
            }
        }
    }
}