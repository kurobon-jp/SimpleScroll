#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SimpleScroll.Editor
{
    [CustomPropertyDrawer(typeof(Scroller))]
    public class ScrollerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new Foldout();
            container.text = "<b>Dragging</b>";

            // var axis = new PropertyField(property.FindPropertyRelative("_axis"));
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
                field.value = Mathf.Clamp(field.value, 1f, float.MaxValue);
            });
            
            inertia.RegisterValueChangeCallback(e =>
            {
                var enabled = e.changedProperty.boolValue;
                deceleration.SetEnabled(enabled);
                maxVelocity.SetEnabled(enabled);
            }); 

            // container.Add(axis);
            container.Add(inertia);
            container.Add(deceleration);
            container.Add(maxVelocity);
            container.Add(dragSensitivity);
            container.Add(overScroll);
            return container;
        }
    }
}
#endif