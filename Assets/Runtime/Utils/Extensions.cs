using UnityEngine;

namespace SimpleScroll
{
    internal static class Extensions
    {
        internal static float GetAxialValue(this Vector2 self)
        {
            return Mathf.Abs(Vector2.Dot(self.normalized, Vector2.right)) < 0.5f ? self.y : self.x;
        }

        internal static void SetPivot(this RectTransform self, float value, int axis)
        {
            var pivot = self.pivot;
            pivot[axis] = value;
            self.pivot = pivot;
        }
        
        internal static void SetAnchor(this RectTransform self, float value, int axis)
        {
            var anchorMin = self.anchorMin;
            var anchorMax = self.anchorMax;
            anchorMin[axis] = value;
            anchorMax[axis] = value;
            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
        }

        internal static void SetAnchoredPosition(this RectTransform self, float value, int axis)
        {
            var anchoredPosition = self.anchoredPosition;
            anchoredPosition[axis] = value;
            self.anchoredPosition = anchoredPosition;
        }

        internal static void SetLocalPosition(this Transform self, float value, int axis)
        {
            var localPosition = self.localPosition;
            localPosition[axis] = value;
            self.localPosition = localPosition;
        }
    }
}