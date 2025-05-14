using System;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    public class ScriptAsset
    {
        public string AssetName;
        public Object Asset;
    }
}