using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;

namespace Hypernex.CCK.Unity
{
    public static class WhitelistedComponents
    {
        public static readonly List<Type> AllowedAvatarTypes = new List<Type>
        {
            // Mesh
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(MeshFilter),
            // Physics
            typeof(CapsuleCollider),
            typeof(SphereCollider),
            typeof(BoxCollider),
            typeof(FixedJoint),
            typeof(HingeJoint),
            typeof(SpringJoint),
            typeof(ConfigurableJoint),
            // Constraints
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ScaleConstraint),
            typeof(ParentConstraint),
            typeof(AimConstraint),
            typeof(LookAtConstraint),
            // Lighting
            typeof(Light),
            typeof(LightProbeProxyVolume),
            // Particles
            typeof(ParticleSystem),
            typeof(ParticleSystemForceField),
            // Renderer
            typeof(TrailRenderer),
            typeof(LineRenderer),
            // Etc.
            typeof(Cloth),
            typeof(Transform),
            typeof(Animator),
            // Built-in
            typeof(AssetIdentifier),
            typeof(Avatar),
            typeof(MaterialDescriptor)
        };

        public static Type GetTMPType(TMPTypes tmpType)
        {
            Type t = null;
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ass.FullName.StartsWith("System."))
                    continue;
                t = ass.GetType("TMP." + tmpType);
                if (t != null)
                    break;
            }
            return t;
        }

        public static Component[] GetDeniedTypes(Transform[] transforms, ref List<Type> allowedTypes)
        {
            List<Component> deniedComponents = new List<Component>();
            foreach (Transform transform in transforms)
            {
                List<Component> cs = new List<Component>();
                transform.gameObject.GetComponents(cs);
                foreach (Component component in cs)
                {
                    if(!allowedTypes.Contains(component.GetType()))
                        deniedComponents.Add(component);
                }
            }
            return deniedComponents.ToArray();
        }

        public enum TMPTypes
        {
            TMP_Text,
            TMP_Asset,
            TMP_FontAsset,
            TMP_Dropdown
        }
    }
}