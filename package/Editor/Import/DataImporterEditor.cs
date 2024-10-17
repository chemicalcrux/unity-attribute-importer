using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ChemicalCrux.AttributeImporter
{
    [CustomEditor(typeof(DataImporter))]
    public class DataImporterEditor : ScriptedImporterEditor
    {
        void SetColors(SerializedProperty targetArray)
        {
            for (int targetIndex = 0; targetIndex < targetArray.arraySize; ++targetIndex)
            {
                var targetProp = targetArray.GetArrayElementAtIndex(targetIndex);
                var kindProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.kind));
                var vertexColorProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.vertexColorTarget));

                var vertexColorComponentProp = vertexColorProp.FindPropertyRelative(nameof(VertexColorTarget.component));

                kindProp.enumValueIndex = (int) AttributeTarget.Kind.VertexColor;
                vertexColorComponentProp.enumValueIndex = targetIndex;
            }
        }

        void SetUVs(SerializedProperty targetArray, int channel)
        {
            for (int targetIndex = 0; targetIndex < targetArray.arraySize; ++targetIndex)
            {
                var targetProp = targetArray.GetArrayElementAtIndex(targetIndex);
                var kindProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.kind));
                var uvProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.uvTarget));

                var vertexColorChannelProp = uvProp.FindPropertyRelative(nameof(UVTarget.channel));
                var vertexColorComponentProp = uvProp.FindPropertyRelative(nameof(UVTarget.component));

                kindProp.enumValueIndex = (int) AttributeTarget.Kind.UV;
                vertexColorChannelProp.enumValueIndex = channel;
                vertexColorComponentProp.enumValueIndex = targetIndex;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var array = serializedObject.FindProperty(nameof(DataImporter.attributeConfigs));

            for (int attributeIndex = 0; attributeIndex < array.arraySize; ++attributeIndex)
            {
                var attributeProp = array.GetArrayElementAtIndex(attributeIndex);

                var attributeNameProp = attributeProp.FindPropertyRelative(nameof(DataImporter.AttributeConfig.name));
                var existsProp = attributeProp.FindPropertyRelative(nameof(DataImporter.AttributeConfig.exists));

                if (!existsProp.boolValue)
                    continue;

                EditorGUILayout.BeginVertical();

                var targetArray = attributeProp.FindPropertyRelative("targets");

                Color oldColor = GUI.backgroundColor;

                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(attributeNameProp.stringValue);

                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(150));

                if (GUILayout.Button("Color"))
                {
                    SetColors(targetArray);
                }
                for (int i = 0; i < 4; ++i)
                {
                    if (GUILayout.Button($"UV{i}"))
                    {
                        SetUVs(targetArray, i);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
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

                    var targetProp = targetArray.GetArrayElementAtIndex(targetIndex);

                    var kindProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.kind));

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
                        var uvProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.uvTarget));
                        EditorGUILayout.PropertyField(uvProp);
                    }
                    else if (kind == AttributeTarget.Kind.VertexColor)
                    {
                        var vertexColorProp = targetProp.FindPropertyRelative(nameof(AttributeTarget.vertexColorTarget));
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

                var transformerProp = attributeProp.FindPropertyRelative(nameof(DataImporter.AttributeConfig.transformers));
                EditorGUILayout.PropertyField(transformerProp);
                
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
