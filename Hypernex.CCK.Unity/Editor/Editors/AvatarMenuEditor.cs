using System.Linq;
using Hypernex.CCK.Unity.Assets;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(AvatarMenu))]
    public class AvatarMenuEditor : UnityEditor.Editor
    {
        private AvatarMenu AvatarMenu;
        private SerializedProperty Parameters;
        private SerializedProperty Controls;

        private ReorderableList ReorderableControls;
        private bool dropdownExpanded;

        private void OnEnable()
        {
            AvatarMenu = target as AvatarMenu;
            Parameters = serializedObject.FindProperty("Parameters");
            Controls = serializedObject.FindProperty("Controls");
            ReorderableControls = new ReorderableList(serializedObject, Controls, true, true, true, true);
            ReorderableControls.drawHeaderCallback += rect => EditorUtils.DrawReorderableListHeader(rect, "Controls");
            ReorderableControls.drawElementCallback += DrawMenuControl;
            ReorderableControls.elementHeightCallback += ElementHeightCallback;
        }

        private GUIContent[] GetOptionsFromParameters(AvatarParameters parameters) =>
            new[] {new GUIContent("None")}
                .Union(parameters.Parameters.Select(x => new GUIContent($"{x.ParameterName} ({x.ParameterType})")))
                .ToArray();

        private int GetIndexFromSelection(AvatarParameter parameter, ref AvatarParameters parameters)
        {
            for (int i = 0; i < parameters.Parameters.Length; i++)
            {
                AvatarParameter compare = parameters.Parameters[i];
                if(compare != parameter) continue;
                return i + 1;
            }
            return 0;
        }
        
        private float ElementHeightCallback(int index)
        {
            AvatarControl control = AvatarMenu.Controls[index];
            switch (control.ControlType)
            {
                case ControlType.Dropdown:
                    float mult = control.DropdownOptions.Length * 21f + (control.DropdownOptions.Length <= 0 ? 75f : 55f);
                    if (!dropdownExpanded) mult = 25;
                    if (control.TargetParameterIndex <= 0) return 125f + mult;
                    return 125f + mult;
                case ControlType.Toggle:
                case ControlType.Slider:
                    if (control.TargetParameterIndex <= 0) return 125f;
                    break;
                case ControlType.TwoDimensionalAxis:
                    if (control.TargetParameterIndex <= 0 || control.TargetParameterIndex2 <= 0) return 150f;
                    return 125f;
                case ControlType.SubMenu:
                    if (control.SubMenu == null) return 130f;
                    return 105f;
            }
            return 100f;
        }

        private void DrawMenuControl(Rect rect, int index, bool isactive, bool isfocused)
        {
            AvatarControl rawControl = AvatarMenu.Controls[index];
            SerializedProperty control = Controls.GetArrayElementAtIndex(index);
            SerializedProperty controlName = control.FindPropertyRelative("ControlName");
            SerializedProperty controlSprite = control.FindPropertyRelative("ControlSprite");
            Rect current = new Rect(rect.x, rect.y + 5, rect.width, 25);
            SerializedProperty targetControl = control.FindPropertyRelative("ControlType");
            SerializedProperty targetParameter = control.FindPropertyRelative("TargetParameterIndex");
            SerializedProperty targetParameter2 = control.FindPropertyRelative("TargetParameterIndex2");
            EditorGUI.PropertyField(current, targetControl, new GUIContent("Control"));
            current.y += 25;
            EditorGUI.PropertyField(new Rect(current.x, current.y, current.width, 20), controlName, new GUIContent("Control Name"));
            current.y += 25;
            EditorGUI.PropertyField(new Rect(current.x, current.y, current.width, 20), controlSprite, new GUIContent("Control Icon"));
            current.y += 25;
            switch ((ControlType) targetControl.enumValueIndex)
            {
                case ControlType.Toggle:
                case ControlType.Slider:
                {
                    if (targetParameter.intValue <= 0)
                    {
                        EditorGUI.HelpBox(current, $"No Parameter is set! This {((ControlType) targetControl.enumValueIndex == ControlType.Toggle ? "toggle" : "slider")} will effectively do nothing.",
                            MessageType.Warning);
                        current.y += 25f;
                    }
                    EditorGUI.PrefixLabel(current, new GUIContent("Parameter"));
                    int selection = EditorGUI.Popup(new Rect(current.x + 100, current.y, current.width - 100, current.height),
                        rawControl.TargetParameterIndex,
                        GetOptionsFromParameters(AvatarMenu.Parameters));
                    targetParameter.intValue = selection;
                    break;
                }
                case ControlType.Dropdown:
                {
                    if (targetParameter.intValue <= 0)
                    {
                        EditorGUI.HelpBox(current, "No Parameter is set! This dropdown will effectively do nothing.",
                            MessageType.Warning);
                        current.y += 25f;
                    }
                    SerializedProperty dropdownOptions = control.FindPropertyRelative("DropdownOptions");
                    EditorGUI.PropertyField(current, dropdownOptions);
                    dropdownExpanded = dropdownOptions.isExpanded;
                    current.y += dropdownExpanded ? (21f * rawControl.DropdownOptions.Length) + (rawControl.DropdownOptions.Length <= 0 ? 70f : 50f) : 25f;
                    EditorGUI.PrefixLabel(current, new GUIContent("Parameter"));
                    int selection = EditorGUI.Popup(new Rect(current.x + 100, current.y, current.width - 100, current.height),
                        rawControl.TargetParameterIndex,
                        GetOptionsFromParameters(AvatarMenu.Parameters));
                    targetParameter.intValue = selection;
                    break;
                }
                case ControlType.TwoDimensionalAxis:
                {
                    if (targetParameter.intValue <= 0 || targetParameter2.intValue <= 0)
                    {
                        EditorGUI.HelpBox(current, "Parameters are not set correctly! This axis may not do anything.",
                            MessageType.Warning);
                        current.y += 25f;
                    }
                    EditorGUI.PrefixLabel(current, new GUIContent("X Parameter"));
                    int selection1 = EditorGUI.Popup(new Rect(current.x + 100, current.y, current.width - 100, current.height),
                        rawControl.TargetParameterIndex,
                        GetOptionsFromParameters(AvatarMenu.Parameters));
                    targetParameter.intValue = selection1;
                    current.y += 25f;
                    EditorGUI.PrefixLabel(current, new GUIContent("Y Parameter"));
                    int selection2 = EditorGUI.Popup(new Rect(current.x + 100, current.y, current.width - 100, current.height),
                        rawControl.TargetParameterIndex2,
                        GetOptionsFromParameters(AvatarMenu.Parameters));
                    targetParameter2.intValue = selection2;
                    break;
                }
                case ControlType.SubMenu:
                {
                    SerializedProperty submenu = control.FindPropertyRelative("SubMenu");
                    if (submenu.objectReferenceValue == null)
                    {
                        EditorGUI.HelpBox(current, "No valid SubMenu! This control will not render.",
                            MessageType.Error);
                        current.y += 25f;
                    }
                    EditorGUI.PropertyField(current, submenu, new GUIContent("SubMenu"));
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(Parameters, new GUIContent("Avatar Parameters"));
            ReorderableControls.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}