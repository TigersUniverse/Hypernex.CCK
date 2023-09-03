using UnityEngine;

namespace Hypernex.CCK.Unity
{
    public class LocalScript : MonoBehaviour
    {
        public NexboxScript NexboxScript = new NexboxScript(NexboxLanguage.Unknown, ""){Name = "MyScript"};
    }
}