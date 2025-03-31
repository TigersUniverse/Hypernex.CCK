using System;

namespace Hypernex.CCK.Unity.Internals
{
    public class UnityLogger : Logger
    {
        public override void Debug(object o) => UnityEngine.Debug.Log(o);
        public override void Log(object o) => UnityEngine.Debug.Log(o);
        public override void Warn(object o) => UnityEngine.Debug.LogWarning(o);
        public override void Error(object o) => UnityEngine.Debug.LogError(o);
        public override void Critical(Exception e) => UnityEngine.Debug.LogException(e);
    }
}