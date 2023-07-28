using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using Hypernex.CCK.Unity.Internals;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(FaceTrackingDescriptor))]
    public class FaceTrackingDescriptorEditor : UnityEditor.Editor
    {
        private FaceTrackingDescriptor faceTrackingDescriptor;

        private void OnEnable()
        {
            faceTrackingDescriptor = target as FaceTrackingDescriptor;
        }

        private void DrawFaceTrackingBlendshapes()
        {
            List<(string, SkinnedMeshRenderer, int)> m = new List<(string, SkinnedMeshRenderer, int)>();
            int x = 0;
            foreach (SkinnedMeshRenderer ftVisemeRenderer in faceTrackingDescriptor.SkinnedMeshRenderers)
            {
                if (ftVisemeRenderer != null && ftVisemeRenderer.sharedMesh != null)
                {
                    for (int i = 0; i < ftVisemeRenderer.sharedMesh.blendShapeCount; i++)
                        m.Add((
                            $"{ftVisemeRenderer.gameObject.name} [{x}] - {ftVisemeRenderer.sharedMesh.GetBlendShapeName(i)} [{i}]",
                            ftVisemeRenderer, i));
                }
                x++;
            }
            string[] options = new string[m.Count + 1];
            options[0] = "None";
            for (int i = 1; i < options.Length; i++)
                options[i] = m.ElementAt(i - 1).Item1;
            foreach (KeyValuePair<FaceExpressions,BlendshapeDescriptor> keyValuePair in new 
                         SerializedDictionaries.FaceBlendshapeDict(faceTrackingDescriptor.FaceValues))
            {
                int index = 0;
                for(int i = 0; i < options.Length; i++)
                {
                    if (keyValuePair.Value != null && keyValuePair.Value.MatchString == options[i])
                        index = i;
                }
                //int selected = EditorGUILayout.Popup(keyValuePair.Key.ToString(), index, options);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(keyValuePair.Key.ToString());
                if (GUILayout.Button(options[index]))
                {
                    BlendshapeSelector.ShowWindow(options, selected =>
                    {
                        (string, SkinnedMeshRenderer, int) match = (String.Empty, null, -1);
                        foreach ((string, SkinnedMeshRenderer, int) valueTuple in m)
                        {
                            if (valueTuple.Item1 == options[selected])
                                match = valueTuple;
                        }
                        if (!string.IsNullOrEmpty(match.Item1))
                        {
                            if (faceTrackingDescriptor.FaceValues[keyValuePair.Key] == null)
                                faceTrackingDescriptor.FaceValues[keyValuePair.Key] = new BlendshapeDescriptor();
                            else if(faceTrackingDescriptor.FaceValues[keyValuePair.Key].MatchString != match.Item1)
                                EditorUtility.SetDirty(faceTrackingDescriptor.gameObject);
                            faceTrackingDescriptor.FaceValues[keyValuePair.Key].MatchString = match.Item1;
                            faceTrackingDescriptor.FaceValues[keyValuePair.Key].SkinnedMeshRenderer = match.Item2;
                            faceTrackingDescriptor.FaceValues[keyValuePair.Key].BlendshapeIndex = match.Item3;
                        }
                        else if (selected == 0)
                            faceTrackingDescriptor.FaceValues[keyValuePair.Key] = null;
                    });
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void DrawExtraEyeTrackingBlendshapes()
        {
            List<(string, SkinnedMeshRenderer, int)> m = new List<(string, SkinnedMeshRenderer, int)>();
            int x = 0;
            foreach (SkinnedMeshRenderer ftVisemeRenderer in faceTrackingDescriptor.SkinnedMeshRenderers)
            {
                if (ftVisemeRenderer != null && ftVisemeRenderer.sharedMesh != null)
                {
                    for (int i = 0; i < ftVisemeRenderer.sharedMesh.blendShapeCount; i++)
                        m.Add((
                            $"{ftVisemeRenderer.gameObject.name} [{x}] - {ftVisemeRenderer.sharedMesh.GetBlendShapeName(i)} [{i}]",
                            ftVisemeRenderer, i));
                }
                x++;
            }
            string[] options = new string[m.Count + 1];
            options[0] = "None";
            for (int i = 1; i < options.Length; i++)
                options[i] = m.ElementAt(i - 1).Item1;
            foreach (KeyValuePair<ExtraEyeExpressions,BlendshapeDescriptors> keyValuePair in new 
                         SerializedDictionaries.ExtraEyeBlendshapeDict(faceTrackingDescriptor.ExtraEyeValues))
            {
                GUILayout.Label(keyValuePair.Key.ToString());
                int y = 0;
                foreach (BlendshapeDescriptor blendshapeDescriptor in new List<BlendshapeDescriptor>(keyValuePair.Value.Descriptors))
                {
                    int index = 0;
                    for(int i = 0; i < options.Length; i++)
                    {
                        if (blendshapeDescriptor != null && blendshapeDescriptor.MatchString == options[i])
                            index = i;
                    }
                    //int selected = EditorGUILayout.Popup(index, options);
                    if (GUILayout.Button(options[index]))
                    {
                        BlendshapeSelector.ShowWindow(options, selected =>
                        {
                            (string, SkinnedMeshRenderer, int) match = (String.Empty, null, -1);
                            foreach ((string, SkinnedMeshRenderer, int) valueTuple in m)
                            {
                                if (valueTuple.Item1 == options[selected])
                                    match = valueTuple;
                            }
                            if (!string.IsNullOrEmpty(match.Item1))
                            {
                                if(faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors[y].MatchString != match.Item1)
                                    EditorUtility.SetDirty(faceTrackingDescriptor.gameObject);
                                faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors[y].MatchString = match.Item1;
                                faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors[y].SkinnedMeshRenderer = match.Item2;
                                faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors[y].BlendshapeIndex = match.Item3;
                            }
                            else if (selected == 0)
                                faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors[y] = new BlendshapeDescriptor();
                        });
                    }
                    y++;
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                {
                    faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors.Add(new BlendshapeDescriptor());
                    EditorUtility.SetDirty(faceTrackingDescriptor.gameObject);
                }
                if (GUILayout.Button("Remove"))
                {
                    if (faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors.Count > 0)
                    {
                        faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors
                            .RemoveAt(faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].Descriptors.Count - 1);
                        EditorUtility.SetDirty(faceTrackingDescriptor.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static bool er;

        public override void OnInspectorGUI()
        {
            EditorTools.DrawObjectList(ref faceTrackingDescriptor.SkinnedMeshRenderers, "Face SkinnedMeshRenderers", ref er,
                () => EditorUtility.SetDirty(faceTrackingDescriptor.gameObject), allowSceneObjects: true);
            EditorGUILayout.Separator();
            DrawFaceTrackingBlendshapes();
            EditorGUILayout.Separator();
            DrawExtraEyeTrackingBlendshapes();
        }
    }
}