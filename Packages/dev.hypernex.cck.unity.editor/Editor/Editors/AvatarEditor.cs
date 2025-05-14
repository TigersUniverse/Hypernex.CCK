using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Editor.ListDrawers;
using Hypernex.CCK.Unity.Interaction;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(Avatar))]
    public class AvatarEditor : UnityEditor.Editor
    {
        private static readonly string[] EyeBoneOptions = {"Blendshapes", "Bones"};
        
        private Avatar Avatar;
        
        private SerializedProperty ViewPosition;
        private SerializedProperty SpeechPosition;

        private SerializedProperty UseEyeManager;
        private SerializedProperty EyeRenderers;
        private SerializedProperty UseLeftEyeBoneInstead;
        private SerializedProperty LeftEyeBlendshapes;
        private ReorderableList LeftEyeBlendshapesList;
        private SerializedProperty LeftEyeBone;
        private SerializedProperty LeftEyeUpLimit;
        private SerializedProperty LeftEyeDownLimit;
        private SerializedProperty LeftEyeLeftLimit;
        private SerializedProperty LeftEyeRightLimit;

        private SerializedProperty UseRightEyeBoneInstead;
        private SerializedProperty RightEyeBlendshapes;
        private ReorderableList RightEyeBlendshapesList;
        private SerializedProperty RightEyeBone;
        private SerializedProperty RightEyeUpLimit;
        private SerializedProperty RightEyeDownLimit;
        private SerializedProperty RightEyeLeftLimit;
        private SerializedProperty RightEyeRightLimit;

        private SerializedProperty UseCombinedEyeBlendshapes;
        private SerializedProperty EyeBlendshapes;
        private ReorderableList EyeBlendshapesList;

        private SerializedProperty UseVisemes;
        private SerializedProperty VisemeRenderers;
        private SerializedProperty VisemesDict;
        private ReorderableList VisemesList;

        private SerializedProperty Animators;
        private ReorderableList AnimatorsList;

        private SerializedProperty RootMenu;
        private SerializedProperty Parameters;

        private Queue<(SerializedProperty, int, int)> waitingProperties = new Queue<(SerializedProperty, int, int)>();
        
        private void OnEnable()
        {
            Avatar = target as Avatar;
            if (Avatar == null)
                throw new NullReferenceException("Avatar Component cannot be null!");
            ViewPosition = serializedObject.FindProperty("ViewPosition");
            SpeechPosition = serializedObject.FindProperty("SpeechPosition");
            UseEyeManager = serializedObject.FindProperty("UseEyeManager");
            EyeRenderers = serializedObject.FindProperty("EyeRenderers");
            UseLeftEyeBoneInstead = serializedObject.FindProperty("UseLeftEyeBoneInstead");
            LeftEyeBlendshapes = serializedObject.FindProperty("LeftEyeBlendshapes");
            LeftEyeBone = serializedObject.FindProperty("LeftEyeBone");
            LeftEyeUpLimit = serializedObject.FindProperty("LeftEyeUpLimit");
            LeftEyeDownLimit = serializedObject.FindProperty("LeftEyeDownLimit");
            LeftEyeLeftLimit = serializedObject.FindProperty("LeftEyeLeftLimit");
            LeftEyeRightLimit = serializedObject.FindProperty("LeftEyeRightLimit");
            UseRightEyeBoneInstead = serializedObject.FindProperty("UseRightEyeBoneInstead");
            RightEyeBlendshapes = serializedObject.FindProperty("RightEyeBlendshapes");
            RightEyeBone = serializedObject.FindProperty("RightEyeBone");
            RightEyeUpLimit = serializedObject.FindProperty("RightEyeUpLimit");
            RightEyeDownLimit = serializedObject.FindProperty("RightEyeDownLimit");
            RightEyeLeftLimit = serializedObject.FindProperty("RightEyeLeftLimit");
            RightEyeRightLimit = serializedObject.FindProperty("RightEyeRightLimit");
            UseCombinedEyeBlendshapes = serializedObject.FindProperty("UseCombinedEyeBlendshapes");
            EyeBlendshapes = serializedObject.FindProperty("EyeBlendshapes");
            UseVisemes = serializedObject.FindProperty("UseVisemes");
            VisemeRenderers = serializedObject.FindProperty("VisemeRenderers");
            VisemesDict = serializedObject.FindProperty("VisemesDict");
            Animators = serializedObject.FindProperty("Animators");
            RootMenu = serializedObject.FindProperty("RootMenu");
            Parameters = serializedObject.FindProperty("Parameters");
            // Lists
            BlendshapeDescriptor[] eyeDescriptors = BlendshapeDescriptor.GetAllDescriptors(Avatar.EyeRenderers.ToArray());
            LeftEyeBlendshapesList = new ReorderableList(serializedObject, LeftEyeBlendshapes, false, true, false, false);
            LeftEyeBlendshapesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Left Eye Blendshapes");
            LeftEyeBlendshapesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<EyeBlendshapeAction>(LeftEyeBlendshapes, eyeDescriptors, a, b, c, d);
            LeftEyeBlendshapesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            RightEyeBlendshapesList = new ReorderableList(serializedObject, RightEyeBlendshapes, false, true, false, false);
            RightEyeBlendshapesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Right Eye Blendshapes");
            RightEyeBlendshapesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<EyeBlendshapeAction>(RightEyeBlendshapes, eyeDescriptors, a, b, c, d);
            RightEyeBlendshapesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            EyeBlendshapesList = new ReorderableList(serializedObject, EyeBlendshapes, false, true, false, false);
            EyeBlendshapesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Eye Blendshapes");
            EyeBlendshapesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<EyeBlendshapeAction>(EyeBlendshapes, eyeDescriptors, a, b, c, d);
            EyeBlendshapesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            BlendshapeDescriptor[] lipDescriptors = BlendshapeDescriptor.GetAllDescriptors(Avatar.VisemeRenderers.ToArray());
            VisemesList = new ReorderableList(serializedObject, VisemesDict, false, true, false, false);
            VisemesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Visemes");
            VisemesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<Viseme>(VisemesDict, lipDescriptors, a, b, c, d);
            VisemesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            AnimatorsList = new ReorderableList(serializedObject, Animators, true, true, true, true);
            AnimatorsList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Custom Animators");
            AnimatorsList.drawElementCallback += (a, b, c, d) => CustomPlayableAnimatorListDrawer.DrawList(Animators, a, b, c, d);
            AnimatorsList.elementHeightCallback += CustomPlayableAnimatorListDrawer.GetListHeight;
            // Handlers
            BlendshapeListDrawer.OnListCallback += (property, i, val) =>
            {
                if (property != LeftEyeBlendshapes && property != RightEyeBlendshapes && property != EyeBlendshapes && property != VisemesDict) return;
                waitingProperties.Enqueue((property, i, val));
            };
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawTitle("Avatar Settings");
            EditorUtils.PropertyField(ViewPosition, "View Position");
            EditorUtils.PropertyField(SpeechPosition, "Speech Position");
            EditorGUILayout.Space();
            EditorUtils.DrawTitle("Eye Settings");
            EditorUtils.PropertyField(UseEyeManager, "Enable Eye Movement");
            if (UseEyeManager.boolValue)
            {
                EditorUtils.PropertyField(EyeRenderers, "Eye Renderers");
                EditorUtils.PropertyField(UseCombinedEyeBlendshapes, "Use Combined Eyes");
                if (UseCombinedEyeBlendshapes.boolValue)
                    EyeBlendshapesList.DoLayoutList();
                else
                {
                    BlendshapeDescriptor[] descriptors = BlendshapeDescriptor.GetAllDescriptors(Avatar.EyeRenderers.ToArray());
                    GUILayout.Label("Left Eye", EditorStyles.miniBoldLabel);
                    //EditorUtils.PropertyField(UseLeftEyeBoneInstead, "Use Eye Bones");
                    UseLeftEyeBoneInstead.boolValue = GUILayout.Toolbar(UseLeftEyeBoneInstead.boolValue ? 1 : 0,
                        EyeBoneOptions) == 1;
                    if (UseLeftEyeBoneInstead.boolValue)
                    {
                        EditorUtils.PropertyField(LeftEyeBone, "Eye Bone");
                        EditorUtils.PropertyField(LeftEyeUpLimit, "Eye Up");
                        EditorUtils.PropertyField(LeftEyeDownLimit, "Eye Down");
                        EditorUtils.PropertyField(LeftEyeLeftLimit, "Eye Left");
                        EditorUtils.PropertyField(LeftEyeRightLimit, "Eye Right");
                        BlendshapeListDrawer.DrawLayoutReference(
                            LeftEyeBlendshapes.GetArrayElementAtIndex((int) EyeBlendshapeAction.Blink), descriptors,
                            EyeBlendshapeAction.Blink,
                            i => waitingProperties.Enqueue((LeftEyeBlendshapes, (int) EyeBlendshapeAction.Blink, i)));
                    }
                    else
                        LeftEyeBlendshapesList.DoLayoutList();
                    GUILayout.Label("Right Eye", EditorStyles.miniBoldLabel);
                    //EditorUtils.PropertyField(UseRightEyeBoneInstead, "Use Eye Bones");
                    UseRightEyeBoneInstead.boolValue = GUILayout.Toolbar(UseRightEyeBoneInstead.boolValue ? 1 : 0,
                        EyeBoneOptions) == 1;
                    if (UseRightEyeBoneInstead.boolValue)
                    {
                        EditorUtils.PropertyField(RightEyeBone, "Eye Bone");
                        EditorUtils.PropertyField(RightEyeUpLimit, "Eye Up");
                        EditorUtils.PropertyField(RightEyeDownLimit, "Eye Down");
                        EditorUtils.PropertyField(RightEyeLeftLimit, "Eye Left");
                        EditorUtils.PropertyField(RightEyeRightLimit, "Eye Right");
                        BlendshapeListDrawer.DrawLayoutReference(
                            RightEyeBlendshapes.GetArrayElementAtIndex((int) EyeBlendshapeAction.Blink), descriptors,
                            EyeBlendshapeAction.Blink,
                            i => waitingProperties.Enqueue((RightEyeBlendshapes, (int) EyeBlendshapeAction.Blink, i)));
                    }
                    else
                        RightEyeBlendshapesList.DoLayoutList();
                }
            }
            EditorGUILayout.Space();
            EditorUtils.DrawTitle("Viseme Settings");
            EditorUtils.PropertyField(UseVisemes, "Use Visemes");
            if(UseVisemes.boolValue)
            {
                EditorUtils.PropertyField(VisemeRenderers, "Viseme Renderers");
                VisemesList.DoLayoutList();
            }
            EditorGUILayout.Space();
            EditorUtils.DrawTitle("Playable Settings");
            //EditorUtils.PropertyField(Animators, "Animators");
            AnimatorsList.DoLayoutList();
            EditorUtils.PropertyField(RootMenu, "Root Menu");
            EditorUtils.PropertyField(Parameters, "Avatar Parameters");
            while (waitingProperties.TryDequeue(out (SerializedProperty s, int index, int v) val))
            {
                SerializedProperty t = val.s.GetArrayElementAtIndex(val.index);
                t.intValue = val.v;
            }
#if UNITY_EDITOR && HYPERNEX_CCK_EMULATOR
            if(!EditorApplication.isPlaying)
            {
                EditorGUILayout.Space();
                EditorUtils.DrawTitle("Testing");
                string avatarName = EditorPrefs.GetString("AvatarName");
                bool same = avatarName == Avatar.gameObject.name;
                float height = EditorGUIUtility.singleLineHeight * 2;
                Rect r = EditorGUILayout.GetControlRect(false, height + 20);
                Rect r1 = new Rect(r);
                r1.width /= 2;
                r1.height = height / 2 + 20;
                EditorGUI.HelpBox(r1, same ? "You are testing this avatar." : "You are not testing this avatar!",
                    same ? MessageType.Info : MessageType.Warning);
                r1.y += r1.height;
                r1.height -= 20;
                if (GUI.Button(r1, "Test this Avatar"))
                    EditorPrefs.SetString("AvatarName", Avatar.gameObject.name);
                Rect r2 = new Rect(r);
                r2.width /= 2;
                r2.height = height / 2 + 20;
                r2.x = r.width / 2 + EditorGUIUtility.singleLineHeight;
                bool exists = !string.IsNullOrEmpty(avatarName);
                EditorGUI.HelpBox(r2, exists ? $"You are testing {avatarName}" : "You are not testing any Avatars!",
                    same ? MessageType.Info : MessageType.Error);
                r2.y += r2.height;
                r2.height -= 20;
                if (GUI.Button(r2, "Stop Testing all Avatars"))
                    EditorPrefs.DeleteKey("AvatarName");
            }
#endif
            serializedObject.ApplyModifiedProperties();
        }
    }
}