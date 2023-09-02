using System;
using Hypernex.CCK.Editor.Editors.Tools;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    public class SimpleScriptEditor : EditorWindow
    {
        private Action requestSave;
        
        internal static void ShowWindow(NexboxScript script, Action requestSave)
        {
            SimpleScriptEditor Window = GetWindow<SimpleScriptEditor>();
            Window.titleContent = new GUIContent(script.Name);
            Window.script = script;
            Window.Text = script.Script;
            Window.requestSave = requestSave;
        }

        private NexboxScript script;
        private string Text;
        private Vector2 textScroll;

        private void OnGUI()
        {
            EditorTools.NewGUILine();
            textScroll = GUILayout.BeginScrollView(textScroll);
            Text = GUILayout.TextArea(Text, GUILayout.MaxHeight(Int32.MaxValue));
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save and Close"))
            {
                script.Script = Text;
                requestSave.Invoke();
                Close();
            }
            if (GUILayout.Button("Close without Saving"))
            {
                if (Text == script.Script || EditorUtility.DisplayDialog(script.Name,
                        "Are you sure you want to close without saving?", "Yes", "No"))
                    Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}