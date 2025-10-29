#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(FixedListScroll))]
    public class FixedListScrollEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.Add(new PropertyField(serializedObject.FindProperty("_viewport")));
            container.Add(new PropertyField(serializedObject.FindProperty("_content")));
            container.Add(new PropertyField(serializedObject.FindProperty("_contentPadding")));
            container.Add(new PropertyField(serializedObject.FindProperty("_space")));
            container.Add(new PropertyField(serializedObject.FindProperty("_cellSize")));
            container.Add(new PropertyField(serializedObject.FindProperty("_scrollbar")));
            container.Add(new ScrollerDrawer().CreatePropertyGUI(serializedObject.FindProperty("_scroller")));
            return container;
        }
    }
}
#endif