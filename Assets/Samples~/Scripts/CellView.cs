using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleScroll.Samples
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private Button _button;

        public void Setup(int index)
        {
            gameObject.name = $"{index}";
            _text.text = $"{index}";
        }

        public void Setup(int index, int position, Action<int> onClick)
        {
            gameObject.name = $"{index}";
            _text.text = $"{index}";
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke(position));
        }
    }
}