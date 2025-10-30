using UnityEngine;

namespace SimpleScroll.Utils
{
    internal static class VectorExtensions
    {
        internal static float GetAxialValue(this Vector2 self)
        {
            var axis = Mathf.Abs(Vector2.Dot(self.normalized, Vector2.right)) < 0.5f ? 1 : 0;
            return self[axis];
        }
    }
}