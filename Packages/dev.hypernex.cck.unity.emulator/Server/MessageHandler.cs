using System;
using System.Collections.Generic;
using Hypernex.CCK;
using Hypernex.Networking.Messages;
using Hypernex.Networking.Messages.Bulk;
using Hypernex.Networking.Messages.Data;
using Hypernex.Networking.Server.SandboxedClasses;
using Nexport;

namespace Hypernex.Networking.Server
{
    public static class MessageHandler
    {
        public static void HandleMessage(HypernexInstance instance, MsgMeta msgMeta, MessageChannel channel,
            string from)
        {
            switch (msgMeta.DataId)
            {
                case "Hypernex.Networking.Messages.PlayerUpdate":
                {
                    PlayerUpdate playerUpdate = (PlayerUpdate) Convert.ChangeType(msgMeta.Data, typeof(PlayerUpdate));
                    PlayerHandler.HandlePlayerUpdate(instance, playerUpdate, from);
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerDataUpdate":
                {
                    PlayerDataUpdate playerDataUpdate = (PlayerDataUpdate) Convert.ChangeType(msgMeta.Data, typeof(PlayerDataUpdate));
                    PlayerHandler.HandlePlayerDataUpdate(instance, playerDataUpdate, from);
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerObjectUpdate":
                {
                    PlayerObjectUpdate playerObjectUpdate = (PlayerObjectUpdate) Convert.ChangeType(msgMeta.Data, typeof(PlayerObjectUpdate));
                    PlayerHandler.HandlePlayerObjectUpdate(instance, playerObjectUpdate, from);
                    break;
                }
                case "Hypernex.Networking.Messages.WeightedObjectUpdate":
                {
                    WeightedObjectUpdate weightedObjectUpdate =
                        (WeightedObjectUpdate) Convert.ChangeType(msgMeta.Data, typeof(WeightedObjectUpdate));
                    PlayerHandler.HandleWeightedObjectUpdate(instance, weightedObjectUpdate, from);
                    break;
                }
                case "Hypernex.Networking.Messages.Bulk.BulkWeightedObjectUpdate":
                {
                    BulkWeightedObjectUpdate weightedObjectUpdates =
                        (BulkWeightedObjectUpdate) Convert.ChangeType(msgMeta.Data, typeof(BulkWeightedObjectUpdate));
                    PlayerHandler.HandleWeightedObjectUpdate(instance, weightedObjectUpdates, from);
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerVoice":
                {
                    PlayerVoice playerVoice = (PlayerVoice) Convert.ChangeType(msgMeta.Data, typeof(PlayerVoice));
                    PlayerHandler.HandlePlayerVoice(instance, playerVoice, from);
                    break;
                }
                case "Hypernex.Networking.Messages.PlayerMessage":
                {
                    PlayerMessage playerMessage = (PlayerMessage) Convert.ChangeType(msgMeta.Data, typeof(PlayerMessage));
                    playerMessage!.Auth.TempToken = String.Empty;
                    instance.BroadcastMessageWithExclusion(from, Msg.Serialize(playerMessage));
                    break;
                }
                case "Hypernex.Networking.Messages.WorldObjectUpdate":
                {
                    WorldObjectUpdate worldObjectUpdate =
                        (WorldObjectUpdate) Convert.ChangeType(msgMeta.Data, typeof(WorldObjectUpdate));
                    worldObjectUpdate!.Auth.TempToken = String.Empty;
                    ObjectHandler.HandleObjectUpdateMessage(instance, worldObjectUpdate, from, channel);
                    break;
                }
                case "Hypernex.Networking.Messages.ServerConsoleExecute":
                {
                    ServerConsoleExecute serverConsoleExecute =
                        (ServerConsoleExecute) Convert.ChangeType(msgMeta.Data, typeof(ServerConsoleExecute));
                    ScriptHandler scriptHandler = new ScriptHandler(instance);
                    scriptHandler.LoadAndExecuteScript(
                        new NexboxScript(serverConsoleExecute.Language, serverConsoleExecute.ScriptText)
                            {Name = "console"});
                    break;
                }
            }
        }

        internal static class PlayerHandler
        {
            internal static Dictionary<string, PlayerUpdate> PlayerUpdates => new (_playerUpdates);
            private static Dictionary<string, PlayerUpdate> _playerUpdates = new ();

            internal static Dictionary<string, PlayerDataUpdate> PlayerDataUpdates => new(_playerDatas);
            private static Dictionary<string, PlayerDataUpdate> _playerDatas = new();

            public static void HandlePlayerUpdate(HypernexInstance instance, PlayerUpdate playerUpdate, string from)
            {
                playerUpdate.Auth.TempToken = String.Empty;
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(playerUpdate), MessageChannel.UnreliableSequenced);
                if (_playerUpdates.ContainsKey(playerUpdate.Auth.UserId))
                {
                    _playerUpdates[playerUpdate.Auth.UserId] = playerUpdate;
                    return;
                }
                _playerUpdates.Add(playerUpdate.Auth.UserId, playerUpdate);
                ScriptHandler.GetScriptHandlerFromInstance(instance)?.Events.OnPlayerUpdate.Invoke(playerUpdate.Auth.UserId,
                    playerUpdate.IsPlayerVR, playerUpdate.AvatarId, playerUpdate.IsSpeaking, playerUpdate.IsFBT,
                    playerUpdate.VRIKJson);
            }

            public static void HandlePlayerDataUpdate(HypernexInstance instance, PlayerDataUpdate playerDataUpdate,
                string from)
            {
                playerDataUpdate.Auth.TempToken = String.Empty;
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(playerDataUpdate), MessageChannel.UnreliableSequenced);
                if (_playerDatas.ContainsKey(playerDataUpdate.Auth.UserId))
                {
                    _playerDatas[playerDataUpdate.Auth.UserId] = playerDataUpdate;
                    return;
                }
                _playerDatas.Add(playerDataUpdate.Auth.UserId, playerDataUpdate);
                ScriptHandler scriptHandler = ScriptHandler.GetScriptHandlerFromInstance(instance);
                if(scriptHandler == null) return;
                scriptHandler.Events.OnPlayerTags.Invoke(playerDataUpdate.Auth.UserId,
                    playerDataUpdate.PlayerAssignedTags.ToArray());
                foreach (KeyValuePair<string, object> keyValuePair in playerDataUpdate.ExtraneousData)
                    scriptHandler.Events.OnExtraneousObject.Invoke(playerDataUpdate.Auth.UserId, keyValuePair.Key,
                        keyValuePair.Value);
            }

            public static void HandlePlayerObjectUpdate(HypernexInstance instance, PlayerObjectUpdate playerObjectUpdate,
                string userId)
            {
                playerObjectUpdate.Auth.TempToken = String.Empty;
                if (!StabilityTools.CheckFloats(playerObjectUpdate))
                {
                    Logger.CurrentLogger.Warn($"Banned {userId} for invalid floats");
                    instance.BanUser(userId);
                    return;
                }
                instance.BroadcastMessageWithExclusion(userId, Msg.Serialize(playerObjectUpdate), MessageChannel.Unreliable);
                ScriptHandler scriptHandler = ScriptHandler.GetScriptHandlerFromInstance(instance);
                if(scriptHandler == null) return;
                foreach (KeyValuePair<int, NetworkedObject> keyValuePair in playerObjectUpdate.Objects)
                    scriptHandler.Events.OnPlayerObject.Invoke(playerObjectUpdate.Auth.UserId, keyValuePair.Key,
                        new OfflineNetworkedObject(keyValuePair.Value));
            }

            public static void HandleWeightedObjectUpdate(HypernexInstance instance,
                WeightedObjectUpdate weightedObjectUpdate, string from)
            {
                weightedObjectUpdate.Auth.TempToken = String.Empty;
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(weightedObjectUpdate),
                    MessageChannel.Unreliable);
                ScriptHandler.GetScriptHandlerFromInstance(instance)?.Events.OnWeightedObject.Invoke(
                    weightedObjectUpdate.Auth.UserId, weightedObjectUpdate.WeightIndex, weightedObjectUpdate.Weight);
            }
            
            public static void HandleWeightedObjectUpdate(HypernexInstance instance,
                BulkWeightedObjectUpdate weightedObjectUpdates, string from)
            {
                weightedObjectUpdates.Auth.TempToken = String.Empty;
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(weightedObjectUpdates),
                    MessageChannel.UnreliableSequenced);
                ScriptHandler scriptHandler = ScriptHandler.GetScriptHandlerFromInstance(instance);
                if(scriptHandler == null) return;
                foreach (WeightedObjectUpdate weightedObjectUpdate in weightedObjectUpdates.WeightedObjectUpdates)
                    scriptHandler.Events.OnWeightedObject.Invoke(weightedObjectUpdates.Auth.UserId,
                        weightedObjectUpdate.WeightIndex, weightedObjectUpdate.Weight);
            }

            public static void HandlePlayerVoice(HypernexInstance instance, PlayerVoice playerVoice, string from)
            {
                playerVoice.Auth.TempToken = String.Empty;
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(playerVoice), MessageChannel.UnreliableSequenced);
            }
        }

        internal static class ObjectHandler
        {
            private static Dictionary<string, Dictionary<string, List<WorldObjectUpdate>>> objects = new();
            public static Dictionary<string, Dictionary<string, List<WorldObjectUpdate>>> Objects => new(objects);

            internal static void RemoveInstanceFromWorldObjects(HypernexInstance instance)
            {
                if (Objects.ContainsKey(instance.InstanceId))
                    objects.Remove(instance.InstanceId);
            }

            internal static void RemovePlayerFromWorldObjects(HypernexInstance instance, string userid)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return;
                objects[instance.InstanceId].Remove(userid);
            }

            private static Dictionary<string, List<WorldObjectUpdate>> GetInstance(HypernexInstance instance)
            {
                if(!Objects.ContainsKey(instance.InstanceId))
                    objects.Add(instance.InstanceId, new Dictionary<string, List<WorldObjectUpdate>>());
                return objects[instance.InstanceId];
            }

            private static bool IsObjectClaimed(HypernexInstance instance, WorldObjectUpdate worldObjectUpdate)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                foreach (KeyValuePair<string,List<WorldObjectUpdate>> inp in dic)
                    foreach (WorldObjectUpdate objectUpdate in inp.Value)
                        if (objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation)
                            return true;
                return false;
            }

            private static bool CanObjectBeStolen(HypernexInstance instance, WorldObjectUpdate worldObjectUpdate)
            {
                if (!IsObjectClaimed(instance, worldObjectUpdate))
                    return true;
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                foreach (List<WorldObjectUpdate> inp in dic.Values)
                foreach (WorldObjectUpdate objectUpdate in inp)
                    if (objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation && objectUpdate.CanBeStolen)
                        return true;
                return false;
            }

            private static bool IsObjectClaimedByUser(HypernexInstance instance, string userid,
                WorldObjectUpdate worldObjectUpdate)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return false;
                foreach (WorldObjectUpdate objectUpdate in dic[userid])
                    if (objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation)
                        return true;
                return false;
            }

            private static void ClaimObject(HypernexInstance instance, string userid, WorldObjectUpdate worldObjectUpdate)
            {
                //worldObjectUpdate.Action = WorldObjectAction.Claim;
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return;
                if (IsObjectClaimed(instance, worldObjectUpdate))
                {
                    // Remove it first
                    foreach (string dicKey in dic.Keys)
                        RemoveObject(instance, dicKey, worldObjectUpdate);
                }
                objects[instance.InstanceId][userid].Add(worldObjectUpdate);
            }

            private static WorldObjectUpdate GetObjectByPath(HypernexInstance instance, string path)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                foreach (List<WorldObjectUpdate> inp in dic.Values)
                foreach (WorldObjectUpdate objectUpdate in inp)
                    if (objectUpdate.Object.ObjectLocation == path)
                        return objectUpdate;
                return null;
            }

            private static void UpdateObject(HypernexInstance instance, string userid, WorldObjectUpdate worldObjectUpdate)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return;
                int i = 0;
                foreach (WorldObjectUpdate objectUpdate in new List<WorldObjectUpdate>(dic[userid]))
                {
                    if (objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation)
                    {
                        objects[instance.InstanceId][userid].RemoveAt(i);
                        objects[instance.InstanceId][userid].Add(worldObjectUpdate);
                    }
                    i++;
                }
            }

            private static void MakeObjectClaimable(HypernexInstance instance, string userid, WorldObjectUpdate worldObjectUpdate)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return;
                int i = 0;
                foreach (WorldObjectUpdate objectUpdate in new List<WorldObjectUpdate>(dic[userid]))
                {
                    if (objectUpdate.Auth.UserId == userid &&
                        objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation)
                    {
                        objects[instance.InstanceId][userid][i].CanBeStolen = true;
                        objects[instance.InstanceId][userid][i].Auth.UserId = String.Empty;
                    }
                    i++;
                }
            }

            private static void RemoveObject(HypernexInstance instance, string userid, WorldObjectUpdate worldObjectUpdate)
            {
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if (!dic.ContainsKey(userid))
                    return;
                int i = 0;
                foreach (WorldObjectUpdate objectUpdate in new List<WorldObjectUpdate>(dic[userid]))
                {
                    if (objectUpdate.Auth.UserId == userid &&
                        objectUpdate.Object.ObjectLocation == worldObjectUpdate.Object.ObjectLocation)
                        objects[instance.InstanceId][userid].RemoveAt(i);
                    i++;
                }
            }

            private static void BroadcastMessage(HypernexInstance instance, WorldObjectUpdate worldObjectUpdate,
                string from, MessageChannel messageChannel) =>
                instance.BroadcastMessageWithExclusion(from, Msg.Serialize(worldObjectUpdate), messageChannel);

            public static void HandleObjectUpdateMessage(HypernexInstance instance, WorldObjectUpdate worldObjectUpdate,
                string from, MessageChannel messageChannel)
            {
                string userid = worldObjectUpdate.Auth.UserId;
                Dictionary<string, List<WorldObjectUpdate>> dic = GetInstance(instance);
                if(!dic.ContainsKey(userid))
                    dic.Add(userid, new List<WorldObjectUpdate>());
                switch (worldObjectUpdate.Action)
                {
                    case WorldObjectAction.Claim:
                        bool notClaimed = !IsObjectClaimed(instance, worldObjectUpdate);
                        bool stealable = CanObjectBeStolen(instance, worldObjectUpdate);
                        if (notClaimed || stealable)
                        {
                            WorldObjectUpdate oldUpdate = GetObjectByPath(instance, worldObjectUpdate.Object.ObjectLocation);
                            if (stealable && oldUpdate != null && worldObjectUpdate.Auth.UserId != oldUpdate.Auth.UserId)
                            {
                                oldUpdate.Action = WorldObjectAction.Unclaim;
                                instance.SendMessageToClient(oldUpdate.Auth.UserId, Msg.Serialize(oldUpdate));
                            }
                            ClaimObject(instance, userid, worldObjectUpdate);
                            BroadcastMessage(instance, worldObjectUpdate, from, messageChannel);
                        }
                        break;
                    case WorldObjectAction.Update:
                        if (IsObjectClaimedByUser(instance, userid, worldObjectUpdate))
                        {
                            UpdateObject(instance, userid, worldObjectUpdate);
                            BroadcastMessage(instance, worldObjectUpdate, from, messageChannel);
                        }
                        break;
                    case WorldObjectAction.Unclaim:
                        if(IsObjectClaimedByUser(instance, userid, worldObjectUpdate))
                        {
                            MakeObjectClaimable(instance, userid, worldObjectUpdate);
                            BroadcastMessage(instance, worldObjectUpdate, from, messageChannel);
                        }
                        break;
                }
            }
        }
    }
}