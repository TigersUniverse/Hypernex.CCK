using System;
using System.Collections.Generic;
using System.Linq;
using FindClosestString;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using UnityEditor;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Avatar;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(Avatar))]
    public class AvatarEditor : UnityEditor.Editor
    {
        private SerializedProperty ViewPosition;
        private SerializedProperty SpeechPosition;
        private SerializedProperty UseVisemes;
        private SerializedProperty UseEyeManager;
        private SerializedProperty UseLeftEyeBoneInstead;
        private SerializedProperty LeftEyeBone;
        private SerializedProperty LeftEyeUpLimit;
        private SerializedProperty LeftEyeDownLimit;
        private SerializedProperty LeftEyeLeftLimit;
        private SerializedProperty LeftEyeRightLimit;
        private SerializedProperty UseRightEyeBoneInstead;
        private SerializedProperty RightEyeBone;
        private SerializedProperty RightEyeUpLimit;
        private SerializedProperty RightEyeDownLimit;
        private SerializedProperty RightEyeLeftLimit;
        private SerializedProperty RightEyeRightLimit;
        private SerializedProperty UseCombinedEyeBlendshapes;
        private Avatar Avatar;

        private void OnEnable()
        {
            ViewPosition = serializedObject.FindProperty("ViewPosition");
            SpeechPosition = serializedObject.FindProperty("SpeechPosition");
            UseVisemes = serializedObject.FindProperty("UseVisemes");
            UseEyeManager = serializedObject.FindProperty("UseEyeManager");
            UseLeftEyeBoneInstead = serializedObject.FindProperty("UseLeftEyeBoneInstead");
            LeftEyeBone = serializedObject.FindProperty("LeftEyeBone");
            LeftEyeUpLimit = serializedObject.FindProperty("LeftEyeUpLimit");
            LeftEyeDownLimit = serializedObject.FindProperty("LeftEyeDownLimit");
            LeftEyeLeftLimit = serializedObject.FindProperty("LeftEyeLeftLimit");
            LeftEyeRightLimit = serializedObject.FindProperty("LeftEyeRightLimit");
            UseRightEyeBoneInstead = serializedObject.FindProperty("UseRightEyeBoneInstead");
            RightEyeBone = serializedObject.FindProperty("RightEyeBone");
            RightEyeUpLimit = serializedObject.FindProperty("RightEyeUpLimit");
            RightEyeDownLimit = serializedObject.FindProperty("RightEyeDownLimit");
            RightEyeLeftLimit = serializedObject.FindProperty("RightEyeLeftLimit");
            RightEyeRightLimit = serializedObject.FindProperty("RightEyeRightLimit");
            UseCombinedEyeBlendshapes = serializedObject.FindProperty("UseCombinedEyeBlendshapes");
            Avatar = target as Avatar;
        }

        private static bool an;
        private static bool avs;
        private static bool em;
        private static bool er = true;
        private static bool dv;
        private static bool dvs = true;

        private void DrawEye(ref SerializedDictionaries.EyeBlendshapeActionDict f, bool useBones)
        {
            List<(string, SkinnedMeshRenderer, int)> m = new List<(string, SkinnedMeshRenderer, int)>();
            int x = 0;
            foreach (SkinnedMeshRenderer avatarEyeRenderer in Avatar.EyeRenderers)
            {
                if (avatarEyeRenderer != null && avatarEyeRenderer.sharedMesh != null)
                {
                    for (int i = 0; i < avatarEyeRenderer.sharedMesh.blendShapeCount; i++)
                        m.Add((
                            $"{avatarEyeRenderer.gameObject.name} [{x}] - {avatarEyeRenderer.sharedMesh.GetBlendShapeName(i)} [{i}]",
                            avatarEyeRenderer, i));
                }
                x++;
            }
            string[] options = new string[m.Count + 1];
            options[0] = "None";
            for (int i = 1; i < options.Length; i++)
                options[i] = m.ElementAt(i - 1).Item1;
            foreach (KeyValuePair<EyeBlendshapeAction, BlendshapeDescriptor> keyValuePair in new SerializedDictionaries.EyeBlendshapeActionDict(f))
            {
                if (!useBones || keyValuePair.Key == EyeBlendshapeAction.Blink)
                {
                    int index = 0;
                    for(int i = 0; i < options.Length; i++)
                    {
                        if (keyValuePair.Value != null && keyValuePair.Value.MatchString == options[i])
                            index = i;
                    }
                    int selected = EditorGUILayout.Popup(keyValuePair.Key.ToString(), index, options);
                    (string, SkinnedMeshRenderer, int) match = (String.Empty, null, -1);
                    foreach ((string, SkinnedMeshRenderer, int) valueTuple in m)
                    {
                        if (valueTuple.Item1 == options[selected])
                            match = valueTuple;
                    }
                    if (!string.IsNullOrEmpty(match.Item1))
                    {
                        if (f[keyValuePair.Key] == null)
                            f[keyValuePair.Key] = new BlendshapeDescriptor();
                        else if(f[keyValuePair.Key].MatchString != match.Item1)
                            EditorUtility.SetDirty(Avatar.gameObject);
                        f[keyValuePair.Key].MatchString = match.Item1;
                        f[keyValuePair.Key].SkinnedMeshRenderer = match.Item2;
                        f[keyValuePair.Key].BlendshapeIndex = match.Item3;
                    }
                    else if (selected == 0)
                        f[keyValuePair.Key] = null;
                }
            }
        }

        private void DrawVisemes()
        {
            List<(string, SkinnedMeshRenderer, int)> m = new List<(string, SkinnedMeshRenderer, int)>();
            int x = 0;
            foreach (SkinnedMeshRenderer avatarVisemeRenderer in Avatar.VisemeRenderers)
            {
                if (avatarVisemeRenderer != null && avatarVisemeRenderer.sharedMesh != null)
                {
                    for (int i = 0; i < avatarVisemeRenderer.sharedMesh.blendShapeCount; i++)
                        m.Add((
                            $"{avatarVisemeRenderer.gameObject.name} [{x}] - {avatarVisemeRenderer.sharedMesh.GetBlendShapeName(i)} [{i}]",
                            avatarVisemeRenderer, i));
                }
                x++;
            }
            string[] options = new string[m.Count + 1];
            options[0] = "None";
            for (int i = 1; i < options.Length; i++)
                options[i] = m.ElementAt(i - 1).Item1;
            foreach (KeyValuePair<Viseme,BlendshapeDescriptor> keyValuePair in new SerializedDictionaries.VisemesDict(Avatar.VisemesDict))
            {
                int index = 0;
                for(int i = 0; i < options.Length; i++)
                {
                    if (keyValuePair.Value != null && keyValuePair.Value.MatchString == options[i])
                        index = i;
                }
                int selected = EditorGUILayout.Popup(keyValuePair.Key.ToString(), index, options);
                (string, SkinnedMeshRenderer, int) match = (String.Empty, null, -1);
                foreach ((string, SkinnedMeshRenderer, int) valueTuple in m)
                {
                    if (valueTuple.Item1 == options[selected])
                        match = valueTuple;
                }
                if (!string.IsNullOrEmpty(match.Item1))
                {
                    if (Avatar.VisemesDict[keyValuePair.Key] == null)
                        Avatar.VisemesDict[keyValuePair.Key] = new BlendshapeDescriptor();
                    else if(Avatar.VisemesDict[keyValuePair.Key].MatchString != match.Item1)
                        EditorUtility.SetDirty(Avatar.gameObject);
                    Avatar.VisemesDict[keyValuePair.Key].MatchString = match.Item1;
                    Avatar.VisemesDict[keyValuePair.Key].SkinnedMeshRenderer = match.Item2;
                    Avatar.VisemesDict[keyValuePair.Key].BlendshapeIndex = match.Item3;
                }
                else if (selected == 0)
                    Avatar.VisemesDict[keyValuePair.Key] = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ViewPosition.vector3Value = EditorGUILayout.Vector3Field("Viewpoint", ViewPosition.vector3Value);
            SpeechPosition.vector3Value = EditorGUILayout.Vector3Field("Speech Position", SpeechPosition.vector3Value);
            EditorGUILayout.Separator();
            UseEyeManager.boolValue = EditorGUILayout.Toggle("Use Eye Manager", UseEyeManager.boolValue);
            if (UseEyeManager.boolValue)
            {
                em = EditorGUILayout.Foldout(em, "<b>Eye Manager</b>", new GUIStyle(EditorStyles.foldout) {richText = true});
                if (em)
                {
                    EditorGUILayout.HelpBox("Input needed SkinnedMeshRenderers to look for possible Eye Blendshapes",
                        MessageType.Info);
                    EditorTools.DrawObjectList(ref Avatar.EyeRenderers, "Eye SkinnedMeshRenderers", ref er,
                        () => EditorUtility.SetDirty(Avatar.gameObject), allowSceneObjects:true);
                    EditorGUILayout.Separator();
                    GUILayout.Label("Left Eye", EditorStyles.miniBoldLabel);
                    UseLeftEyeBoneInstead.boolValue = EditorGUILayout.Toggle("Use Transform",
                        UseLeftEyeBoneInstead.boolValue);
                    if (UseLeftEyeBoneInstead.boolValue)
                    {
                        EditorGUILayout.PropertyField(LeftEyeBone);
                        EditorGUILayout.PropertyField(LeftEyeUpLimit);
                        EditorGUILayout.PropertyField(LeftEyeDownLimit);
                        EditorGUILayout.PropertyField(LeftEyeLeftLimit);
                        EditorGUILayout.PropertyField(LeftEyeRightLimit);
                    }
                    if (!UseCombinedEyeBlendshapes.boolValue)
                        DrawEye(ref Avatar.LeftEyeBlendshapes, UseLeftEyeBoneInstead.boolValue);
                    EditorGUILayout.Separator();
                    GUILayout.Label("Right Eye", EditorStyles.miniBoldLabel);
                    UseRightEyeBoneInstead.boolValue = EditorGUILayout.Toggle("Use Transform",
                        UseRightEyeBoneInstead.boolValue);
                    if (UseRightEyeBoneInstead.boolValue)
                    {
                        EditorGUILayout.PropertyField(RightEyeBone);
                        EditorGUILayout.PropertyField(RightEyeUpLimit);
                        EditorGUILayout.PropertyField(RightEyeDownLimit);
                        EditorGUILayout.PropertyField(RightEyeLeftLimit);
                        EditorGUILayout.PropertyField(RightEyeRightLimit);
                    }
                    if (!UseCombinedEyeBlendshapes.boolValue)
                        DrawEye(ref Avatar.RightEyeBlendshapes, UseRightEyeBoneInstead.boolValue);
                    GUILayout.Label("Combined Eye", EditorStyles.miniBoldLabel);
                    UseCombinedEyeBlendshapes.boolValue = EditorGUILayout.Toggle("Use Combined BlendShapes",
                        UseCombinedEyeBlendshapes.boolValue);
                    if (UseCombinedEyeBlendshapes.boolValue)
                        DrawEye(ref Avatar.EyeBlendshapes, false);
                }
            }
            EditorGUILayout.Separator();
            UseVisemes.boolValue = EditorGUILayout.Toggle("Use Visemes", UseVisemes.boolValue);
            if (UseVisemes.boolValue)
            {
                dv = EditorGUILayout.Foldout(dv, "<b>Visemes</b>", new GUIStyle(EditorStyles.foldout) {richText = true});
                if (dv)
                {
                    EditorGUILayout.HelpBox("Input needed SkinnedMeshRenderers to look for possible Viseme Blendshapes",
                        MessageType.Info);
                    EditorTools.DrawObjectList(ref Avatar.VisemeRenderers, "Viseme SkinnedMeshRenderers", ref dvs,
                        () => EditorUtility.SetDirty(Avatar.gameObject), (renderer, i) =>
                        {
                            if (renderer == null || renderer.sharedMesh == null)
                                return;
                            if (GUILayout.Button("Auto Select Visemes"))
                            {
                                foreach (KeyValuePair<Viseme, BlendshapeDescriptor> keyValuePair in
                                         new SerializedDictionaries.VisemesDict(Avatar.VisemesDict))
                                {
                                    if (keyValuePair.Value == null || keyValuePair.Value.MatchString == String.Empty)
                                    {
                                        List<string> blendshapes = new List<string>();
                                        for (int j = 0; j < renderer.sharedMesh.blendShapeCount; j++)
                                            blendshapes.Add(renderer.sharedMesh.GetBlendShapeName(j));
                                        (string, int) closestBlendshape =
                                            ClosestString.UsingLevenshtein(keyValuePair.Key.ToString(), blendshapes);
                                        if (closestBlendshape.Item1 != null)
                                        {
                                            int e = renderer.sharedMesh.GetBlendShapeIndex(closestBlendshape.Item1);
                                            if (Avatar.VisemesDict[keyValuePair.Key] == null)
                                                Avatar.VisemesDict[keyValuePair.Key] = new BlendshapeDescriptor();
                                            Avatar.VisemesDict[keyValuePair.Key].MatchString =
                                                $"{renderer.gameObject.name} [{i}] - {renderer.sharedMesh.GetBlendShapeName(e)} [{e}]";
                                            Avatar.VisemesDict[keyValuePair.Key].SkinnedMeshRenderer = renderer;
                                            Avatar.VisemesDict[keyValuePair.Key].BlendshapeIndex = e;
                                        }
                                    }
                                }
                            }
                        }, allowSceneObjects:true);
                    DrawVisemes();
                }
            }
            EditorGUILayout.Separator();
            EditorTools.DrawSimpleList(ref Avatar.Animators, "Custom Animators", ref an,
                () => EditorUtility.SetDirty(Avatar.gameObject), ca =>
                {
                    if (ca.AnimatorController == null)
                        return "New Animator";
                    return ca.AnimatorController.name;
                }, () => new CustomPlayableAnimator());
            EditorGUILayout.Separator();
            EditorTools.DrawSimpleList(ref Avatar.LocalAvatarScripts, "Local Avatar Scripts", ref avs,
                () => EditorUtility.SetDirty(Avatar.gameObject),
                script => script.Name == String.Empty
                    ? script.Language + " Script"
                    : script.Name + script.GetExtensionFromLanguage(), Activator.CreateInstance<NexboxScript>,
                (script, index) => EditorTools.DrawScriptEditorOnCustomEvent(Avatar, ref script, index),
                OnRemove: (script, i) =>
                {
                    if (ScriptEditorInstance.IsOpen)
                    {
                        ScriptEditorInstance scriptEditorInstance = ScriptEditorInstance.GetInstanceFromScript(script);
                        if (scriptEditorInstance != null)
                            scriptEditorInstance.RemoveScript();
                    }
                });
            serializedObject.ApplyModifiedProperties();
            //base.OnInspectorGUI();
        }
    }
}