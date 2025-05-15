using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Hypernex.CCK;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Emulator;
using Hypernex.CCK.Unity.Scripting;
using Hypernex.Game;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Nexport;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Hypernex.Networking
{
    public class HypernexInstance
    {
        public Action<string> OnClientConnect { get; set; } = userId => { };
        public Action<string, MsgMeta, MessageChannel> OnMessage { get; set; } = (userId, meta, channel) => { };
        public Action<string> OnClientDisconnect { get; set; } = userId => { };
        public bool IsOpen => true;
        public List<string> ConnectedClients => Init.Instance.users.Select(x => x.Id).ToList();
        public string InstanceId => String.Empty;
        public string WorldOwnerId => String.Empty;
        public string InstanceCreatorId => APIPlayer.APIUser.Id;
        public string[] Moderators => new List<string>(moderators).ToArray();

        public string HostId
        {
            get
            {
                if (ConnectedClients.Contains(InstanceCreatorId))
                    return InstanceCreatorId;
                return ConnectedClients[0];
            }
        }

        internal List<string> moderators = new();
        internal List<string> BannedUsers = new();
        internal List<string> SocketConnectedUsers = new();

        private Action<HypernexInstance> onStop;
        private int loadedScripts;
        private Timer joinTimer;

        public HypernexInstance(Action<HypernexInstance> initializedWorld,
            Action<(HypernexInstance,List<NexboxScript>)> scriptHandler, Action<HypernexInstance> OnStop = null)
        {
            onStop = OnStop;
            RegisterEvents();
            // TODO: Get Server Scripts
            List<NexboxScript> scripts = new List<NexboxScript>();
#if UNITY_EDITOR
            string s = EditorPrefs.GetString("WorldServerScript");
            if (!string.IsNullOrEmpty(s))
            {
                WorldServerScripts worldServerScripts = AssetDatabase.LoadAssetAtPath<WorldServerScripts>(s);
                foreach (ModuleScript moduleScript in worldServerScripts.ServerScripts)
                    scripts.Add(moduleScript);
            }
#endif
            scriptHandler.Invoke((this, scripts));
            initializedWorld.Invoke(this);
        }

        private void RegisterEvents()
        {
            OnClientConnect += identifier =>
            {
                GameInstance.FocusedInstance.OnClientConnect.Invoke(identifier);
            };
            OnMessage += (userId, meta, messageChannel) =>
            {
                if (meta.TypeOfData == typeof(WarnPlayer))
                {
                    if (moderators.Contains(userId))
                    {
                        WarnPlayer o = (WarnPlayer) Convert.ChangeType(meta.Data, typeof(WarnPlayer));
                        WarnPlayer warnPlayer = new WarnPlayer
                        {
                            targetUserId = o.targetUserId,
                            message = o.message
                        };
                        if(moderators.Contains(warnPlayer.targetUserId) && userId != InstanceCreatorId)
                            return;
                        SendMessageToClient(userId, Msg.Serialize(warnPlayer));
                    }
                }
                else if (meta.TypeOfData == typeof(KickPlayer))
                {
                    if (moderators.Contains(userId))
                    {
                        KickPlayer o = (KickPlayer) Convert.ChangeType(meta.Data, typeof(KickPlayer));
                        KickPlayer kickPlayer = new KickPlayer
                        {
                            targetUserId = o.targetUserId,
                            message = o.message
                        };
                        if(moderators.Contains(kickPlayer.targetUserId) && userId != InstanceCreatorId)
                            return;
                        KickUser(userId, Msg.Serialize(kickPlayer));
                    }
                }
                else if (meta.TypeOfData == typeof(BanPlayer))
                {
                    if (moderators.Contains(userId))
                    {
                        BanPlayer o = (BanPlayer) Convert.ChangeType(meta.Data, typeof(BanPlayer));
                        BanPlayer banPlayer = new BanPlayer
                        {
                            targetUserId = o.targetUserId,
                            message = o.message
                        };
                        if(moderators.Contains(banPlayer.targetUserId) && userId != InstanceCreatorId)
                            return;
                        BanUser(banPlayer.targetUserId, Msg.Serialize(banPlayer));
                    }
                }
                else if (meta.TypeOfData == typeof(UnbanPlayer))
                {
                    if (moderators.Contains(userId))
                    {
                        UnbanPlayer u = (UnbanPlayer) Convert.ChangeType(meta.Data, typeof(UnbanPlayer));
                        UnbanUser(u.targetUserId);
                    }
                }
                else if (meta.TypeOfData == typeof(AddModerator))
                {
                    if (moderators.Contains(userId))
                    {
                        AddModerator a = (AddModerator) Convert.ChangeType(meta.Data, typeof(AddModerator));
                        AddModerator(a.targetUserId);
                    }
                }
                else if (meta.TypeOfData == typeof(RemoveModerator))
                {
                    if (moderators.Contains(userId))
                    {
                        RemoveModerator r = (RemoveModerator) Convert.ChangeType(meta.Data, typeof(RemoveModerator));
                        if(moderators.Contains(r.targetUserId) && userId != InstanceCreatorId)
                            return;
                        RemoveModerator(r.targetUserId);
                    }
                }
            };
            OnClientDisconnect += identifier =>
            {
                GameInstance.FocusedInstance.OnClientDisconnect.Invoke(identifier);
            };
        }

        public void KickUser(string client, byte[] optionalMessage = null)
        {
            // TODO: Impl
        }

        public void BanUser(string userid, byte[] optionalMessage = null)
        {
            KickUser(userid, optionalMessage);
            if(!BannedUsers.Contains(userid))
                BannedUsers.Add(userid);
        }

        public void UnbanUser(string userid)
        {
            if (BannedUsers.Contains(userid))
                BannedUsers.Remove(userid);
        }

        public void AddModerator(string userid)
        {
            if(!moderators.Contains(userid))
                moderators.Add(userid);
        }

        public void RemoveModerator(string userid)
        {
            if (moderators.Contains(userid))
                moderators.Remove(userid);
        }

        public void SendMessageToClient(string id, byte[] message,
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            // TODO: Target client
            GameInstance.FocusedInstance.OnMessage.Invoke(Msg.GetMeta(message), messageChannel);
        }

        public void BroadcastMessage(byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            GameInstance.FocusedInstance.OnMessage.Invoke(Msg.GetMeta(message), messageChannel);
        }

        public void BroadcastMessageWithExclusion(string s, byte[] message,
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            GameInstance.FocusedInstance.OnMessage.Invoke(Msg.GetMeta(message), messageChannel);
        }
        
        private string GetListAsString<T>(List<T> l)
        {
            string g = String.Empty;
            foreach (T t in l)
                g += t + " ";
            return g;
        }
        
        public override string ToString() =>
            $"Id: {InstanceId}, Players: [{GetListAsString(SocketConnectedUsers)}], LoadedScripts: {loadedScripts}";
    }
}