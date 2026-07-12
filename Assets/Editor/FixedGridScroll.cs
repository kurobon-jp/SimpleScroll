#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(FixedGridScroll))]
    public class FixedGridScrollEditor : BaseScrollEditor
    {
        protected override void CreateInherentProperties(VisualElement container)
        {
            container.Add(new PropertyField(serializedObject.FindProperty("_cellSize")));
            container.Add(new PropertyField(serializedObject.FindProperty("_column")));
            container.Add(new PropertyField(serializedObject.FindProperty("_scrollbar")));
        }
    }
}
#endif