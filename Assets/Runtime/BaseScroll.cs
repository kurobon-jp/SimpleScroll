#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleScroll
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class BaseScroll<TDataSource> : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IScrollbarEventListener,
        IScrollHandler where TDataSource : IDataSource
    {
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Scroller _scroller;
        [SerializeField] private Scrollbar _scrollbar;
        [SerializeField] private bool _isDraggable = true;
        [SerializeField] private bool _isScrollable = true;

        private bool _isDirty;
        private int _pointerId = int.MinValue;
        private int _dataCount;
        private bool _isResized;
        private ScrollbarEventDetector _scrollbarEventDetector;

        internal Scroller Scroller
        {
            get
            {
                _scroller ??= new Scroller();
                return _scroller;
            }
        }

        protected RectTransform Content => _content;
        internal CellViewPool CellViewPool { get; } = new();
        protected TDataSource DataSource { get; private set; }
        protected float ViewportSize { get; private set; }
        protected float ViewportHalf { get; private set; }

        // Layout offsets increase from the start of the content, regardless of the scroll axis.
        protected float ScrollOffset => ToScrollOffset(Scroller.ScrollPosition);

        public Range VisibleRange { get; private set; } = Range.Empty;
        public ScrollEvent OnValueChanged => _scroller.OnValueChanged;
        public bool IsDragging => Scroller.IsDragging;
        public bool IsSnapping => Scroller.IsSnapping;
        public bool IsIdling => Scroller.IsIdling;
        public float ScrollPosition => Scroller.ScrollPosition;
        public float NormalizedPosition => Scroller.NormalizedPosition;

        public bool IsDraggable
        {
            get => _isDraggable;
            set => _isDraggable = value;
        }

        public bool IsScrollable
        {
            get => _isScrollable;
            set => _isScrollable = value;
        }

        public event Action<Range> OnVisibleRangeChanged;

        public event Action<ScrollState> OnScrollStateChanged
        {
            add => Scroller.OnScrollStateChanged += value;
            remove => Scroller.OnScrollStateChanged -= value;
        }

        protected override void OnEnable()
        {
            _pointerId = int.MinValue;
            if (_viewport == null)
            {
                _viewport = transform as RectTransform;
            }

            SetAxisPivotAndDeltaSize();
            SetDirty();
            if (_scrollbar == null) return;
            _scrollbarEventDetector = _scrollbar.gameObject.AddComponent<ScrollbarEventDetector>();
            _scrollbarEventDetector.Listener = this;
        }

        protected override void OnDisable()
        {
            OnScrollInterrupted();
            Scroller.Stop();
            _pointerId = int.MinValue;
            if (_scrollbarEventDetector == null) return;
            Destroy(_scrollbarEventDetector);
            _scrollbarEventDetector = null;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar == null) return;
            _scrollbar.SetValueWithoutNotify(Scroller.NormalizedPosition);
        }

        private void Resize()
        {
            if (DataSource == null) return;
            ViewportSize = _viewport.rect.size[Scroller.Axis];
            ViewportHalf = ViewportSize * 0.5f;

            var scrollSize = GetScrollSize();
            Scroller.Initialize(scrollSize);
            _isResized = true;

            if (_scrollbar == null) return;
            if (float.IsInfinity(scrollSize) || scrollSize <= 0)
            {
                _scrollbar.gameObject.SetActive(false);
            }
            else
            {
                var size = Mathf.Clamp01(ViewportSize / (scrollSize + ViewportSize));
                _scrollbar.size = Mathf.Max(size, 0.05f);
                _scrollbar.gameObject.SetActive(true);
            }
        }

        public void SetDataSource(TDataSource dataSource)
        {
            DataSource = dataSource;
            CellViewPool.SetDataSource(dataSource);
        }

        public void Refresh(bool isRefreshVisibleCells = true)
        {
            if (isRefreshVisibleCells)
            {
                CellViewPool.ReleaseAll();
            }

            Resize();
        }

        /// <summary>Refreshes data and layout at the requested normalized position before cells are updated.</summary>
        public virtual void Refresh(float normalizedPosition, bool isRefreshVisibleCells = true)
        {
            Refresh(isRefreshVisibleCells);
            SetNormalizedPosition(normalizedPosition);
        }

        protected virtual void LateUpdate()
        {
            UpdatePosition();
        }

        protected void EnsureLayout()
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            if (dataCount != _dataCount || _isDirty)
            {
                Refresh(_isDirty);
                _dataCount = dataCount;
                _isDirty = false;
            }
        }

        protected void UpdatePosition()
        {
            if (DataSource == null) return;
            EnsureLayout();
            var dataCount = _dataCount;
            if (dataCount == 0)
            {
                OnScrollInterrupted();
                Scroller.MoveTo(0f, false);
            }
            PrepareScroll();
            var scrollDelta = Scroller.Update(Time.unscaledDeltaTime);
            var visibleRange = Reposition(scrollDelta, _isResized);
            UpdateScrollbar();
            _isResized = false;
            if (!VisibleRange.Equals(visibleRange))
            {
                VisibleRange = visibleRange;
                OnVisibleRangeChanged?.Invoke(visibleRange);
            }

            Scroller.NotifyScrollStateChanged();
        }

        [Obsolete("Use MoveToPosition(position, smooth) to start movement and UpdatePosition() to update it.")]
        protected void UpdatePosition(float targetPosition)
        {
            Scroller.SetTargetPosition(targetPosition);
            UpdatePosition();
        }

        /// <summary>Sets a position between 0 and 1 without rebinding cells. Requires a finite, initialized layout.</summary>
        public virtual void SetNormalizedPosition(float normalizedPosition)
        {
            if (_pointerId != int.MinValue) Scroller.Stop();
            _pointerId = int.MinValue;
            OnScrollInterrupted();
            Scroller.NormalizedPosition = normalizedPosition;
            OnNormalizePositionChanged(Scroller.NormalizedPosition);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != int.MinValue || !IsDraggable) return;
            if (!Scroller.OverScroll && Scroller.ScrollSize <= 0f) return;
            _pointerId = e.pointerId;
            OnScrollInterrupted();
            Scroller.OnBeginDrag(e);
        }

        void IDragHandler.OnDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId || !IsDraggable) return;
            Scroller.OnDrag(e);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId) return;
            _pointerId = int.MinValue;
            if (!IsDraggable)
            {
                Stop();
                return;
            }
            var targetPosition = Scroller.OnEndDrag(e);
            OnDrag(targetPosition);
        }

        void IScrollHandler.OnScroll(PointerEventData e)
        {
            if (DataSource == null || !IsScrollable || _scrollbarEventDetector is { IsHandling: true }) return;
            var scrollSize = Scroller.ScrollSize;
            if (scrollSize <= 0f) return;
            var axialValue = e.scrollDelta.GetAxialValue();
            if (!float.IsInfinity(scrollSize))
            {
                var normalizedPosition = Scroller.NormalizedPosition;
                if (normalizedPosition <= 0f && axialValue > 0f || normalizedPosition >= 1f && axialValue < 0f) return;
            }

            OnScrollInterrupted();
            OnScroll(axialValue);
        }

        void IScrollbarEventListener.OnScrollbarValueChanged(float normalizedPosition)
        {
            SetNormalizedPosition(normalizedPosition);
        }

        void IScrollbarEventListener.OnScrollbarHandling(bool isHandled)
        {
            OnScrollInterrupted();
            Scroller.Stop();
            if (isHandled) Scroller.State = ScrollState.Dragging;
            else OnDrag(ScrollPosition);
        }

        protected abstract float GetScrollSize();
        protected abstract Range Reposition(float scrollDelta, bool isResized);
        protected virtual void OnDrag(float targetPosition)
        {
            Scroller.SetTargetPosition(targetPosition);
        }

        protected virtual void OnScroll(float delta)
        {
            Scroller.MoveTo(ScrollPosition + delta * Scroller.ScrollSensitivity, true);
        }

        protected virtual void OnStopScroll(float velocity) {}

        // Cancel layout-specific tracking before another source takes control of movement.
        protected virtual void OnScrollInterrupted() {}

        protected virtual void PrepareScroll() {}

        protected void MoveToPosition(float position, bool smooth)
        {
            _pointerId = int.MinValue;
            OnScrollInterrupted();
            Scroller.MoveTo(position, smooth);
        }

        protected virtual void OnNormalizePositionChanged(float normalizedPosition)
        {
        }

        /// <summary>Cancels movement and cell tracking at the current position.</summary>
        public void Stop()
        {
            _pointerId = int.MinValue;
            OnScrollInterrupted();
            Scroller.Stop();
        }

        [Obsolete("Use Stop() to stop at the current position. CarouselScroll.SnapToNearest() explicitly snaps to a cell.")]
        public void StopScroll()
        {
            var velocity = Scroller.Velocity;
            Stop();
            OnStopScroll(velocity);
        }

        protected float ClampPosition(float position)
        {
            return Scroller?.ClampPosition(position) ?? position;
        }

        protected float ToScrollOffset(float position) => position * Scroller.Direction;

        protected float ToScrollPosition(float offset) => offset * Scroller.Direction;

        // SetCellPosition expects the cell center relative to the viewport center.
        protected float GetCellPosition(float offset, float size)
        {
            return ToScrollPosition(ViewportHalf - offset - size * 0.5f);
        }

        protected float GetAlignedScrollPosition(float offset, float size, float anchor)
        {
            return ToScrollPosition(offset - (ViewportSize - size) * anchor);
        }

        protected void SetDirty()
        {
            _isDirty = true;
        }

        private void SetAxisPivotAndDeltaSize()
        {
            var axis = Scroller.Axis;
            if (_viewport != null)
            {
                _viewport.SetPivot(0.5f, axis);
            }

            if (_content != null)
            {
                _content.SetDeltaSize(0f, axis);
            }
        }

#if UNITY_EDITOR
        private DrivenRectTransformTracker _tracker;

        protected override void OnValidate()
        {
            if (IsDestroyed()) return;
            _tracker.Clear();
            var axis = Scroller.Axis;
            var pivot = axis == 0
                ? DrivenTransformProperties.PivotX
                : DrivenTransformProperties.PivotY;
            var deltaSize = axis == 0
                ? DrivenTransformProperties.SizeDeltaX
                : DrivenTransformProperties.SizeDeltaY;
            if (_viewport != null)
            {
                _tracker.Add(this, _viewport, pivot);
            }

            if (_content != null)
            {
                _tracker.Add(this, _content, deltaSize);
            }

            EditorApplication.delayCall += SetAxisPivotAndDeltaSize;
            SetDirty();
        }
#endif
    }
}
