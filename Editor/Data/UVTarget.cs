using UnityEditor;
using UnityEngine;

namespace ChemicalCrux.UVImporter
{
    [System.Serializable]
    public struct UVTarget
    {
        /// <summary>
        /// UV0, UV1, etc.
        /// </summary>
        public UVChannel channel;
        /// <summary>
        /// X, Y, Z, W
        /// </summary>
        public UVComponent component;

        public int ChannelIndex
        {
            get => (int)channel;
            set => channel = (UVChannel)value;
        }

        public int ComponentIndex
        {
            get => (int)component;
            set => component = (UVComponent)value;
        }

        public Color ChannelColor => channel switch
        {
            UVChannel.UV0 => Color.red,
            UVChannel.UV1 => Color.green,
            UVChannel.UV2 => Color.blue,
            UVChannel.UV3 => Color.white,
            _ => Color.white
        };

        public Color ComponentColor => component switch
        {
            UVComponent.X => Color.red,
            UVComponent.Y => Color.green,
            UVComponent.Z => Color.blue,
            UVComponent.W => Color.white,
            _ => Color.white
        };

        public override string ToString()
        {
            return channel + "." + component;
        }
    }

    [CustomPropertyDrawer(typeof(UVTarget))]
    public class UVTargetPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var channelProperty = property.FindPropertyRelative(nameof(UVTarget.channel));
            var componentProperty = property.FindPropertyRelative(nameof(UVTarget.component));

            Color oldColor = GUI.backgroundColor;
            UVTarget target = new UVTarget() { channel = (UVChannel)channelProperty.enumValueIndex, component = (UVComponent)componentProperty.enumValueIndex };

            Rect channelRect = new(position);
            channelRect.xMax = Mathf.Lerp(channelRect.xMin, channelRect.xMax, 0.5f);

            GUI.backgroundColor = Color.Lerp(target.ChannelColor, oldColor, 0.75f);

            EditorGUI.PropertyField(channelRect, channelProperty, GUIContent.none, true);

            Rect componentRect = new(position);
            componentRect.xMin = Mathf.Lerp(componentRect.xMin, componentRect.xMax, 0.5f);

            GUI.backgroundColor = target.ComponentColor;

            EditorGUI.PropertyField(componentRect, componentProperty, GUIContent.none, true);

            GUI.backgroundColor = oldColor;

            EditorGUI.EndProperty();
        }
    }
}