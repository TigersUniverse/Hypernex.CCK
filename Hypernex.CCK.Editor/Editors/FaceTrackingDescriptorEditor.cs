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
                int selected = EditorGUILayout.Popup(keyValuePair.Key.ToString(), index, options);
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
            foreach (KeyValuePair<ExtraEyeExpressions,BlendshapeDescriptor> keyValuePair in new 
                         SerializedDictionaries.ExtraEyeBlendshapeDict(faceTrackingDescriptor.ExtraEyeValues))
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
                    if (faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key] == null)
                        faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key] = new BlendshapeDescriptor();
                    else if(faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].MatchString != match.Item1)
                        EditorUtility.SetDirty(faceTrackingDescriptor.gameObject);
                    faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].MatchString = match.Item1;
                    faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].SkinnedMeshRenderer = match.Item2;
                    faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key].BlendshapeIndex = match.Item3;
                }
                else if (selected == 0)
                    faceTrackingDescriptor.ExtraEyeValues[keyValuePair.Key] = null;
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