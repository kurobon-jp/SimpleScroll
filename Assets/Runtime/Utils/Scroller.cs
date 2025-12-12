using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleScroll
{
    [Serializable]
    internal class Scroller
    {
        private enum ScrollStatus
        {
            Idle,
            Dragging,
            Scrolling
        }

        internal const float VelocityThreshold = 10f;

        [SerializeField] private RectTransform.Axis _axis;
        [SerializeField] private bool _inertia = true;
        [SerializeField, Tooltip("px/sec²")] private float _deceleration = 10000f; // px/sec²
        [SerializeField, Tooltip("px/sec")] private float _maxVelocity = 10000f; // px/sec

        [SerializeField, Range(0.1f, 1f), Tooltip("%")]
        private float _dragSensitivity = 1f;

        [SerializeField] private ScrollEvent _onValueChanged;

        private ScrollStatus _status;
        private float _scrollPosition;
        private float _scrollDelta;
        private float _scrollSize = float.PositiveInfinity;
        private float _velocity;
        private float _elasticTime;
        private Vector2 _pointerPoint;
        private RectTransform _dragTarget;
        private int _dragFrame;

        internal int Axis => (int)_axis;
        internal int Direction => _axis == RectTransform.Axis.Horizontal ? -1 : 1;
        internal bool IsInertia => _inertia;
        internal bool IsIdling => _status == ScrollStatus.Idle;
        internal bool IsDragging => _status == ScrollStatus.Dragging;
        internal bool IsScrolling => _status == ScrollStatus.Scrolling;
        internal float Velocity => _velocity * -Direction;
        internal ScrollEvent OnValueChanged => _onValueChanged;

        internal float ScrollSize
        {
            get => _scrollSize;
            set => _scrollSize = value;
        }

        internal float ScrollPosition
        {
            get => _scrollPosition;
            set
            {
                if (Mathf.Approximately(_scrollPosition, value)) return;
                _scrollPosition = value;
                _onValueChanged?.Invoke(value);
            }
        }

        internal float NormalizedPosition
        {
            get
            {
                if (_scrollSize == 0f || float.IsInfinity(_scrollSize)) return 0f;
                return Mathf.Clamp01(ScrollPosition * Direction / _scrollSize);
            }
            set
            {
                if (_scrollSize == 0f || float.IsInfinity(_scrollSize)) return;
                ScrollPosition = Mathf.Clamp01(value) * _scrollSize * Direction;
            }
        }

        internal void Initialize(float scrollSize = float.PositiveInfinity)
        {
            _scrollSize = Mathf.Max(0f, scrollSize);
            if (!float.IsInfinity(scrollSize))
            {
                var min = 0f;
                var max = scrollSize;
                if (Axis == 0)
                {
                    min = -scrollSize;
                    max = 0;
                }

                ScrollPosition = Mathf.Clamp(_scrollPosition, min, max);
            }

            _velocity = 0f;
            _status = ScrollStatus.Idle;
        }

        internal void OnBeginDrag(PointerEventData e)
        {
            _dragTarget = e.pointerDrag?.GetComponent<RectTransform>();
            if (_dragTarget == null) return;
            _status = ScrollStatus.Dragging;
            _velocity = 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragTarget, e.position, e.pressEventCamera,
                out _pointerPoint);
        }

        internal void OnDrag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragTarget, e.position, e.pressEventCamera,
                out var localPoint);
            var delta = (localPoint - _pointerPoint)[Axis] * _dragSensitivity;
            _pointerPoint = localPoint;
            _dragFrame = Time.frameCount;
            var dt = Time.unscaledDeltaTime;
            var newVelocity = delta / dt;
            _velocity = Mathf.Lerp(_velocity, newVelocity, dt * 10f);
            _scrollDelta += delta;
            ScrollPosition += delta;
        }

        internal float OnEndDrag(PointerEventData e)
        {
            if (!_inertia)
            {
                _status = ScrollStatus.Idle;
                // _velocity = 0f;
                return ScrollPosition;
            }

            _status = ScrollStatus.Scrolling;
            _velocity = Mathf.Clamp(_velocity, -_maxVelocity, _maxVelocity);
            return ScrollPosition + _velocity * _velocity / (_deceleration * 2f) * Mathf.Sign(_velocity);
        }

        internal float Update(float targetPosition)
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (_status == ScrollStatus.Dragging)
            {
                if (_dragFrame != Time.frameCount)
                {
                    _velocity = Mathf.Lerp(_velocity, 0f, deltaTime * 10f);
                }

                try
                {
                    return _scrollDelta;
                }
                finally
                {
                    _scrollDelta = 0f;
                }
            }

            var speed = _velocity;
            var scrollPos = ScrollPosition;
            if (_status == ScrollStatus.Scrolling)
            {
                var absSpeed = Mathf.Abs(speed);
                var smoothTime = Mathf.Max(0.05f, absSpeed / _deceleration);
                scrollPos = Mathf.SmoothDamp(scrollPos, targetPosition, ref speed, smoothTime, _maxVelocity, deltaTime);
                if (!float.IsPositiveInfinity(_scrollSize))
                {
                    var axis = Axis;
                    var overMin = axis == 0 ? scrollPos > 0f : scrollPos > _scrollSize;
                    var overMax = axis == 0 ? scrollPos < -_scrollSize : scrollPos < 0f;
                    if ((overMin && speed > 0f) || (overMax && speed < 0f))
                    {
                        speed = Mathf.Lerp(speed, 0f, _elasticTime += deltaTime * (1f / 0.05f));
                    }
                }

                _velocity = speed;
                if (Mathf.Abs(speed) < VelocityThreshold)
                {
                    Stop();
                    _elasticTime = 0f;
                }
            }
            else
            {
                scrollPos = Mathf.Abs(scrollPos - targetPosition) < 0.1f
                    ? targetPosition
                    : Mathf.Lerp(scrollPos, targetPosition, deltaTime * 10f);
                _velocity = 0;
            }

            var scrollDelta = scrollPos - ScrollPosition;
            ScrollPosition = scrollPos;
            return scrollDelta;
        }

        internal void Stop()
        {
            _status = ScrollStatus.Idle;
            _velocity = 0f;
        }
    }
}