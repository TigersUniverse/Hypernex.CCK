using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Editor.ListDrawers;
using Hypernex.CCK.Unity.Interaction;
using UnityEditor;
using UnityEditorInternal;

namespace Hypernex.CCK.Unity.Editor.Editors
{
    [CustomEditor(typeof(FaceTrackingDescriptor))]
    public class FaceTrackingDescriptorEditor : UnityEditor.Editor
    {
        private FaceTrackingDescriptor faceTrackingDescriptor;

        private SerializedProperty SkinnedMeshRenderers;
        
        private SerializedProperty FaceValues;
        private ReorderableList FaceValuesList;
        
        private SerializedProperty ExtraEyeValues;
        private ReorderableList ExtraEyeValuesList;
        
        private Queue<(SerializedProperty, int, int)> waitingProperties = new Queue<(SerializedProperty, int, int)>();

        private void OnEnable()
        {
            faceTrackingDescriptor = target as FaceTrackingDescriptor;
            if (faceTrackingDescriptor == null)
                throw new NullReferenceException("Avatar Component cannot be null!");
            SkinnedMeshRenderers = serializedObject.FindProperty("SkinnedMeshRenderers");
            FaceValues = serializedObject.FindProperty("FaceValues");
            ExtraEyeValues = serializedObject.FindProperty("ExtraEyeValues");
            // Lists
            BlendshapeDescriptor[] descriptors = BlendshapeDescriptor.GetAllDescriptors(faceTrackingDescriptor.SkinnedMeshRenderers.ToArray());
            FaceValuesList = new ReorderableList(serializedObject, FaceValues, false, true, false, false);
            FaceValuesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Face Values");
            FaceValuesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<FaceExpressions>(FaceValues, descriptors, a, b, c, d, 150);
            FaceValuesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            ExtraEyeValuesList = new ReorderableList(serializedObject, ExtraEyeValues, false, true, false, false);
            ExtraEyeValuesList.drawHeaderCallback +=
                rect => EditorUtils.DrawReorderableListHeader(rect, "Extra Eye Shapes");
            ExtraEyeValuesList.drawElementCallback += (a, b, c, d) => BlendshapeListDrawer.DrawList<ExtraEyeExpressions>(ExtraEyeValues, descriptors, a, b, c, d, 150);
            ExtraEyeValuesList.elementHeightCallback += BlendshapeListDrawer.GetListHeight;
            BlendshapeListDrawer.OnListCallback += (property, i, val) =>
            {
                if (property != FaceValues && property != ExtraEyeValues) return;
                waitingProperties.Enqueue((property, i, val));
            };
        }

        public override void OnInspectorGUI()
        {
            EditorUtils.DrawTitle("Face Tracking");
            EditorGUILayout.Space();
            EditorUtils.PropertyField(SkinnedMeshRenderers, "Face Meshes");
            EditorGUILayout.Separator();
            FaceValuesList.DoLayoutList();
            ExtraEyeValuesList.DoLayoutList();
            while (waitingProperties.TryDequeue(out (SerializedProperty s, int index, int v) val))
            {
                SerializedProperty t = val.s.GetArrayElementAtIndex(val.index);
                t.intValue = val.v;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}