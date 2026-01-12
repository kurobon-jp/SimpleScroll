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
            if (axis == 0)
                pivot.x = value;
            else
                pivot.y = value;
            self.pivot = pivot;
        }

        internal static void SetDeltaSize(this RectTransform self, float value, int axis)
        {
            var sizeDelta = self.sizeDelta;
            if (axis == 0)
                sizeDelta.x = value;
            else
                sizeDelta.y = value;
            self.sizeDelta = sizeDelta;
        }

        internal static void SetAnchor(this RectTransform self, float value, int axis)
        {
            var anchorMin = self.anchorMin;
            var anchorMax = self.anchorMax;
            if (axis == 0)
            {
                anchorMin.x = value;
                anchorMax.x = value;
            }
            else
            {
                anchorMin.y = value;
                anchorMax.y = value;
            }

            self.anchorMin = anchorMin;
            self.anchorMax = anchorMax;
        }

        internal static void SetPivotAndAnchor(this RectTransform self, float value, int axis)
        {
            self.SetPivot(value, axis);
            self.SetAnchor(value, axis);
        }

        internal static void SetAnchoredPosition(this RectTransform self, float value, int axis)
        {
            var pos = self.anchoredPosition;
            if (axis == 0)
                pos.x = value;
            else
                pos.y = value;
            self.anchoredPosition = pos;
        }

        internal static void SetLocalPosition(this Transform self, float value, int axis)
        {
            var pos = self.localPosition;
            if (axis == 0)
                pos.x = value;
            else
                pos.y = value;
            pos[axis] = value;
            self.localPosition = pos;
        }

        internal static void SetCellPosition(this RectTransform self, float value, int axis)
        {
            var pos = self.localPosition;
            if (axis == 0)
                pos.x = (self.pivot.x - 0.5f) * self.rect.width + value;
            else
                pos.y = (self.pivot.y - 0.5f) * self.rect.height + value;
            self.localPosition = pos;
        }

        internal static void SetCellPosition(this RectTransform self, Vector2 value)
        {
            var pos = self.localPosition;
            pos.x = (self.pivot.x - 0.5f) * self.rect.width + value.x;
            pos.y = (self.pivot.y - 0.5f) * self.rect.height + value.y;
            self.localPosition = pos;
        }
    }
}