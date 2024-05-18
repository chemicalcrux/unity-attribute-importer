using UnityEditor;
using UnityEngine;

namespace ChemicalCrux.UVImporter
{
    [System.Serializable]
    public struct VertexColorTarget
    {
        public VertexColorComponent component;

        public int ComponentIndex
        {
            get => (int)component;
            set => component = (VertexColorComponent)value;
        }

        public Color ComponentColor => component switch
        {
            VertexColorComponent.R => Color.red,
            VertexColorComponent.G => Color.green,
            VertexColorComponent.B => Color.blue,
            VertexColorComponent.A => Color.white,
            _ => Color.white
        };
    }

    [CustomPropertyDrawer(typeof(VertexColorTarget))]
    public class VertexColorTargetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var componentProperty = property.FindPropertyRelative(nameof(VertexColorTarget.component));

            Color oldColor = GUI.backgroundColor;
            VertexColorTarget target = new VertexColorTarget() { component = (VertexColorComponent)componentProperty.enumValueIndex };

            GUI.backgroundColor = target.ComponentColor;

            EditorGUI.PropertyField(position, componentProperty, GUIContent.none, true);

            GUI.backgroundColor = oldColor;

            EditorGUI.EndProperty();
        }
    }
}