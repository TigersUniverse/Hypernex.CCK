using Hypernex.CCK.Unity;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(RespawnableDescriptor))]
    public class RespawnableDescriptorEditor : UnityEditor.Editor
    {
        private RespawnableDescriptor RespawnableDescriptor;
        private SerializedProperty LowestPointRespawnThreshold;

        private void OnEnable()
        {
            RespawnableDescriptor = target as RespawnableDescriptor;
            LowestPointRespawnThreshold = serializedObject.FindProperty("LowestPointRespawnThreshold");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            NetworkSyncDescriptor networkSyncDescriptor =
                RespawnableDescriptor.gameObject.GetComponent<NetworkSyncDescriptor>();
            if (networkSyncDescriptor != null && !(networkSyncDescriptor.InstanceHostOnly || networkSyncDescriptor.AlwaysSync))
                EditorGUILayout.HelpBox("A NetworkSync without InstanceHostOnly or AlwaysSync was detected on the GameObject. " +
                                        "This will cause Desync unless NetworkSync Authority is granted somehow.",
                    MessageType.Error);
            LowestPointRespawnThreshold.floatValue =
                EditorGUILayout.FloatField(
                    new GUIContent("LowestPointRespawnThreshold",
                        "How far below the lowest point until the GameObject is respawned. This value should be greater than 0."),
                    LowestPointRespawnThreshold.floatValue);
            serializedObject.ApplyModifiedProperties();
        }
    }
}