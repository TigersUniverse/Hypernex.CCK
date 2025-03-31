using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.CCK.Unity.Descriptors
{
    [Serializable]
    public class BlendshapeDescriptor
    {
        public string MatchString;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public int BlendshapeIndex;

        public void SetWeight(float weight) => SkinnedMeshRenderer.SetBlendShapeWeight(BlendshapeIndex, weight);

        public static BlendshapeDescriptor[] GetAllDescriptors(params SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            List<BlendshapeDescriptor> descriptors = new List<BlendshapeDescriptor>();
            for (int i = 0; i < skinnedMeshRenderers.Length; i++)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRenderers[i];
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                int count = mesh.blendShapeCount;
                for (int j = 0; j < count; j++)
                {
                    BlendshapeDescriptor descriptor = new BlendshapeDescriptor();
                    descriptor.BlendshapeIndex = j;
                    descriptor.SkinnedMeshRenderer = skinnedMeshRenderer;
                    descriptor.MatchString =
                        $"{skinnedMeshRenderer.gameObject.name} [{i}] - {mesh.GetBlendShapeName(j)} [{j}]";
                    descriptors.Add(descriptor);
                }
            }
            return descriptors.ToArray();
        }
    }
}