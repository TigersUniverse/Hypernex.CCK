using System.Collections.Generic;
using HypernexSharp.APIObjects;
using UnityEngine;

namespace Hypernex.Game
{
    public class PlayerManagement
    {
        private static Dictionary<GameInstance, List<NetPlayer>> players = new();
        public static Dictionary<GameInstance, List<NetPlayer>> Players => new(players);

        public static NetPlayer GetNetPlayer(GameInstance instance, string userid)
        {
            foreach (KeyValuePair<GameInstance,List<NetPlayer>> keyValuePair in Players)
            {
                if (keyValuePair.Key.gameServerId == instance.gameServerId && keyValuePair.Key.instanceId == instance.instanceId)
                    foreach (NetPlayer netPlayer in keyValuePair.Value)
                        if (netPlayer.UserId == userid)
                            return netPlayer;
            }
            return null;
        }
        
        public static void PlayerLeave(GameInstance gameInstance, string user)
        {
            if (!Players.ContainsKey(gameInstance))
                return;
            NetPlayer netPlayer = GetNetPlayer(gameInstance, user);
            if (netPlayer != null)
            {
                players[gameInstance].Remove(netPlayer);
                Object.Destroy(netPlayer.gameObject);
            }
            gameInstance.LocalScriptEvents?.OnUserLeave.Invoke(user);
            gameInstance.AvatarScriptEvents?.OnUserLeave.Invoke(user);
            if (!gameInstance.isHost) return;
            // Claim all NetworkSyncs that have Host Only
            foreach (GameObject rootGameObject in gameInstance.currentScene.GetRootGameObjects())
            {
                Transform[] ts = rootGameObject.GetComponentsInChildren<Transform>(true);
                foreach (Transform transform in ts)
                {
                    NetworkSync networkSync = transform.gameObject.GetComponent<NetworkSync>();
                    if (networkSync == null) continue;
                    if(networkSync.InstanceHostOnly)
                        networkSync.Claim();
                }
            }
        }
    }
}