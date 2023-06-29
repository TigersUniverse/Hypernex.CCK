using System;
using System.Collections.Generic;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    [Serializable]
    [RequireComponent(typeof(AssetIdentifier))]
    public class World : MonoBehaviour
    {
        public List<GameObject> SpawnPoints = new List<GameObject>();
        public List<NexboxScript> LocalScripts = new List<NexboxScript>();
        public List<NexboxScript> ServerScripts = new List<NexboxScript>();
        public List<ScriptAsset> ScriptAssets = new List<ScriptAsset>();
        public bool AllowRespawn = true;
        public float Gravity = -9.87f;
        public float JumpHeight = 1.0f;
        public float WalkSpeed = 5f;
        public float RunSpeed = 10f;
        public bool AllowRunning = true;

        public bool ReplaceImage;
        public Sprite Thumbnail;
        public List<Sprite> Icons = new List<Sprite>();
        public WorldMeta Meta;
    }
}