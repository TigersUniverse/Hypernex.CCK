using System;
using System.Collections.Generic;
using Hypernex.CCK.Unity.Internals;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    [Serializable]
    [RequireComponent(typeof(AssetIdentifier))]
    [RequireComponent(typeof(Animator))]
    public class Avatar : MonoBehaviour
    {
        public GameObject ViewPosition;
        public GameObject SpeechPosition;

        public bool UseEyeManager;
        public List<SkinnedMeshRenderer> EyeRenderers = new List<SkinnedMeshRenderer>();
        public bool UseLeftEyeBoneInstead;
        public SerializedDictionaries.EyeBlendshapeActionDict LeftEyeBlendshapes =
            new SerializedDictionaries.EyeBlendshapeActionDict(new Dictionary<EyeBlendshapeAction, BlendshapeDescriptor>
            {
                [EyeBlendshapeAction.LookUp] = null,
                [EyeBlendshapeAction.LookDown] = null,
                [EyeBlendshapeAction.LookLeft] = null,
                [EyeBlendshapeAction.LookRight] = null,
                [EyeBlendshapeAction.Blink] = null
            });
        public Transform LeftEyeBone;
        public Quaternion LeftEyeUpLimit;
        public Quaternion LeftEyeDownLimit;
        public Quaternion LeftEyeLeftLimit;
        public Quaternion LeftEyeRightLimit;
        public bool UseRightEyeBoneInstead;
        public SerializedDictionaries.EyeBlendshapeActionDict RightEyeBlendshapes =
            new SerializedDictionaries.EyeBlendshapeActionDict(new Dictionary<EyeBlendshapeAction, BlendshapeDescriptor>
            {
                [EyeBlendshapeAction.LookUp] = null,
                [EyeBlendshapeAction.LookDown] = null,
                [EyeBlendshapeAction.LookLeft] = null,
                [EyeBlendshapeAction.LookRight] = null,
                [EyeBlendshapeAction.Blink] = null
            });
        public Transform RightEyeBone;
        public Quaternion RightEyeUpLimit;
        public Quaternion RightEyeDownLimit;
        public Quaternion RightEyeLeftLimit;
        public Quaternion RightEyeRightLimit;
        public bool UseCombinedEyeBlendshapes;
        public SerializedDictionaries.EyeBlendshapeActionDict EyeBlendshapes =
            new SerializedDictionaries.EyeBlendshapeActionDict(new Dictionary<EyeBlendshapeAction, BlendshapeDescriptor>
            {
                [EyeBlendshapeAction.LookUp] = null,
                [EyeBlendshapeAction.LookDown] = null,
                [EyeBlendshapeAction.LookLeft] = null,
                [EyeBlendshapeAction.LookRight] = null,
                [EyeBlendshapeAction.Blink] = null
            });
        
        public bool UseVisemes;
        public List<SkinnedMeshRenderer> VisemeRenderers = new List<SkinnedMeshRenderer>();
        public SerializedDictionaries.VisemesDict VisemesDict = new SerializedDictionaries.VisemesDict(
            new Dictionary<Viseme, BlendshapeDescriptor>
            {
                [Viseme.sil] = null,
                [Viseme.PP] = null,
                [Viseme.FF] = null,
                [Viseme.TH] = null,
                [Viseme.DD] = null,
                [Viseme.kk] = null,
                [Viseme.CH] = null,
                [Viseme.SS] = null,
                [Viseme.nn] = null,
                [Viseme.RR] = null,
                [Viseme.aa] = null,
                [Viseme.EE] = null,
                [Viseme.IH] = null,
                [Viseme.OH] = null,
                [Viseme.OU] = null
            });

        public List<CustomPlayableAnimator> Animators = new List<CustomPlayableAnimator>();
        public List<NexboxScript> LocalAvatarScripts = new List<NexboxScript>();

        public bool ShowAllParameters;
        public List<string> VisibleParameters = new List<string>();

        // Editor only Things
        [SerializeField] public AvatarMeta Meta;
        [SerializeField] public bool ReplaceImage;
        [SerializeField] public Sprite Image;
    }
}