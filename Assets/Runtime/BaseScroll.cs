using SimpleScroll.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleScroll
{
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    public abstract class BaseScroll<TDataSource> : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IScrollHandler where TDataSource : IDataSource
    {
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private Transform _content;
        [SerializeField] private Scroller _scroller;
        [SerializeField] private Scrollbar _scrollbar;

        private bool _isDirty;
        private int _pointerId = int.MinValue;
        private int _dataCount;
        private bool _isResized;

        internal Scroller Scroller => _scroller;
        protected Transform Content => _content;
        internal CellViewPool CellViewPool { get; } = new();
        protected TDataSource DataSource { get; private set; }
        protected float ViewportSize { get; private set; }
        protected float ViewportHalf { get; private set; }

        public bool IsScrollable { get; set; } = true;
        public bool IsDraggable { get; set; } = true;

        protected override void OnEnable()
        {
            SetDirty();
            if (_viewport == null)
            {
                _viewport = GetComponent<RectTransform>();
            }

            if (_scrollbar == null) return;
            _scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
        }

        protected override void OnDisable()
        {
            if (_scrollbar == null) return;
            _scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        public virtual void SetNormalizedPosition(float normalizedPosition)
        {
            _scroller.Stop();
            _scroller.NormalizedPosition = normalizedPosition;
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar == null) return;
            _scrollbar.SetValueWithoutNotify(_scroller.NormalizedPosition);
        }

        private void Resize(bool force = false)
        {
            if (DataSource == null) return;
            var viewportSize = _viewport.rect.size[_scroller.Axis];
            if (!force && Mathf.Approximately(ViewportSize, viewportSize)) return;
            ViewportSize = viewportSize;
            ViewportHalf = viewportSize * 0.5f;

            var scrollSize = GetScrollSize();
            _scroller.Initialize(scrollSize);
            _isResized = true;

            if (_scrollbar == null) return;
            if (float.IsInfinity(scrollSize))
            {
                _scrollbar.gameObject.SetActive(false);
            }
            else
            {
                var scrollbarSize = Mathf.Clamp01(ViewportSize / scrollSize);
                _scrollbar.size = Mathf.Clamp(scrollbarSize, 0f, 1f - 0.001f);
                _scrollbar.gameObject.SetActive(scrollbarSize < 1f);
            }
        }

        public void SetDataSource(TDataSource dataSource)
        {
            DataSource = dataSource;
            CellViewPool.SetDataSource(dataSource);
        }

        public void Refresh(bool resetPosition = true)
        {
            CellViewPool.ReleaseAll();
            Resize(true);
            if (resetPosition)
            {
                SetNormalizedPosition(0);
            }
        }

        protected void UpdatePosition(float targetPosition)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            if (dataCount != _dataCount || _isDirty)
            {
                Refresh(false);
                _dataCount = dataCount;
                _isDirty = false;
            }

            var scrollDelta = _scroller.Update(targetPosition);
            UpdateScrollbar();
            Reposition(scrollDelta, _isResized);
            _isResized = false;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != int.MinValue) return;
            _pointerId = e.pointerId;
            _scroller.OnBeginDrag(e);
        }

        void IDragHandler.OnDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId || !IsDraggable) return;
            _scroller.OnDrag(e);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId) return;
            _pointerId = int.MinValue;
            var targetPosition = _scroller.OnEndDrag(e);
            OnDrag(targetPosition);
        }

        void IScrollHandler.OnScroll(PointerEventData e)
        {
            if (DataSource == null && !IsScrollable) return;
            _scroller.Stop();
            OnScroll(e.scrollDelta.GetAxialValue());
        }

        protected abstract float GetScrollSize();
        protected abstract void Reposition(float scrollDelta, bool isResized);
        protected abstract void OnDrag(float targetPosition);
        protected abstract void OnScroll(float delta);
        protected abstract void OnStopScroll(float velocity);

        public void StopScroll()
        {
            var velocity = _scroller.Velocity;
            _scroller.Stop();
            OnStopScroll(velocity);
        }

        protected float ClampPosition(float position)
        {
            var scrollSize = _scroller.ScrollSize;
            var axis = _scroller.Axis;
            var min = axis == 0 ? -scrollSize : 0f;
            var max = axis == 0 ? 0f : scrollSize;
            return Mathf.Clamp(position, min, max);
        }

        protected void SetDirty()
        {
            _isDirty = true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SetDirty();
        }
#endif
    }
}