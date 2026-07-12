#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(LazyLayoutListScroll))]
    public class LazyLayoutListScrollEditor : BaseScrollEditor
    {
        protected override void CreateInherentProperties(VisualElement container)
        {
            container.Add(new PropertyField(serializedObject.FindProperty("_defaultCellSize")));
            container.Add(new PropertyField(serializedObject.FindProperty("_scrollbar")));
        }
    }
}
#endif