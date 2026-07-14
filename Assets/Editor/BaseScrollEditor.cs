using UnityEditor.UIElements;
using UnityEngine;
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
            CreateScrollingProperties(container);
            CreateDraggingProperties(container);
            container.Add(new PropertyField(serializedObject.FindProperty("_scroller._onValueChanged")));
            return container;
        }

        protected virtual void CreateInherentProperties(VisualElement container)
        {
        }

        protected virtual void CreateScrollingProperties(VisualElement container)
        {
            var foldout = new Foldout
            {
                text = "<b>Scrolling</b>",
                value = false
            };
            var isScrollable = new PropertyField(serializedObject.FindProperty("_isScrollable"), "Scrollable");
            isScrollable.RegisterValueChangeCallback(evt => { foldout.SetEnabled(evt.changedProperty.boolValue); });
            container.Add(isScrollable);
            container.Add(foldout);
            var property = serializedObject.FindProperty("_scroller");
            var scrollSensitivity = new PropertyField(property.FindPropertyRelative("_scrollSensitivity"));
            foldout.Add(scrollSensitivity);
        }

        protected virtual void CreateDraggingProperties(VisualElement container)
        {
            var foldout = new Foldout
            {
                text = "<b>Dragging</b>",
                value = false
            };
            var isDraggable = new PropertyField(serializedObject.FindProperty("_isDraggable"), "Draggable");
            isDraggable.RegisterValueChangeCallback(evt => { foldout.SetEnabled(evt.changedProperty.boolValue); });
            container.Add(isDraggable);
            container.Add(foldout);
            var property = serializedObject.FindProperty("_scroller");
            var inertia = new PropertyField(property.FindPropertyRelative("_inertia"));
            var deceleration = new PropertyField(property.FindPropertyRelative("_deceleration"));
            var maxVelocity = new PropertyField(property.FindPropertyRelative("_maxVelocity"));
            var dragSensitivity = new PropertyField(property.FindPropertyRelative("_dragSensitivity"));
            var overScroll = new PropertyField(property.FindPropertyRelative("_overScroll"));
            deceleration.RegisterValueChangeCallback(e =>
            {
                var field = deceleration.Q<FloatField>();
                field.value = Mathf.Clamp(field.value, 1f, float.MaxValue);
            });

            maxVelocity.RegisterValueChangeCallback(e =>
            {
                var field = maxVelocity.Q<FloatField>();
                field.value = Mathf.Clamp(field.value, 10f, float.MaxValue);
            });

            inertia.RegisterValueChangeCallback(e =>
            {
                var enabled = e.changedProperty.boolValue;
                deceleration.SetEnabled(enabled);
                maxVelocity.SetEnabled(enabled);
            });

            foldout.Add(inertia);
            foldout.Add(deceleration);
            foldout.Add(maxVelocity);
            foldout.Add(dragSensitivity);
            foldout.Add(overScroll);
        }
    }
}