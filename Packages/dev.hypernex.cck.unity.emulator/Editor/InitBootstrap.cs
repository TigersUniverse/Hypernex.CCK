using System.Linq;
using Hypernex.CCK.Unity.Internals;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

namespace Hypernex.CCK.Unity.Emulator.Editor
{
    [InitializeOnLoad]
    public class InitBootstrap
    {
        static InitBootstrap()
        {
            PackageManager.AddScriptingDefineSymbol("HYPERNEX_CCK_EMULATOR");
            EditorApplication.playModeStateChanged += LogPlayModeState;
        }
        
        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if(state != PlayModeStateChange.EnteredPlayMode) return;
            GameObject init = new GameObject("Init");
            Init i = init.AddComponent<Init>();
            ReferenceAvatar(ref i);
        }

        private static void ReferenceAvatar(ref Init i)
        { 
            Avatar avatar = null;
            bool clone = true;
            string avatarGameObjectName = EditorPrefs.GetString("AvatarName");
            if (!string.IsNullOrEmpty(avatarGameObjectName))
            {
                clone = false;
                GameObject[] p = SceneManager.GetActiveScene().GetRootGameObjects()
                    .Where(x => x.name == avatarGameObjectName).ToArray();
                foreach (GameObject possibleAvatar in p)
                {
                    avatar = possibleAvatar.GetComponent<Avatar>();
                    if(avatar != null) break;
                }
            }
            if (avatar == null)
                clone = true;
            i.clone = clone;
            i.avatar = avatar;
        }
    }
}