using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity
{
    [Serializable]
    public class ScriptAsset
    {
        public string AssetName;
        public Object Asset;
    }
}