using UnityEngine;
using UnityEngine.UI;

namespace SimpleScroll.Samples
{
    public class ChatView : MonoBehaviour
    {
        [SerializeField] private Text _text;

        public void Setup(string name, string message)
        {
            gameObject.name = name;
            _text.text = message;
        }
    }
}
