using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleScroll
{
    public enum ScrollState
    {
        Idle,
        Dragging,
        Coasting,
        Snapping
    }

    [Serializable]
    internal class Scroller
    {
        internal const float VelocityThreshold = 10f;
        private const float SnapResponse = 10f;
        private const float PositionTolerance = 0.001f;

        [SerializeField] private RectTransform.Axis _axis;
        [SerializeField] private bool _inertia = true;
        [SerializeField, Tooltip("px/sec²")] private float _deceleration = 10000f; // px/sec²
        [SerializeField, Tooltip("px/sec")] private float _maxVelocity = 10000f; // px/sec

        [SerializeField, Range(0.1f, 1f), Tooltip("%")]
        private float _dragSensitivity = 1f;

        [SerializeField]
        private float _scrollSensitivity = 100f;
        
        [SerializeField] private bool _overScroll = true;
        [SerializeField] private ScrollEvent _onValueChanged;

        private ScrollState _state;
        private bool _stateChanged = true;
        private float _scrollPosition;
        private float _targetPosition;
        private float _scrollDelta;
        private float _scrollSize = float.PositiveInfinity;
        private float _velocity;
        private float _elasticTime;
        private Vector2 _pointerPoint;
        private RectTransform _dragTarget;
        private int _dragFrame;

        internal ScrollState State
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                _stateChanged = true;
            }
        }

        internal int Axis => (int)_axis;
        internal int Direction => _axis == RectTransform.Axis.Horizontal ? -1 : 1;
        internal bool IsInertia => _inertia;
        internal bool IsIdling => State == ScrollState.Idle;
        internal bool IsDragging => State == ScrollState.Dragging;
        internal bool IsSnapping => State == ScrollState.Snapping;
        internal float Velocity => _velocity * -Direction;
        internal float ScrollSensitivity => _scrollSensitivity * -Direction;
        internal bool OverScroll => _overScroll;
        internal float TargetPosition => _targetPosition;

        internal ScrollEvent OnValueChanged => _onValueChanged;

        internal event Action<ScrollState> OnScrollStateChanged;

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
                var position = _overScroll || float.IsInfinity(ScrollSize) ? value : ClampPosition(value);
                if (_scrollPosition == position) return;
                _scrollPosition = position;
                _onValueChanged?.Invoke(position);
            }
        }

        internal float NormalizedPosition
        {
            get
            {
                if (ScrollSize == 0f || float.IsInfinity(ScrollSize)) return 0f;
                return Mathf.Clamp01(ScrollPosition * Direction / _scrollSize);
            }
            set
            {
                if (ScrollSize == 0f || float.IsInfinity(ScrollSize)) return;
                var dragging = IsDragging;
                MoveTo(Mathf.Clamp01(value) * _scrollSize * Direction, false);
                if (dragging) State = ScrollState.Dragging;
            }
        }

        internal void Initialize(float scrollSize = float.PositiveInfinity)
        {
            ScrollSize = Mathf.Max(0f, scrollSize);
            ScrollPosition = ClampPosition(ScrollPosition);
            _targetPosition = IsIdling ? ScrollPosition : ClampPosition(_targetPosition);
            _stateChanged = true;
        }

        internal void OnBeginDrag(PointerEventData e)
        {
            _dragTarget = e.pointerDrag?.transform as RectTransform;
            if (_dragTarget == null) return;
            Stop();
            State = ScrollState.Dragging;
            _velocity = 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_dragTarget, e.position, e.pressEventCamera,
                out _pointerPoint);
        }

        internal void OnDrag(PointerEventData e)
        {
            if (!IsDragging || _dragTarget == null) return;
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
                State = ScrollState.Idle;
                return ScrollPosition;
            }

            State = ScrollState.Coasting;
            _velocity = Mathf.Clamp(_velocity, -_maxVelocity, _maxVelocity);
            return ScrollPosition + _velocity * _velocity / (_deceleration * 2f) * Mathf.Sign(_velocity);
        }

        internal void MoveTo(float position, bool smooth)
        {
            Stop();
            _targetPosition = ClampPosition(position);
            if (smooth && Mathf.Abs(_targetPosition - ScrollPosition) > PositionTolerance)
            {
                OnSnap();
            }
            else
            {
                ScrollPosition = _targetPosition;
                _targetPosition = ScrollPosition;
            }
        }

        // Retarget a moving layout without resetting its velocity or restarting the movement.
        internal void SetTargetPosition(float position)
        {
            _targetPosition = State == ScrollState.Coasting ? position : ClampPosition(position);
            if (IsIdling && Mathf.Abs(_targetPosition - ScrollPosition) > PositionTolerance) OnSnap();
        }

        // Layout corrections move the coordinate system, including the destination.
        internal void ShiftPosition(float delta)
        {
            _targetPosition += delta;
            ScrollPosition += delta;
            if (IsIdling) _targetPosition = ScrollPosition;
        }

        private void OnSnap()
        {
            State = ScrollState.Snapping;
            _velocity = 0f;
        }

        internal float Update(float deltaTime)
        {
            if (IsIdling)
            {
                _velocity = 0f;
                return 0f;
            }
            if (State == ScrollState.Dragging)
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
            if (IsSnapping) _targetPosition = ClampPosition(_targetPosition);
            var targetPosition = _targetPosition;
            if (State == ScrollState.Coasting)
            {
                var absSpeed = Mathf.Abs(speed);
                var smoothTime = Mathf.Max(0.05f, absSpeed / _deceleration);
                scrollPos = Mathf.SmoothDamp(scrollPos, targetPosition, ref speed, smoothTime, _maxVelocity, deltaTime);
                if (!float.IsInfinity(_scrollSize))
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
                    _targetPosition = ClampPosition(targetPosition);
                    OnSnap();
                    _elasticTime = 0f;
                }
            }
            else
            {
                scrollPos = Mathf.Lerp(scrollPos, targetPosition, deltaTime * SnapResponse);
                _velocity = 0;
            }

            var previousPosition = ScrollPosition;
            if (Mathf.Abs(_targetPosition - scrollPos) > PositionTolerance)
            {
                ScrollPosition = scrollPos;
            }
            else
            {
                ScrollPosition = _targetPosition;
                Stop();
            }

            return ScrollPosition - previousPosition;
        }

        internal void Stop()
        {
            State = ScrollState.Idle;
            _targetPosition = ScrollPosition;
            _velocity = 0f;
            _scrollDelta = 0f;
            _elasticTime = 0f;
        }

        internal float ClampPosition(float position)
        {
            var scrollSize = ScrollSize;
            if (float.IsInfinity(scrollSize)) return position;
            var axis = Axis;
            var min = axis == 0 ? -scrollSize : 0f;
            var max = axis == 0 ? 0f : scrollSize;
            return Mathf.Clamp(position, min, max);
        }

        internal void NotifyScrollStateChanged()
        {
            if (!_stateChanged) return;
            _stateChanged = false;
            OnScrollStateChanged?.Invoke(State);
        }
    }
}
