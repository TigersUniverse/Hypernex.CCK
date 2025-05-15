using Hypernex.CCK.Unity.Assets;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(AvatarParameters))]
    public class AvatarParametersEditor : UnityEditor.Editor
    {
        private AvatarParameters avatarParameters;
        private SerializedProperty Parameters;

        private ReorderableList ReorderableParameters;

        private void OnEnable()
        {
            avatarParameters = target as AvatarParameters;
            Parameters = serializedObject.FindProperty("Parameters");
            ReorderableParameters = new ReorderableList(serializedObject, Parameters, true, true, true, true);
            ReorderableParameters.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Parameters");
            ReorderableParameters.drawElementCallback += DrawMenuControl;
            ReorderableParameters.elementHeightCallback += ElementHeightCallback;
            ReorderableParameters.onAddCallback = (ReorderableList list) =>
            {
                int index = Parameters.arraySize;
                Parameters.arraySize++;
                list.index = index;

                SerializedProperty element = Parameters.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("ParameterName").stringValue = "New Parameter";
                element.FindPropertyRelative("ParameterType").enumValueIndex = 0;
                element.FindPropertyRelative("DefaultBoolValue").boolValue = false;
                element.FindPropertyRelative("DefaultFloatValue").floatValue = 0f;
                element.FindPropertyRelative("DefaultIntValue").intValue = 0;
                element.FindPropertyRelative("Saved").boolValue = false;
                element.FindPropertyRelative("IsNetworked").boolValue = false;

                serializedObject.ApplyModifiedProperties();
            };
        }
        
        private float ElementHeightCallback(int index)
        {
            AvatarParameter avatarParameter = avatarParameters.Parameters[index];
            return (avatarParameter.ParameterType == AnimatorControllerParameterType.Trigger ? 4 : 5) * 25;
        }

        private void DrawMenuControl(Rect rect, int index, bool isactive, bool isfocused)
        {
            SerializedProperty parameter = Parameters.GetArrayElementAtIndex(index);
            SerializedProperty parameterType = parameter.FindPropertyRelative("ParameterType");
            SerializedProperty parameterName = parameter.FindPropertyRelative("ParameterName");
            SerializedProperty defaultBoolValue = parameter.FindPropertyRelative("DefaultBoolValue");
            SerializedProperty defaultFloatValue = parameter.FindPropertyRelative("DefaultFloatValue");
            SerializedProperty defaultIntValue = parameter.FindPropertyRelative("DefaultIntValue");
            SerializedProperty saved = parameter.FindPropertyRelative("Saved");
            SerializedProperty networked = parameter.FindPropertyRelative("IsNetworked");
            Rect current = new Rect(rect.x, rect.y + 5, rect.width, 20);
            EditorGUI.PropertyField(current, parameterName, new GUIContent("Name"));
            current.y += 25;
            EditorGUI.PropertyField(current, parameterType, new GUIContent("Type"));
            switch (parameterType.enumValueIndex)
            {
                case 0:
                    current.y += 25;
                    EditorGUI.PropertyField(current, defaultFloatValue, new GUIContent("Default"));
                    break;
                case 1:
                    current.y += 25;
                    EditorGUI.PropertyField(current, defaultIntValue, new GUIContent("Default"));
                    break;
                case 2:
                    current.y += 25;
                    EditorGUI.PropertyField(current, defaultBoolValue, new GUIContent("Default"));
                    break;
                case 3:
                    break;
            }
            current.y += 25;
            EditorGUI.PropertyField(current, saved, new GUIContent("Saved"));
            current.y += 25;
            EditorGUI.PropertyField(current, networked, new GUIContent("Network"));
        }

        public override void OnInspectorGUI()
        {
            ReorderableParameters.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(avatarParameters);
        }
    }
}