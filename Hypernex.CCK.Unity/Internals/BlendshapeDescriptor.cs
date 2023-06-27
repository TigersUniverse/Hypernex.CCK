using System;
using UnityEngine;

namespace Hypernex.CCK.Unity.Internals
{
    [Serializable]
    public class BlendshapeDescriptor
    {
        public string MatchString;
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public int BlendshapeIndex;

        public void SetWeight(float weight) => SkinnedMeshRenderer.SetBlendShapeWeight(BlendshapeIndex, weight);
    }
}