using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    public abstract class BaseScrollEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var container = new VisualElement();
            container.Add(new PropertyField(serializedObject.FindProperty("_scroller._axis")));
            container.Add(new PropertyField(serializedObject.FindProperty("_viewport")));
            container.Add(new PropertyField(serializedObject.FindProperty("_content")));
            container.Add(new PropertyField(serializedObject.FindProperty("_contentPadding")));
            container.Add(new PropertyField(serializedObject.FindProperty("_space")));
            CreateInherentProperties(container);
            container.Add(new PropertyField(serializedObject.FindProperty("_isScrollable"), "Scrollable"));
            var isDraggable = new PropertyField(serializedObject.FindProperty("_isDraggable"), "Draggable");
            var scroller = new ScrollerDrawer().CreatePropertyGUI(serializedObject.FindProperty("_scroller"));
            isDraggable.RegisterValueChangeCallback(evt => { scroller.SetEnabled(evt.changedProperty.boolValue); });
            container.Add(isDraggable);
            container.Add(scroller);
            container.Add(new PropertyField(serializedObject.FindProperty("_scroller._onValueChanged")));
            return container;
        }

        protected virtual void CreateInherentProperties(VisualElement container)
        {
        }
    }
}