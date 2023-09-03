using System;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using UnityEditor;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Avatar;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(AssetIdentifier))]
    public class AssetIdentifierEditor : UnityEditor.Editor
    {
        private AssetIdentifier AssetIdentifier;
        private string lastId;

        private void OnEnable()
        {
            bool wasNull = AssetIdentifier == null;
            AssetIdentifier = target as AssetIdentifier;
            if (wasNull && AssetIdentifier != null)
            {
                lastId = AssetIdentifier.GetId();
                newId = lastId;
            }
        }

        private string newId;

        public override void OnInspectorGUI()
        {
            if (AssetIdentifier == null)
                return;
            if (AuthManager.Instance == null || AuthManager.CurrentUser == null)
            {
                GUILayout.Label("Please Login to manage Asset Identifiers!", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            EditorGUILayout.HelpBox(
                "An Asset Id is assigned by the server, and tells the server where to apply an uploaded asset.",
                MessageType.Info);
            newId = GUILayout.TextField(newId);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Remove Asset Identifier"))
            {
                lastId = String.Empty;
                newId = String.Empty;
                AssetIdentifier.SetId(String.Empty);
                EditorTools.MakeSave(AssetIdentifier);
                //EditorUtility.SetDirty(AssetIdentifier.gameObject);
            }
            if (GUILayout.Button("Apply Asset Identifier"))
            {
                bool contains = false;
                if (AssetIdentifier.gameObject.GetComponent<Avatar>() != null)
                    contains = AuthManager.CurrentUser.Avatars.Contains(newId);
                else if (AssetIdentifier.gameObject.GetComponent<World>() != null)
                    contains = AuthManager.CurrentUser.Worlds.Contains(newId);
                if (contains)
                {
                    lastId = newId;
                    AssetIdentifier.SetId(newId);
                    EditorTools.MakeSave(AssetIdentifier);
                    //EditorUtility.SetDirty(AssetIdentifier.gameObject);
                }
                else
                {
                    newId = lastId;
                    EditorUtility.DisplayDialog("Hypernex.CCK", "Invalid Id provided!", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
            //base.OnInspectorGUI();
        }
    }
}