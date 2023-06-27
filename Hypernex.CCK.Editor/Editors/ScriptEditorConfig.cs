using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    public class ScriptEditorConfig : EditorWindow
    {
        [MenuItem("Hypernex.CCK/ScriptEditor Config")]
        private static void ShowWindow()
        {
            ScriptEditorConfig Window = GetWindow<ScriptEditorConfig>();
            Window.titleContent = new GUIContent("ScriptEditor Config");
        }

        private void OnGUI()
        {
            EditorConfig c = EditorConfig.GetConfig();
            GUILayout.Label("Hypernex Script Editor", EditorStyles.miniBoldLabel);
            if (!ScriptEditorInstance.IsOpen)
            {
                EditorConfig.LoadedConfig.ScriptEditorLocation =
                    EditorGUILayout.TextField("Script Editor Location",
                        EditorConfig.LoadedConfig.ScriptEditorLocation);
                if (GUILayout.Button("Select Script Editor"))
                    EditorConfig.LoadedConfig.ScriptEditorLocation =
                        EditorUtility.OpenFilePanel("Select Hypernex Script Editor", "", "");
                if (GUILayout.Button("Start Script Editor"))
                {
                    EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
                    ScriptEditorInstance.StartApp(EditorConfig.LoadedConfig.ScriptEditorLocation);
                    Close();
                }
            }
            else
                GUILayout.Label("Already Connected!", EditorStyles.miniLabel);
        }
    }
}