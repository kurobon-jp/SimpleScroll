using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleScroll
{
    internal class ScrollbarEventDetector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Scrollbar _scrollbar;
        private bool _isHandling;

        internal bool IsHandling
        {
            get => _isHandling;
            private set
            {
                if (_isHandling == value) return;
                _isHandling = value;
                Listener?.OnScrollbarHandling(value);
            }
        }

        internal IScrollbarEventListener Listener { get; set; }

        private void OnEnable()
        {
            _scrollbar = GetComponent<Scrollbar>();
            _scrollbar.onValueChanged.AddListener(OnScrollbarValueChanged);
        }

        private void OnDisable()
        {
            IsHandling = false;
            Listener = null;
            if (_scrollbar == null) return;
            _scrollbar.onValueChanged.RemoveListener(OnScrollbarValueChanged);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!_scrollbar.IsInteractable()) return;
            IsHandling = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (!_scrollbar.IsInteractable()) return;
            IsHandling = false;
        }

        private void OnScrollbarValueChanged(float normalizedPosition)
        {
            Listener?.OnScrollbarValueChanged(normalizedPosition);
        }
    }

    internal interface IScrollbarEventListener
    {
        void OnScrollbarValueChanged(float normalizedPosition);
        void OnScrollbarHandling(bool isHandled);
    }
}