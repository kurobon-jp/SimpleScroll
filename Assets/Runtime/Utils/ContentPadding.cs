using System;
using UnityEngine;

namespace SimpleScroll
{
    [Serializable]
    public struct ContentPadding
    {
        [SerializeField] private float _start;
        [SerializeField] private float _end;

        public float Start => _start;
        public float End => _end;
        public float Size => _start + _end;
    }
}