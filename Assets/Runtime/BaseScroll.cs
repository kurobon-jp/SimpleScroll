#if UNITY_EDITOR
using UnityEditor;
#endif
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
        [SerializeField] private RectTransform _content;
        [SerializeField] private Scroller _scroller;
        [SerializeField] private Scrollbar _scrollbar;

        private bool _isDirty;
        private int _pointerId = int.MinValue;
        private int _dataCount;
        private bool _isResized;

        internal Scroller Scroller => _scroller;
        protected RectTransform Content => _content;
        internal CellViewPool CellViewPool { get; } = new();
        protected TDataSource DataSource { get; private set; }
        protected float ViewportSize { get; private set; }
        protected float ViewportHalf { get; private set; }
        public ScrollEvent OnValueChanged => Scroller?.OnValueChanged;
        public bool IsScrollable { get; set; } = true;
        public bool IsDraggable { get; set; } = true;
        public float ScrollPosition => _scroller.ScrollPosition;
        public float NormalizedPosition => _scroller.NormalizedPosition;

        protected override void OnEnable()
        {
            _pointerId = int.MinValue;
            if (_viewport == null)
            {
                _viewport = GetComponent<RectTransform>();
            }

            SetAxisPivots();
            SetDirty();
            if (_scrollbar == null) return;
            _scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        }

        protected override void OnDisable()
        {
#if UNITY_EDITOR
            _tracker.Clear();
#endif
            if (_scrollbar == null) return;
            _scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        protected virtual void OnScrollbarValueChanged(float normalizedPosition)
        {
            if (_scroller == null) return;
            _scroller.Stop();
            _scroller.NormalizedPosition = normalizedPosition;
        }

        private void UpdateScrollbar()
        {
            if (_scrollbar == null || _scroller == null) return;
            _scrollbar.SetValueWithoutNotify(_scroller.NormalizedPosition);
        }

        private void Resize()
        {
            if (DataSource == null || _scroller == null) return;
            ViewportSize = _viewport.rect.size[_scroller.Axis];
            ViewportHalf = ViewportSize * 0.5f;

            var scrollSize = GetScrollSize();
            _scroller.Initialize(scrollSize);
            _isResized = true;

            if (_scrollbar == null) return;
            if (float.IsInfinity(scrollSize) || scrollSize <= 0)
            {
                _scrollbar.gameObject.SetActive(false);
            }
            else
            {
                _scrollbar.gameObject.SetActive(true);
                var size = Mathf.Clamp01(ViewportSize / (scrollSize + ViewportSize));
                _scrollbar.size = Mathf.Max(size, 0.05f);
            }
        }

        public void SetDataSource(TDataSource dataSource)
        {
            DataSource = dataSource;
            CellViewPool.SetDataSource(dataSource);
        }

        public void Refresh()
        {
            CellViewPool.ReleaseAll();
            Resize();
        }

        public virtual void Refresh(float normalizedPosition)
        {
            Refresh();
            OnScrollbarValueChanged(normalizedPosition);
        }

        protected void UpdatePosition(float targetPosition)
        {
            if (DataSource == null) return;
            var dataCount = DataSource.GetDataCount();
            if (dataCount != _dataCount || _isDirty)
            {
                Refresh();
                _dataCount = dataCount;
                _isDirty = false;
            }

            if (_scroller == null) return;
            var scrollDelta = _scroller.Update(targetPosition);
            UpdateScrollbar();
            Reposition(scrollDelta, _isResized);
            _isResized = false;
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != int.MinValue) return;
            _pointerId = e.pointerId;
            _scroller?.OnBeginDrag(e);
        }

        void IDragHandler.OnDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId || !IsDraggable) return;
            _scroller?.OnDrag(e);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData e)
        {
            if (DataSource == null || _pointerId != e.pointerId) return;
            _pointerId = int.MinValue;
            var targetPosition = _scroller?.OnEndDrag(e) ?? 0f;
            OnDrag(targetPosition);
        }

        void IScrollHandler.OnScroll(PointerEventData e)
        {
            if (DataSource == null || !IsScrollable) return;
            _scroller?.Stop();
            OnScroll(e.scrollDelta.GetAxialValue());
        }

        protected abstract float GetScrollSize();
        protected abstract void Reposition(float scrollDelta, bool isResized);
        protected abstract void OnDrag(float targetPosition);
        protected abstract void OnScroll(float delta);
        protected abstract void OnStopScroll(float velocity);

        public void StopScroll()
        {
            if (_scroller == null) return;
            var velocity = _scroller.Velocity;
            _scroller.Stop();
            OnStopScroll(velocity);
        }

        protected float ClampPosition(float position)
        {
            return Scroller?.ClampPosition(position) ?? position;
        }

        protected void SetDirty()
        {
            _isDirty = true;
        }

        private void SetAxisPivots()
        {
            if (_scroller == null) return;
            var axis = _scroller.Axis;
            if (_viewport != null)
            {
                _viewport.SetPivot(0.5f, axis);
            }

            if (_content != null)
            {
                _content.SetPivot(0.5f, axis);
            }
        }

#if UNITY_EDITOR
        private DrivenRectTransformTracker _tracker;

        protected override void OnValidate()
        {
            if (IsDestroyed() || _scroller == null) return;
            _tracker.Clear();
            var axis = _scroller.Axis;
            var properties = axis == 0
                ? DrivenTransformProperties.PivotX
                : DrivenTransformProperties.PivotY;
            if (_viewport != null)
            {
                _tracker.Add(this, _viewport, properties);
            }

            if (_content != null)
            {
                _tracker.Add(this, _content, properties);
            }

            EditorApplication.delayCall += SetAxisPivots;
            SetDirty();
        }
#endif
    }
}