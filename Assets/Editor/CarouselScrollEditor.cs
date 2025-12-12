#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(CarouselScroll))]
    public class CarouselScrollEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.Add(new PropertyField(serializedObject.FindProperty("_scroller._axis")));
            container.Add(new PropertyField(serializedObject.FindProperty("_viewport")));
            container.Add(new PropertyField(serializedObject.FindProperty("_content")));
            container.Add(new PropertyField(serializedObject.FindProperty("_space")));
            container.Add(new PropertyField(serializedObject.FindProperty("_cellSize")));
            container.Add(new PropertyField(serializedObject.FindProperty("_loop")));
            container.Add(new PropertyField(serializedObject.FindProperty("_indicator")));
            container.Add(new ScrollerDrawer().CreatePropertyGUI(serializedObject.FindProperty("_scroller")));
            container.Add(new PropertyField(serializedObject.FindProperty("_scroller._onValueChanged")));
            return container;
        }
    }
}
#endif