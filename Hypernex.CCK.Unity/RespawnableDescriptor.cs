using UnityEngine;

namespace Hypernex.CCK.Unity
{
    public class RespawnableDescriptor : MonoBehaviour
    {
        [Tooltip("How far below the lowest point until the GameObject is respawned. This value should be greater than 0.")]
        public float LowestPointRespawnThreshold = 50f;
    }
}