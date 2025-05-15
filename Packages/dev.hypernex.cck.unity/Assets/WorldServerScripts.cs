using System;
using Hypernex.CCK.Unity.Scripting;
using UnityEngine;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [CreateAssetMenu(fileName = "World Server Scripts", menuName = "Hypernex/World/ServerScripts")]
    public class WorldServerScripts : ScriptableObject
    {
        public ModuleScript[] ServerScripts;
    }
}