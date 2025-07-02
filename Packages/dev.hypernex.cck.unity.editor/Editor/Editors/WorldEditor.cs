using System;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(World))]
    public class WorldEditor : UnityEditor.Editor
    {
        private World World;
        
        private SerializedProperty AllowRespawn;
        private SerializedProperty Gravity;
        private SerializedProperty JumpHeight;
        private SerializedProperty WalkSpeed;
        private SerializedProperty RunSpeed;
        private SerializedProperty AllowRunning;
        private SerializedProperty AllowScaling;
        private SerializedProperty LockAvatarSwitching;
        private SerializedProperty SpawnPoints;
        private SerializedProperty ScriptAssets;
        
        private (int, string) ValidateSpawnPoints()
        {
            if (World.SpawnPoints.Count <= 0)
                return (1, "No SpawnPoints specified. This GameObject will be the default.");
            foreach (GameObject spawnPoint in World.SpawnPoints)
            {
                if(spawnPoint == null)
                    return (3, "SpawnPoint cannot be null!");
            }
            return (0, String.Empty);
        }

        private (int, string) ValidateNames()
        {
            string[] names = World.ScriptAssets.Select(x => x.AssetName).ToArray();
            foreach (ScriptAsset scriptAsset in ScriptAssets)
            {
                if(string.IsNullOrEmpty(scriptAsset.AssetName))
                    return (3, "AssetName cannot be empty!");
                int c = 0;
                foreach (string s in names)
                {
                    if (scriptAsset.AssetName == s)
                        c++;
                }
                if(c > 1) return (3, "AssetNames cannot be the same!");
            }
            return (0, String.Empty);
        }

        private void OnEnable()
        {
            World = target as World;
            AllowRespawn = serializedObject.FindProperty("AllowRespawn");
            Gravity = serializedObject.FindProperty("Gravity");
            JumpHeight = serializedObject.FindProperty("JumpHeight");
            WalkSpeed = serializedObject.FindProperty("WalkSpeed");
            RunSpeed = serializedObject.FindProperty("RunSpeed");
            AllowRunning = serializedObject.FindProperty("AllowRunning");
            AllowScaling = serializedObject.FindProperty("AllowScaling");
            LockAvatarSwitching = serializedObject.FindProperty("LockAvatarSwitching");
            SpawnPoints = serializedObject.FindProperty("SpawnPoints");
            ScriptAssets = serializedObject.FindProperty("ScriptAssets");
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawTitle("World Settings");
            EditorUtils.PropertyField(AllowRespawn, "Allow Respawn");
            EditorUtils.PropertyField(Gravity, "Gravity");
            EditorUtils.PropertyField(JumpHeight, "Jump Height");
            EditorUtils.PropertyField(WalkSpeed, "Walk Speed");
            EditorUtils.PropertyField(RunSpeed, "Run Speed");
            EditorUtils.PropertyField(AllowRunning, "Allow Running");
            EditorUtils.PropertyField(AllowScaling, "Allow Scaling");
            EditorUtils.PropertyField(LockAvatarSwitching, "Lock Avatar Switching");
            (int, string) validSpawns = ValidateSpawnPoints();
            if (validSpawns.Item1 > 0)
                EditorUtils.DrawSpecialHelpBox((MessageType) validSpawns.Item1, validSpawns.Item2);
            EditorUtils.PropertyField(SpawnPoints, "Spawn Points");
            EditorGUILayout.Space();
            EditorUtils.DrawTitle("Scripting");
            (int, string) validNames = ValidateNames();
            if(validNames.Item1 > 0)
                EditorUtils.DrawSpecialHelpBox((MessageType) validNames.Item1, validNames.Item2);
            EditorUtils.PropertyField(ScriptAssets, "Script Assets");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
