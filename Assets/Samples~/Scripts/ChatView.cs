using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SimpleScroll.Samples
{
    public class ChatView : MonoBehaviour
    {
        private static readonly string _charas = " abcdefghijklmnopqrstuvwxyz";
        [SerializeField] private Text _text;

        public void Setup(int index)
        {
            gameObject.name = $"{index}";
            Random.InitState(index);
            _text.text = string.Join("",
                Enumerable.Range(0, Random.Range(10, 500)).Select(x => _charas[Random.Range(0, _charas.Length)]));
        }
    }
}