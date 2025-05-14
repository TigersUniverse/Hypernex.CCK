using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Descriptors;
using Hypernex.CCK.Unity.Emulator;
using Hypernex.CCK.Unity.Interaction;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Networking.Messages;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Object = UnityEngine.Object;
using Security = Hypernex.CCK.Unity.Internals.Security;

namespace Hypernex.Game.Avatar
{
    public abstract class AvatarCreator : IDisposable
    {
        public const string PARAMETER_ID = "parameter";
        public const string BLENDSHAPE_ID = "blendshape";
        public const string MAIN_ANIMATOR = "*main";
        public const string ALL_ANIMATOR_LAYERS = "*all";
        public const float CHARACTER_HEIGHT = 1.36144f;
        
        public CCK.Unity.Assets.Avatar Avatar { get; protected set; }
        public Animator MainAnimator { get; protected set; }
        public FaceTrackingDescriptor FaceTrackingDescriptor { get; protected set; }
        public List<AnimatorPlayable> AnimatorPlayables = new();
        public bool Calibrated { get; protected set; }

        internal List<WeightedObjectUpdate> DefaultWeights;
        
        private Dictionary<string, Transform> cachedTransforms = new();
        private Dictionary<Transform, SkinnedMeshRenderer> cachedSkinnedMeshRenderers = new();

        private List<AnimatorControllerParameter> _parameters;
        public List<AnimatorControllerParameter> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    List<AnimatorControllerParameter> allParameters = new();
                    foreach (AnimatorPlayable animatorPlayable in AnimatorPlayables)
                    {
                        List<AnimatorControllerParameter> playableParameters =
                            GetAllParameters(animatorPlayable.AnimatorControllerPlayable);
                        allParameters = allParameters.Concat(playableParameters).ToList();
                    }
                    _parameters = allParameters;
                }
                return _parameters;
            }
        }

        public AnimatorControllerParameter[] MainAnimatorParameters => MainAnimator == null
            ? Array.Empty<AnimatorControllerParameter>()
            : MainAnimator.parameters;
        
        protected BlendshapeDescriptor[] VisemeRenderers { get; private set; }
        
        protected GameObject HeadAlign;
        internal GameObject VoiceAlign;

        protected readonly RuntimeAnimatorController animatorController =
            Object.Instantiate(Init.Instance.DefaultAvatarAnimatorController);
        private List<SkinnedMeshRenderer> skinnedMeshRenderers = new();
        internal AudioSource audioSource;
        internal List<Sandbox> localAvatarSandboxes = new();
        internal RotationOffsetDriver headRotator;
        private Dictionary<SkinnedMeshRenderer, HashSet<int>> usedVisemes;

        protected void OnCreate(CCK.Unity.Assets.Avatar a, int layer, AllowedAvatarComponent allowedAvatarComponent)
        {
            Security.RemoveOffendingItems(a, allowedAvatarComponent,
                SecurityTools.AdditionalAllowedAvatarTypes.ToArray());
            Security.ApplyComponentRestrictions(a);
            FaceTrackingDescriptor = a.gameObject.GetComponent<FaceTrackingDescriptor>();
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in a.gameObject
                         .GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.updateWhenOffscreen = true;
                skinnedMeshRenderers.Add(skinnedMeshRenderer); 
            }
            foreach (Transform transform in a.transform.GetComponentsInChildren<Transform>())
                transform.gameObject.layer = layer;
            Animator an = a.transform.GetComponent<Animator>();
            if(an != null)
                an.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if(MainAnimator == null || MainAnimator.avatar == null) return;
            MainAnimator.runtimeAnimatorController = null;
            MainAnimator.applyRootMotion = false;
            Transform head = MainAnimator.GetBoneTransform(HumanBodyBones.Head);
            if(head == null) return;
            headRotator = new RotationOffsetDriver(head, a.transform);
            VisemeRenderers = BlendshapeDescriptor.GetAllDescriptors(a.VisemeRenderers.ToArray());
            usedVisemes = BlendshapeDescriptor.GetUsedBlendshapes(VisemeRenderers, a.VisemesDict);
        }

        protected void DriveCamera(Transform cam)
        {
            if(headRotator == null) return;
            headRotator.Rotate(cam.rotation);
        }
        
        protected virtual void SetupAnimators()
        {
            foreach (CustomPlayableAnimator customPlayableAnimator in Avatar.Animators)
            {
                if (customPlayableAnimator == null || customPlayableAnimator.AnimatorController == null) continue;
                if (customPlayableAnimator.AnimatorOverrideController != null)
                    customPlayableAnimator.AnimatorOverrideController.runtimeAnimatorController =
                        customPlayableAnimator.AnimatorController;
                PlayableGraph playableGraph = PlayableGraph.Create(customPlayableAnimator.AnimatorController.name);
                AnimatorControllerPlayable animatorControllerPlayable =
                    AnimatorControllerPlayable.Create(playableGraph, customPlayableAnimator.AnimatorController);
                PlayableOutput playableOutput = AnimationPlayableOutput.Create(playableGraph,
                    customPlayableAnimator.AnimatorController.name, MainAnimator);
                playableOutput.SetSourcePlayable(animatorControllerPlayable);
                AnimatorPlayables.Add(new AnimatorPlayable
                {
                    CustomPlayableAnimator = customPlayableAnimator,
                    PlayableGraph = playableGraph,
                    AnimatorControllerPlayable = animatorControllerPlayable,
                    PlayableOutput = playableOutput,
                    AnimatorControllerParameters = GetAllParameters(animatorControllerPlayable)
                });
                playableGraph.Play();
            }
            DefaultWeights = GetAnimatorWeights(true);
        }

        private Bounds GetAvatarBounds()
        {
            Bounds bounds = new Bounds(MainAnimator.transform.position, Vector3.zero);
            foreach (Renderer renderer in MainAnimator.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        protected void AlignAvatar(bool isVR)
        {
            Bounds b = GetAvatarBounds();
            float avatarBottom = MainAnimator.transform.parent.InverseTransformPoint(b.min).y - MainAnimator.transform.localPosition.y;
            float requiredOffset = -avatarBottom;
            Vector3 pos = new Vector3(0, requiredOffset - (CHARACTER_HEIGHT / 2f), 0);
            Avatar.transform.localPosition = isVR ? Vector3.zero : pos;
            Avatar.transform.localRotation = Quaternion.identity;
        }
        
        // Here's an idea Unity.. EXPOSE THE PARAMETERS??
        private List<AnimatorControllerParameter> GetAllParameters(AnimatorControllerPlayable animatorControllerPlayable)
        {
            List<AnimatorControllerParameter> parameters = new();
            bool c = true;
            int i = 0;
            while (c)
            {
                try
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        animatorControllerPlayable.GetParameter(i);
                    parameters.Add(animatorControllerParameter);
                    i++;
                }
                catch (IndexOutOfRangeException) {c = false;}
            }
            return parameters;
        }

        protected AnimatorControllerParameter GetParameterByName(string name, AnimatorPlayable animatorPlayable)
        {
            foreach (AnimatorControllerParameter animatorPlayableAnimatorControllerParameter in animatorPlayable.AnimatorControllerParameters)
            {
                if (animatorPlayableAnimatorControllerParameter.name == name)
                    return animatorPlayableAnimatorControllerParameter;
            }
            return null;
        }

        protected AnimatorPlayable? GetPlayable(CustomPlayableAnimator customPlayableAnimator) =>
            AnimatorPlayables.Find(x => x.CustomPlayableAnimator == customPlayableAnimator);

        public T GetParameter<T>(string parameterName, CustomPlayableAnimator target = null)
        {
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.Boolean:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetBool(parameterName), typeof(T));
                        case TypeCode.Int32:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return (T) Convert.ChangeType(
                                animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                        default:
                            if(typeof(T) == typeof(float))
                                return (T) Convert.ChangeType(
                                    animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                            return default;
                    }
                }
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                if (playableAnimator.AnimatorControllerParameters.Count(x => x.name == parameterName) > 0)
                {
                    switch (Type.GetTypeCode(typeof(T)))
                    {
                        case TypeCode.Boolean:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetBool(parameterName), typeof(T));
                        case TypeCode.Int32:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetInteger(parameterName), typeof(T));
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return (T) Convert.ChangeType(
                                playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                        default:
                            if(typeof(T) == typeof(float))
                                return (T) Convert.ChangeType(
                                    playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName), typeof(T));
                            return default;
                    }
                }
            }
            return default;
        }
        
        public object GetParameter(string parameterName, CustomPlayableAnimator target = null)
        {
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        GetParameterByName(parameterName, animatorPlayable.Value);
                    if (animatorControllerParameter != null)
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetBool(parameterName);
                            case AnimatorControllerParameterType.Int:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetInteger(parameterName);
                            case AnimatorControllerParameterType.Float:
                                return animatorPlayable.Value.AnimatorControllerPlayable.GetFloat(parameterName);
                        }
                    }
                }
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                AnimatorControllerParameter animatorControllerParameter =
                    GetParameterByName(parameterName, playableAnimator);
                if (animatorControllerParameter != null)
                {
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            return playableAnimator.AnimatorControllerPlayable.GetBool(parameterName);
                        case AnimatorControllerParameterType.Int:
                            return playableAnimator.AnimatorControllerPlayable.GetInteger(parameterName);
                        case AnimatorControllerParameterType.Float:
                            return playableAnimator.AnimatorControllerPlayable.GetFloat(parameterName);
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// Sets the value of a Parameter
        /// </summary>
        /// <param name="parameterName">The name of the Parameter</param>
        /// <param name="value">The value to set the parameter to</param>
        /// <param name="target">The target Playable, null for all</param>
        /// <param name="force">Forces the value to be casted to all types</param>
        /// <param name="mainAnimator">Drive the parameter for the MainAnimator</param>
        /// <typeparam name="T">A valid parameter type (float, int, or bool)</typeparam>
        public void SetParameter<T>(string parameterName, T value, CustomPlayableAnimator target = null, 
            bool force = false, bool mainAnimator = false)
        {
            if (mainAnimator && MainAnimator != null)
            {
                try
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        MainAnimatorParameters.First(x => x.name == parameterName);
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            MainAnimator.SetBool(parameterName, (bool) Convert.ChangeType(value, typeof(bool)));
                            break;
                        case AnimatorControllerParameterType.Int:
                            MainAnimator.SetInteger(parameterName, (int) Convert.ChangeType(value, typeof(int)));
                            break;
                        case AnimatorControllerParameterType.Float:
                            MainAnimator.SetFloat(parameterName, (float) Convert.ChangeType(value, typeof(float)));
                            break;
                    }
                }catch(Exception){}
            }
            if (target != null)
            {
                AnimatorPlayable? animatorPlayable = GetPlayable(target);
                if (animatorPlayable != null)
                {
                    AnimatorControllerParameter animatorControllerParameter =
                        GetParameterByName(parameterName, animatorPlayable.Value);
                    if (animatorControllerParameter != null)
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case AnimatorControllerParameterType.Int:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case AnimatorControllerParameterType.Float:
                                animatorPlayable.Value.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
                return;
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                if (force)
                {
                    foreach (AnimatorControllerParameter animatorControllerParameter in playableAnimator
                                 .AnimatorControllerParameters.Where(x => x.name == parameterName))
                    {
                        switch (animatorControllerParameter.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case AnimatorControllerParameterType.Int:
                                playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case AnimatorControllerParameterType.Float:
                                playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
                else
                {
                    if (playableAnimator.AnimatorControllerParameters.Count(x => x.name == parameterName) > 0)
                    {
                        switch (Type.GetTypeCode(typeof(T)))
                        {
                            case TypeCode.Boolean:
                                playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                    (bool) Convert.ChangeType(value, typeof(bool)));
                                break;
                            case TypeCode.Int32:
                                playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                    (int) Convert.ChangeType(value, typeof(int)));
                                break;
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                                playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                    (float) Convert.ChangeType(value, typeof(float)));
                                break;
                            default:
                                if (typeof(T) == typeof(float))
                                    playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                        (float) Convert.ChangeType(value, typeof(float)));
                                break;
                        }
                    }
                }
            }
        }
        
        public void SetParameter(WeightedObjectUpdate weight)
        {
            switch (weight.TypeOfWeight.ToLower())
            {
                case PARAMETER_ID:
                {
                    string parameterName = weight.WeightIndex;
                    if (weight.PathToWeightContainer == MAIN_ANIMATOR)
                    {
                        if(MainAnimator == null || MainAnimator.runtimeAnimatorController == null) break;
                        try
                        {
                            AnimatorControllerParameter parameter =
                                MainAnimator.parameters.First(x => x.name == weight.WeightIndex);
                            switch (parameter.type)
                            {
                                case AnimatorControllerParameterType.Bool:
                                    MainAnimator.SetBool(parameter.name, Math.Abs(weight.Weight - 1.00f) < 0.01);
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    MainAnimator.SetInteger(parameter.name, (int) weight.Weight);
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    MainAnimator.SetFloat(parameter.name, weight.Weight);
                                    break;
                            }
                        } catch(Exception){}
                        break;
                    }
                    foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
                    {
                        if (playableAnimator.CustomPlayableAnimator.AnimatorController.name !=
                            weight.PathToWeightContainer && weight.PathToWeightContainer != ALL_ANIMATOR_LAYERS) continue;
                        try
                        {
                            AnimatorControllerParameter parameter = GetParameterByName(parameterName, playableAnimator);
                            if (parameter != null)
                            {
                                switch (parameter.type)
                                {
                                    case AnimatorControllerParameterType.Bool:
                                        playableAnimator.AnimatorControllerPlayable.SetBool(parameterName,
                                            Math.Abs(weight.Weight - 1.00f) < 0.01);
                                        break;
                                    case AnimatorControllerParameterType.Int:
                                        playableAnimator.AnimatorControllerPlayable.SetInteger(parameterName,
                                            (int) weight.Weight);
                                        break;
                                    case AnimatorControllerParameterType.Float:
                                        playableAnimator.AnimatorControllerPlayable.SetFloat(parameterName,
                                            weight.Weight);
                                        break;
                                }
                            }
                        } catch(Exception){}
                    }
                    break;
                }
                case BLENDSHAPE_ID:
                {
                    try
                    {
                        if(!cachedTransforms.TryGetValue(weight.PathToWeightContainer, out Transform t))
                        {
                            t = Avatar.transform.parent.Find(weight.PathToWeightContainer);
                            if(t == null) break;
                            cachedTransforms.Add(weight.PathToWeightContainer, t);
                        }
                        if(!cachedSkinnedMeshRenderers.TryGetValue(t, out SkinnedMeshRenderer s))
                        {
                            s = t.gameObject.GetComponent<SkinnedMeshRenderer>();
                            if (s == null) break;
                            cachedSkinnedMeshRenderers.Add(t, s);
                        }
                        s.SetBlendShapeWeight(Convert.ToInt32(weight.WeightIndex), weight.Weight);
                    } catch(Exception){}
                    break;
                }
            }
        }

        public void SetParameters(WeightedObjectUpdate[] weights)
        {
            foreach (WeightedObjectUpdate weight in weights)
                SetParameter(weight);
        }

        internal List<WeightedObjectUpdate> GetAnimatorWeights(bool skipMain = false)
        {
            List<WeightedObjectUpdate> weights = new();
            if(MainAnimator != null && !skipMain)
            {
                foreach (AnimatorControllerParameter animatorControllerParameter in MainAnimator.parameters)
                {
                    float weight;
                    switch (animatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            weight = MainAnimator.GetBool(animatorControllerParameter.name) ? 1f : 0f;
                            break;
                        case AnimatorControllerParameterType.Int:
                            weight = MainAnimator.GetInteger(animatorControllerParameter.name);
                            break;
                        case AnimatorControllerParameterType.Float:
                            weight = MainAnimator.GetFloat(animatorControllerParameter.name);
                            break;
                        default:
                            continue;
                    }
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        PathToWeightContainer = MAIN_ANIMATOR,
                        TypeOfWeight = PARAMETER_ID,
                        WeightIndex = animatorControllerParameter.name,
                        Weight = weight
                    };
                    weights.Add(weightedObjectUpdate);
                }
            }
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
            {
                foreach (AnimatorControllerParameter playableAnimatorControllerParameter in playableAnimator.AnimatorControllerParameters)
                {
                    WeightedObjectUpdate weightedObjectUpdate = new WeightedObjectUpdate
                    {
                        PathToWeightContainer = playableAnimator.CustomPlayableAnimator.AnimatorController.name,
                        TypeOfWeight = PARAMETER_ID,
                        WeightIndex = playableAnimatorControllerParameter.name
                    };
                    switch (playableAnimatorControllerParameter.type)
                    {
                        case AnimatorControllerParameterType.Bool:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetBool(playableAnimatorControllerParameter
                                    .name)
                                    ? 1.00f
                                    : 0.00f;
                            break;
                        case AnimatorControllerParameterType.Int:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetInteger(
                                    playableAnimatorControllerParameter.name);
                            break;
                        case AnimatorControllerParameterType.Float:
                            weightedObjectUpdate.Weight =
                                playableAnimator.AnimatorControllerPlayable.GetFloat(playableAnimatorControllerParameter
                                    .name);
                            break;
                        default:
                            continue;
                    }
                    weights.Add(weightedObjectUpdate);
                }
            }
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
            {
                PathDescriptor p = skinnedMeshRenderer.gameObject.GetComponent<PathDescriptor>();
                if(p == null) continue;
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++)
                {
                    if (Avatar.UseVisemes &&
                        usedVisemes.TryGetValue(skinnedMeshRenderer, out HashSet<int> used) &&
                        used.Contains(i))
                    {
                        continue;
                    }
                    float w = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    weights.Add(new WeightedObjectUpdate
                    {
                        PathToWeightContainer = p.path,
                        TypeOfWeight = BLENDSHAPE_ID,
                        WeightIndex = i.ToString(),
                        Weight = w
                    });
                }
            }
            return weights;
        }
        
        public void GetBlendshapeWeight(SkinnedMeshRenderer skinnedMeshRenderer, int blendshapeIndex) =>
            skinnedMeshRenderer.GetBlendShapeWeight(blendshapeIndex);

        public void SetBlendshapeWeight(SkinnedMeshRenderer skinnedMeshRenderer, int blendshapeIndex, float weight) =>
            skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndex, weight);
        
        public Transform GetBoneFromHumanoid(HumanBodyBones humanBodyBones)
        {
            if (MainAnimator == null)
                return null;
            return MainAnimator.GetBoneTransform(humanBodyBones);
        }
        
        public Transform GetTargetChild(Transform tracker)
        {
            for (int i = 0; i < tracker.childCount; i++)
            {
                Transform t = tracker.GetChild(0);
                if (t.gameObject.name.Contains("Target")) return t;
            }
            return null;
        }
        
        internal void FixedUpdate() => localAvatarSandboxes.ForEach(x => x.InstanceContainer.Runtime.FixedUpdate());

        internal void Update()
        {
            /*SetParameter("Viseme", GetVisemeIndex(), null, true);
            foreach (KeyValuePair<string, float> viseme in GetVisemes())
                SetParameter(viseme.Key, viseme.Value, null, true);*/
            localAvatarSandboxes.ForEach(x => x.InstanceContainer.Runtime.Update());
        }
        
        internal void LateUpdate() => localAvatarSandboxes.ForEach(x => x.InstanceContainer.Runtime.LateUpdate());

        protected void DisposeScripts()
        {
            foreach (Sandbox localAvatarSandbox in new List<Sandbox>(localAvatarSandboxes))
            {
                localAvatarSandboxes.Remove(localAvatarSandbox);
                localAvatarSandbox.Dispose();
            }
        }

        public virtual void Dispose()
        {
            foreach (AnimatorPlayable playableAnimator in AnimatorPlayables)
                try
                {
                    playableAnimator.PlayableGraph.Destroy();
                }
                catch(ArgumentException){}
            DisposeScripts();
            cachedTransforms.Clear();
            cachedSkinnedMeshRenderers.Clear();
            Object.Destroy(Avatar.gameObject);
        }
    }
}