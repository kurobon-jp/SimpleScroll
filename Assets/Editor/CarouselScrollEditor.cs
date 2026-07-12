#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(CarouselScroll))]
    public class CarouselScrollEditor : BaseScrollEditor
    {
        protected override void CreateInherentProperties(VisualElement container)
        {
            container.Add(new PropertyField(serializedObject.FindProperty("_cellSize")));
            container.Add(new PropertyField(serializedObject.FindProperty("_loop")));
            container.Add(new PropertyField(serializedObject.FindProperty("_indicator")));
        }
    }
}
#endif