using System;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    public class UnityLogger : Logger
    {
        public override void Log(object o) => Debug.Log(o);
        public override void Warn(object o) => Debug.LogWarning(o);
        public override void Error(object o) => Debug.LogError(o);
        public override void Critical(Exception e) => Debug.LogException(e);
    }
}