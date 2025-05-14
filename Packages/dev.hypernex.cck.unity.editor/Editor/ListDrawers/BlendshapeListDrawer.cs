using System;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Editor.Editors;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.ListDrawers
{
    public static class BlendshapeListDrawer
    {
        public static Action<SerializedProperty, int, int> OnListCallback = (target, index, v) => { };
        
        public static string[] PopupOptions(BlendshapeDescriptor[] descriptors)
        {
            string[] contents = new string[descriptors.Length + 1];
            contents[0] = "None";
            for (int i = 1; i < contents.Length; i++)
            {
                BlendshapeDescriptor descriptor = descriptors[i - 1];
                contents[i] = descriptor.MatchString;
            }
            return contents;
        }

        public static float GetListHeight(int _) => 20f;

        public static void DrawList<T>(SerializedProperty targetDescriptors, BlendshapeDescriptor[] descriptors, Rect rect, int index, bool isactive, bool isfocused, int overrideWidth = 100) where T : Enum
        {
            string enumName = Enum.GetName(typeof(T), index);
            SerializedProperty blendshapeDescriptor = targetDescriptors.GetArrayElementAtIndex(index);
            string[] options = PopupOptions(descriptors);
            EditorGUI.PrefixLabel(rect, new GUIContent(enumName));
            if (GUI.Button(new Rect(rect.x + overrideWidth, rect.y, rect.width - overrideWidth, rect.height),
                    options[blendshapeDescriptor.intValue]))
                BlendshapeSelector.ShowWindow(options, i => OnListCallback.Invoke(targetDescriptors, index, i));
            //blendshapeDescriptor.intValue = EditorGUI.Popup(rect, new GUIContent(enumName), blendshapeDescriptor.intValue, PopupOptions(descriptors));
        }

        public static void DrawLayoutReference<T>(SerializedProperty target, BlendshapeDescriptor[] descriptors, T e, Action<int> outputIndex, int overrideWidth = 100) where T : Enum
        {
            string enumName = Enum.GetName(typeof(T), e);
            string[] options = PopupOptions(descriptors);
            EditorGUILayout.BeginHorizontal();
            Rect r = EditorGUILayout.GetControlRect(true, 20f);
            GUI.Label(r, enumName);
            if(GUI.Button(new Rect(r.x + overrideWidth, r.y, r.width - overrideWidth, r.height), options[target.intValue]))
                BlendshapeSelector.ShowWindow(options, outputIndex);
            EditorGUILayout.EndHorizontal();
        }
    }
}