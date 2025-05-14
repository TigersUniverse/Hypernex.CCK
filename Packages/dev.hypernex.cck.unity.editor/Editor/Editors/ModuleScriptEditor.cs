using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Scripting;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(ModuleScript), true)]
    public class ModuleScriptEditor : UnityEditor.Editor
    {
        private ModuleScript ModuleScript;

        private void OnEnable() => ModuleScript = target as ModuleScript;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(false);
            bool key = EditorPrefs.HasKey("WorldServerScript");
            if (key)
            {
                WorldServerScripts w =
                    AssetDatabase.LoadAssetAtPath<WorldServerScripts>(EditorPrefs.GetString("WorldServerScript"));
                if(w != null)
                {
                    if (w.ServerScripts.Contains(ModuleScript))
                    {
                        if (GUILayout.Button("Remove from " + w.name))
                        {
                            w.ServerScripts = w.ServerScripts.Where(x => x != ModuleScript).ToArray();
                            EditorUtility.SetDirty(w);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Add to " + w.name))
                        {
                            List<ModuleScript> moduleScripts = w.ServerScripts.ToList();
                            moduleScripts.Add(ModuleScript);
                            w.ServerScripts = moduleScripts.ToArray();
                            EditorUtility.SetDirty(w);
                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.Label(ModuleScript.FileName);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(ModuleScript.Text);
            EditorGUI.EndDisabledGroup();
        }
    }
}