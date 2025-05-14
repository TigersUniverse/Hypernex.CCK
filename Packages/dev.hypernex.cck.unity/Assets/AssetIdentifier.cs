using System;
using UnityEngine;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
#if UNITY_EDITOR
    [ExecuteInEditMode]
#endif
    public class AssetIdentifier : MonoBehaviour
    {
        [SerializeField]
#if UNITY_EDITOR
        [HideInInspector]
#endif
        public string Id;
    }
}