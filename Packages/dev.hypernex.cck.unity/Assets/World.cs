using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [RequireComponent(typeof(AssetIdentifier))]
    public class World : MonoBehaviour
    {
        public bool AllowRespawn = true;
        public float Gravity = -9.87f;
        public float JumpHeight = 1.0f;
        public float WalkSpeed = 3f;
        public float RunSpeed = 7f;
        public bool AllowRunning = true;
        public bool AllowScaling = true;
        public bool LockAvatarSwitching;
        public List<GameObject> SpawnPoints = new List<GameObject>();
        public List<ScriptAsset> ScriptAssets = new List<ScriptAsset>();
    }
}