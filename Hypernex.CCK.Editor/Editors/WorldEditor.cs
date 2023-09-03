using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using UnityEditor;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(World))]
    public class WorldEditor : UnityEditor.Editor
    {
        private SerializedProperty AllowRespawn;
        private SerializedProperty Gravity;
        private SerializedProperty JumpHeight;
        private SerializedProperty WalkSpeed;
        private SerializedProperty RunSpeed;
        private SerializedProperty AllowRunning;
        private SerializedProperty LockAvatarSwitching;
        private World World;

        private void OnEnable()
        {
            AllowRespawn = serializedObject.FindProperty("AllowRespawn");
            Gravity = serializedObject.FindProperty("Gravity");
            JumpHeight = serializedObject.FindProperty("JumpHeight");
            WalkSpeed = serializedObject.FindProperty("WalkSpeed");
            RunSpeed = serializedObject.FindProperty("RunSpeed");
            AllowRunning = serializedObject.FindProperty("AllowRunning");
            LockAvatarSwitching = serializedObject.FindProperty("LockAvatarSwitching");
            World = target as World;
        }

        private static bool ss = true;
        private static bool lrv;
        private static bool srv;
        private static bool sav;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            AllowRespawn.boolValue = EditorGUILayout.Toggle("Allow Respawn", AllowRespawn.boolValue);
            AllowRunning.boolValue = EditorGUILayout.Toggle("Allow Running", AllowRunning.boolValue);
            LockAvatarSwitching.boolValue =
                EditorGUILayout.Toggle("Lock Avatar Switching", LockAvatarSwitching.boolValue);
            EditorGUILayout.Separator();
            WalkSpeed.floatValue = EditorGUILayout.FloatField("Walk Speed", WalkSpeed.floatValue);
            RunSpeed.floatValue = EditorGUILayout.FloatField("Run Speed", RunSpeed.floatValue);
            JumpHeight.floatValue = EditorGUILayout.FloatField("Jump Height", JumpHeight.floatValue);
            Gravity.floatValue = EditorGUILayout.FloatField("Gravity", Gravity.floatValue);
            EditorGUILayout.Separator();
            EditorTools.DrawObjectList(ref World.SpawnPoints, "Spawn Points", ref ss,
                () => EditorUtility.SetDirty(World.gameObject), allowSceneObjects: true);
            EditorGUILayout.Separator();
            EditorTools.DrawSimpleList(ref World.LocalScripts, "Local Scripts", ref lrv,
                () => EditorUtility.SetDirty(World.gameObject),
                script =>
                {
                    if (string.IsNullOrEmpty(script.Name))
                        return script.Language + " Local Script";
                    return script.Name + script.GetExtensionFromLanguage();
                }, () => new NexboxScript(),
                (script, i) => EditorTools.DrawScriptEditorOnCustomEvent(World, ref script, i));
            EditorGUILayout.Separator();
            EditorTools.DrawSimpleList(ref World.ServerScripts, "Server Scripts", ref srv,
                () => EditorUtility.SetDirty(World.gameObject),
                script =>
                {
                    if (string.IsNullOrEmpty(script.Name))
                        return script.Language + " Script";
                    return script.Name + script.GetExtensionFromLanguage();
                }, () => new NexboxScript(),
                (script, i) => EditorTools.DrawScriptEditorOnCustomEvent(World, ref script, i));
            EditorGUILayout.Separator();
            EditorTools.DrawSimpleList(ref World.ScriptAssets, "Local Script Assets", ref sav,
                () => EditorUtility.SetDirty(World.gameObject), asset =>
                {
                    if (string.IsNullOrEmpty(asset.AssetName))
                        return "New Asset";
                    return asset.AssetName;
                }, () => new ScriptAsset());
            EditorGUILayout.Separator();
            serializedObject.ApplyModifiedProperties();
        }
    }
}