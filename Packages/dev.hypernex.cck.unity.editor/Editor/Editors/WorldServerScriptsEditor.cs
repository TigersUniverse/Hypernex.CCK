using System;
using Hypernex.CCK.Unity.Assets;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(WorldServerScripts))]
    public class WorldServerScriptsEditor : UnityEditor.Editor
    {
        private WorldServerScripts WorldServerScripts;
        
        private SerializedProperty ServerScripts;

        private void OnEnable()
        {
            WorldServerScripts = target as WorldServerScripts;
            ServerScripts = serializedObject.FindProperty("ServerScripts");
        }
        
        private (int, string) IsTestLocal()
        {
            string a = EditorPrefs.GetString("WorldServerScript");
            string b = AssetDatabase.GetAssetPath(this);
            return a == b
                ? (1, "You are testing this script.")
                : (2, "You are not testing this script!");
        }

        private (int, string) IsTestingAny()
        {
            bool t = EditorPrefs.HasKey("WorldServerScript");
            if(!t) return (3, "You are not testing any ServerScripts!");
            WorldServerScripts w = AssetDatabase.LoadAssetAtPath<WorldServerScripts>(EditorPrefs.GetString("WorldServerScript"));
            if(w == null) return (3, "You are not testing any ServerScripts!");
            return (1, $"You are testing {w.name}");
        }

        private void Draw(Rect r, int s, string msg, string bt, Action click)
        {
            MessageType leftMessageType = (MessageType) s;
            r.height = 30;
            EditorGUI.HelpBox(r, msg, leftMessageType);
            r.y += r.height;
            r.height = 18;
            if(GUI.Button(r, bt))
                click.Invoke();
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.PropertyField(ServerScripts, "Server Scripts");
            if (EditorApplication.isPlaying) return;
#if HYPERNEX_CCK_EMULATOR
            EditorGUILayout.BeginHorizontal();
            (int, string) lstate = IsTestLocal();
            Rect rl = EditorGUILayout.GetControlRect(false, 48);
            Draw(rl, lstate.Item1, lstate.Item2, "Test This Script!",
                () => EditorPrefs.SetString("WorldServerScript", AssetDatabase.GetAssetPath(WorldServerScripts)));
            (int, string) rstate = IsTestingAny();
            Rect rr = EditorGUILayout.GetControlRect(false, 48);
            Draw(rr, rstate.Item1, rstate.Item2, "Stop Testing All Scripts",
                () => EditorPrefs.DeleteKey("WorldServerScript"));
            EditorGUILayout.EndHorizontal();
#endif
            serializedObject.ApplyModifiedProperties();
        }
    }
}