#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomEditor(typeof(SizedListScroll))]
    public class SizedListScrollEditor : BaseScrollEditor
    {
        protected override void CreateInherentProperties(VisualElement container)
        {
            container.Add(new PropertyField(serializedObject.FindProperty("_scrollbar")));
        }
    }
}
#endif