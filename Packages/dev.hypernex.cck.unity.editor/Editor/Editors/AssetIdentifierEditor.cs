using System;
using Hypernex.CCK.Auth;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Auth;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Libs.Editor;
using UnityEditor;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(AssetIdentifier))]
    public class AssetIdentifierEditor : UnityEditor.Editor
    {
        private AssetIdentifier AssetIdentifier;

        private SerializedProperty Id;

        private string UserInputId;
        private string updatedId;

        private async void SetIdentifier()
        {
            bool valid = await UserAuth.Instance.IsValidAsset(UserInputId);
            if(!valid)
            {
                EditorUtility.DisplayDialog("Hypernex.CCK.Unity", "Id is not valid!", "OK");
                return;
            }
            updatedId = UserInputId;
        }
        
        private (int, string) ValidateIdInput()
        {
            if (string.IsNullOrEmpty(Id.stringValue))
                return (1,
                    "Asset Id is empty/uninitialized. This means a new asset will be created upon uploading.");
            return (0, String.Empty);
        }
        
        private (int, string) ValidateLogin()
        {
            if (UserAuth.Instance == null || !UserAuth.Instance.IsAuth)
                return (3, "Please login to update asset identifiers!");
            return (0, String.Empty);
        }

        private void OnEnable()
        {
            AssetIdentifier = target as AssetIdentifier;
            Id = serializedObject.FindProperty("Id");
            UserInputId = Id.stringValue;
            GameObject gameObject = AssetIdentifier!.gameObject;
            World w = gameObject.GetComponent<World>();
            Avatar a = gameObject.GetComponent<Avatar>();
            LocalScript l = gameObject.GetComponent<LocalScript>();
            FaceTrackingDescriptor f = gameObject.GetComponent<FaceTrackingDescriptor>();
            GrabbableDescriptor g = gameObject.GetComponent<GrabbableDescriptor>();
            NetworkSyncDescriptor n = gameObject.GetComponent<NetworkSyncDescriptor>();
            RespawnableDescriptor r = gameObject.GetComponent<RespawnableDescriptor>();
            VideoPlayerDescriptor v = gameObject.GetComponent<VideoPlayerDescriptor>();
            if(w != null)
                AssetIdentifier.MoveComponent(w, false);
            if(a != null)
                AssetIdentifier.MoveComponent(a, false);
            if(l != null)
                AssetIdentifier.MoveComponent(l, false);
            if(f != null)
                AssetIdentifier.MoveComponent(f, false);
            if(g != null)
                AssetIdentifier.MoveComponent(g, false);
            if(n != null)
                AssetIdentifier.MoveComponent(n, false);
            if(r != null)
                AssetIdentifier.MoveComponent(r, false);
            if(v != null)
                AssetIdentifier.MoveComponent(v, false);
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawTitle("Asset Identifier");
            EditorGUILayout.Space();
            (int, string) validId = ValidateIdInput();
            if(validId.Item1 > 0)
                EditorUtils.DrawSpecialHelpBox((MessageType) validId.Item1, validId.Item2, 32f);
            UserInputId = EditorGUILayout.TextField(UserInputId);
            EditorGUILayout.Space();
            (int, string) validLogin = ValidateLogin();
            bool loggedIn = UserAuth.Instance != null && UserAuth.Instance.IsAuth;
            if (validLogin.Item1 > 0)
                EditorUtils.DrawSpecialHelpBox((MessageType) validLogin.Item1, validLogin.Item2);
            if(!loggedIn) EditorGUI.BeginDisabledGroup(true);
            if(GUILayout.Button("Update Identifier"))
                SetIdentifier();
            if(!loggedIn) EditorGUI.EndDisabledGroup();
            if(GUILayout.Button("Remove Identifier"))
            {
                Id.stringValue = String.Empty;
                UserInputId = String.Empty;
                updatedId = String.Empty;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
            }
            if (!string.IsNullOrEmpty(updatedId))
            {
                Id.stringValue = updatedId;
                updatedId = String.Empty;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}