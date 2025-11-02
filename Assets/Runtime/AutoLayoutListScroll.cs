using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleScroll.Utils;

namespace SimpleScroll
{
    public class AutoLayoutListScroll : BaseScroll<IDataSource>
    {
        [SerializeField] private float _space = 10f;
        [SerializeField] private ContentPadding _contentPadding;
        [SerializeField] private float _defaultCellSize = 100f;

        private readonly List<float> _offsets = new();
        private readonly List<float> _sizes = new();
        private float _targetPosition;
        private int _targetIndex = -1;
        private float _targetAnchor;
        private bool _targetSmooth;
        private int _knownSizeEndIndex = -1;

        public ContentPadding ContentPadding
        {
            get => _contentPadding;
            set
            {
                _contentPadding = value;
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

        protected override float GetScrollSize()
        {
            var dataCount = DataSource.GetDataCount();
            _offsets.Clear();
            _sizes.Clear();
            _knownSizeEndIndex = -1;

            var offset = _contentPadding.Start;
            for (var i = 0; i < dataCount; i++)
            {
                _offsets.Add(offset);
                var size = _defaultCellSize;
                _sizes.Add(size);
                offset += size;
                if (i < dataCount - 1)
                    offset += _space;
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

            scrollPosition *= direction;

            // 表示範囲を求める
            var start = _offsets.BinarySearch(scrollPosition);
            if (start < 0)
                start = Mathf.Max(~start - 1, 0);

            var end = start;
            var viewportSize = ViewportSize + scrollPosition - _offsets[start];
            for (var i = start; i < dataCount; i++)
            {
                viewportSize -= _sizes[i];
                end = i;
                if (viewportSize < 0) break;
            }

            CellViewPool.ReleaseOutOfRange(start, end);
            var sizeDelta = 0f;
            var fillSize = _offsets[start] - scrollPosition;
            for (var i = start; i <= end; i++)
            {
                var needRebuild = false;
                if (CellViewPool.TryGetVisibleCell(i, out var cell))
                {
                    if (isResized)
                    {
                        needRebuild = true;
                    }
                }
                else
                {
                    cell = CellViewPool.Get(i, Content);
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
                if (!Mathf.Approximately(_sizes[i], actualSize))
                {
                    sizeDelta = _sizes[i] - actualSize;
                    _sizes[i] = actualSize;
                    _knownSizeEndIndex = Mathf.Max(_knownSizeEndIndex, i);
                    RebuildOffsetsFromKnownSizes();
                }

                var offset = _offsets[i];
                var size = _sizes[i];
                var pos = (offset - ViewportHalf + size * 0.5f) * -direction;
                cell.anchoredPosition = axis == 0
                    ? new Vector2(pos, 0f)
                    : new Vector2(0f, pos);

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

            if (sizeDelta != 0f && Math.Sign(scrollDelta) == -direction)
            {
                sizeDelta *= -direction;
                _targetPosition += sizeDelta;
                Scroller.ScrollPosition += sizeDelta;
            }

            if (isResized)
            {
                if (end == dataCount - 1)
                {
                    _targetPosition = Scroller.ScrollPosition = Scroller.ScrollSize * direction;
                }
                else
                {
                    _targetPosition = Scroller.ScrollPosition = ClampPosition(Scroller.ScrollPosition);
                }
            }

            if (_targetIndex >= 0)
            {
                var targetPosition = (_offsets[_targetIndex] - (ViewportSize - _sizes[_targetIndex]) * _targetAnchor) *
                                     direction;
                if (Mathf.Approximately(targetPosition, Scroller.ScrollPosition))
                {
                    _targetIndex = -1;
                }
                else
                {
                    _targetPosition = targetPosition;
                    if (!_targetSmooth)
                    {
                        Scroller.ScrollPosition = targetPosition;
                    }
                }
            }

            var scrollPos = axis == 0
                ? new Vector2(Scroller.ScrollPosition, 0f)
                : new Vector2(0f, Scroller.ScrollPosition);
            Content.localPosition = scrollPos;
        }

        private void LateUpdate()
        {
            if (Scroller.IsIdling)
            {
                _targetPosition = ClampPosition(_targetPosition);
            }

            UpdatePosition(_targetPosition);
        }

        private void RebuildOffsetsFromKnownSizes()
        {
            var offset = _contentPadding.Start;
            var count = _sizes.Count;
            for (var i = 0; i < count; i++)
            {
                _offsets[i] = offset;
                var size = i <= _knownSizeEndIndex ? _sizes[i] : _defaultCellSize;
                offset += size;
                if (i < count - 1)
                    offset += _space;
            }

            Scroller.ScrollSize = _offsets[^1] + _sizes[^1] + _contentPadding.End - ViewportSize;
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
            _targetIndex = index;
            _targetAnchor = anchor;
            _targetSmooth = smooth;
        }
    }
}