using System;
using System.Collections.Generic;
using System.Linq;
using HypernexSharp.APIObjects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Internals
{
    public static class SerializedDictionaries
    {
        [Serializable]
        public class MaterialsDict : SerializedDictionary<Material, Material>
        {
            public MaterialsDict(MaterialsDict copy) : base(new List<Material>(copy.Keys.ToList()),
                new List<Material>(copy.Values.ToList())){}
            public MaterialsDict(IDictionary<Material, Material> i) : base(i){}
        }

        [Serializable]
        public class BuildMaterialsDict : SerializedDictionary<BuildPlatform, MaterialsDict>
        {
            public BuildMaterialsDict(IDictionary<BuildPlatform, MaterialsDict> i) : base(i){}
        }

        [Serializable]
        public class VisemesDict : SerializedDictionary<Viseme, BlendshapeDescriptor>
        {
            public VisemesDict(VisemesDict copy) : base(new List<Viseme>(copy.Keys.ToList()),
                new List<BlendshapeDescriptor>(copy.Values.ToList())){}
            public VisemesDict(IDictionary<Viseme, BlendshapeDescriptor> i) : base(i){}
        }

        [Serializable]
        public class EyeBlendshapeActionDict : SerializedDictionary<EyeBlendshapeAction, BlendshapeDescriptor>
        {
            public EyeBlendshapeActionDict(EyeBlendshapeActionDict copy) : base(
                new List<EyeBlendshapeAction>(copy.Keys.ToList()),
                new List<BlendshapeDescriptor>(copy.Values.ToList())){}

            public EyeBlendshapeActionDict(IDictionary<EyeBlendshapeAction, BlendshapeDescriptor> i) : base(i){}
        }

        [Serializable]
        public class ExtraEyeBlendshapeDict : SerializedDictionary<ExtraEyeExpressions, BlendshapeDescriptors>
        {
            public ExtraEyeBlendshapeDict(ExtraEyeBlendshapeDict e) : base(
                new List<ExtraEyeExpressions>(e.Keys.ToList()), new List<BlendshapeDescriptors>(e.Values.ToList())) {}
            
            public ExtraEyeBlendshapeDict(IDictionary<ExtraEyeExpressions, BlendshapeDescriptors> i) : base(i){}
        }

        [Serializable]
        public class FaceBlendshapeDict : SerializedDictionary<FaceExpressions, BlendshapeDescriptor>
        {
            public FaceBlendshapeDict(FaceBlendshapeDict copy) : base(
                new List<FaceExpressions>(copy.Keys.ToList()),
                new List<BlendshapeDescriptor>(copy.Values.ToList())){}

            public FaceBlendshapeDict(IDictionary<FaceExpressions, BlendshapeDescriptor> i) : base(i){}
        }
    }
}