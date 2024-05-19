using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter
{
    [CustomEditor(typeof(DataImporter))]
    public class DataImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var array = serializedObject.FindProperty(nameof(DataImporter.attributeConfigs));

            for (int attributeIndex = 0; attributeIndex < array.arraySize; ++attributeIndex)
            {
                var attribute = array.GetArrayElementAtIndex(attributeIndex);

                var attributeNameProp = attribute.FindPropertyRelative(nameof(DataImporter.AttributeConfig.name));
                var existsProp = attribute.FindPropertyRelative(nameof(DataImporter.AttributeConfig.exists));

                if (!existsProp.boolValue)
                    continue;

                EditorGUILayout.BeginVertical();

                var targetArray = attribute.FindPropertyRelative("targets");

                Color oldColor = GUI.backgroundColor;

                GUILayout.Label(attributeNameProp.stringValue);
                GUILayout.Space(8);

                for (int targetIndex = 0; targetIndex < targetArray.arraySize; ++targetIndex)
                {
                    GUI.backgroundColor = targetIndex switch
                    {
                        0 => Color.red,
                        1 => Color.green,
                        2 => Color.blue,
                        3 => Color.gray,
                        _ => oldColor
                    };

                    var target = targetArray.GetArrayElementAtIndex(targetIndex);

                    var kindProp = target.FindPropertyRelative(nameof(AttributeTarget.kind));

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(targetIndex switch
                    {
                        0 => "Red",
                        1 => "Green",
                        2 => "Blue",
                        3 => "Alpha",
                        _ => ""
                    }, GUILayout.MinWidth(50));

                    GUILayout.Label(" -> ");

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.PropertyField(kindProp, GUIContent.none);
                    AttributeTarget.Kind kind = (AttributeTarget.Kind)kindProp.enumValueIndex;

                    GUI.backgroundColor = oldColor;

                    if (kind == AttributeTarget.Kind.UV)
                    {
                        var uvProp = target.FindPropertyRelative(nameof(AttributeTarget.uvTarget));
                        EditorGUILayout.PropertyField(uvProp);
                    }
                    else if (kind == AttributeTarget.Kind.VertexColor)
                    {
                        var vertexColorProp = target.FindPropertyRelative(nameof(AttributeTarget.vertexColorTarget));
                        EditorGUILayout.PropertyField(vertexColorProp);
                    }
                    else
                    {
                        GUILayout.FlexibleSpace();
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndHorizontal();
                }

                GUI.backgroundColor = oldColor;

                GUILayout.Space(16);

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }

        protected override bool OnApplyRevertGUI()
        {
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                RevertButton();
                using (new EditorGUI.DisabledScope(!CanApply()))
                {
                    return ApplyButton();
                }
            }
        }
    }
}
