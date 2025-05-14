using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    public class BlendshapeSelector : EditorWindow
    {
        private static BlendshapeSelector Window;
        
        internal static void ShowWindow(IReadOnlyList<string> o, Action<int> s)
        {
            if (Window != null)
                return;
            Window = GetWindow<BlendshapeSelector>();
            Window.titleContent = new GUIContent("Content Builder");
            Window.options = o.ToArray();
            Window.OnSelected = s;
        }

        private string[] options;
        private Action<int> OnSelected = i => { };
        private string search;
        private Vector2 scroll;

        private void OnGUI()
        {
            if (Window != null)
            {
                search = EditorGUILayout.TextField("Search", search);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < options.Length; i++)
                {
                    string option = options[i];
                    if ((string.IsNullOrEmpty(search) || option.ToLower().Contains(search.ToLower())) && GUILayout.Button(option))
                    {
                        OnSelected.Invoke(i);
                        Close();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void OnDestroy()
        {
            Window = null;
        }
    }
}