using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Emulator;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Player;
using Hypernex.Sandboxing;
using Hypernex.Sandboxing.SandboxedTypes.Handlers;
using Hypernex.Tools;
using HypernexSharp.APIObjects;
using HypernexSharp.Socketing.SocketResponses;
using HypernexSharp.SocketObjects;
using Nexport;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Physics = UnityEngine.Physics;
using World = Hypernex.CCK.Unity.Assets.World;
using Security = Hypernex.CCK.Unity.Internals.Security;

namespace Hypernex.Game
{
    public class GameInstance
    {
        public static GameInstance FocusedInstance { get; internal set; }

        private static List<GameInstance> gameInstances = new();
        public static GameInstance[] GameInstances => gameInstances.ToArray();

        public static Action<GameInstance, World, Scene> OnGameInstanceLoaded { get; set; } =
            (instance, meta, scene) => { };
        public static Action<GameInstance> OnGameInstanceDisconnect { get; set; } = instance => { };
        
        public static User[] GetConnectedUsers(GameInstance gameInstance, bool f = true) => Init.Instance.users;

        // In editor, the GameInstance will always be the Focused
        public static GameInstance GetInstanceFromScene(Scene scene) => FocusedInstance;

        public Action OnConnect { get; set; } = () => { };
        public Action<string> OnClientConnect { get; set; } = user => { };
        public Action<MsgMeta, MessageChannel> OnMessage { get; set; } = (meta, channel) => { };
        public Action<string> OnClientDisconnect { get; set; } = identifier => { };
        public Action OnDisconnect { get; set; } = () => { };

        public bool IsOpen => true;
        public List<User> ConnectedUsers => Init.Instance.usersList;
        
        public bool CanInvite
        {
            get
            {
                if (instanceCreatorId == APIPlayer.APIUser.Id)
                    return true;
                switch (Publicity)
                {
                    case InstancePublicity.Anyone:
                        return true;
                    case InstancePublicity.Acquaintances:
                    case InstancePublicity.Friends:
                    case InstancePublicity.OpenRequest:
                        return true;
                    case InstancePublicity.ModeratorRequest:
                        return Init.Instance.instance.Moderators.Contains(APIPlayer.APIUser.Id);
                    case InstancePublicity.ClosedRequest:
                        return false;
                }
                return false;
            }
        }

        public bool IsModerator => Init.Instance.instance.Moderators.Contains(APIPlayer.APIUser.Id);

        public string gameServerId;
        public string instanceId;
        public string userIdToken;
        public WorldMeta worldMeta;
        public World World;
        public User host;
        public Texture2D Thumbnail;
        public InstancePublicity Publicity;
        public bool lockAvatarSwitching;

        internal Scene currentScene;
        internal bool authed;
        internal List<Sandbox> sandboxes = new ();
        public readonly string instanceCreatorId;

        internal bool isHost => true;
        private List<User> usersBeforeMe = new ();
        private bool isDisposed;
        internal ScriptEvents LocalScriptEvents;
        internal ScriptEvents AvatarScriptEvents;
        private Volume[] volumes;

        public GameInstance(World world)
        {
            FocusedInstance = this;
            currentScene = SceneManager.GetActiveScene();
            World = world;
            if(World == null) return;
            host = APIPlayer.APIUser;
            LocalPlayer.Instance.Respawn(currentScene);
            volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(x => x.gameObject.scene == currentScene).ToArray();
            LoadScene();
        }
        
        public void SetupClient()
        {
            OnClientConnect += user =>
            {
                LocalScriptEvents?.OnUserJoin.Invoke(user);
                AvatarScriptEvents?.OnUserJoin.Invoke(user);
            };
            OnMessage += (meta, channel) => MessageHandler.HandleMessage(this, meta, channel);
            OnClientDisconnect += user => PlayerManagement.PlayerLeave(this, user);
            OnDisconnect += Dispose;
        }
        
        public void SendMessage(string name, byte[] message, MessageChannel messageChannel = MessageChannel.Reliable)
        {
            Init.Instance.instance.OnMessage.Invoke(LocalPlayer.Instance.Id, Msg.GetMeta(message), messageChannel);
        }

        public void WarnUser(User user, string message)
        {
            if(!IsModerator)
                return;
            WarnPlayer warnPlayer = new WarnPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(typeof(WarnPlayer).FullName, Msg.Serialize(warnPlayer));
        }

        public void KickUser(User user, string message)
        {
            if (!IsModerator)
                return;
            KickPlayer kickPlayer = new KickPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(typeof(KickPlayer).FullName, Msg.Serialize(kickPlayer));
        }

        public void BanUser(User user, string message)
        {
            if (!IsModerator)
                return;
            BanPlayer banPlayer = new BanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id,
                message = message
            };
            SendMessage(typeof(BanPlayer).FullName, Msg.Serialize(banPlayer));
        }

        public void UnbanUser(User user)
        {
            if (!IsModerator)
                return;
            UnbanPlayer unbanPlayer = new UnbanPlayer
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(typeof(UnbanPlayer).FullName, Msg.Serialize(unbanPlayer));
        }

        public void AddModerator(User user)
        {
            if (!IsModerator)
                return;
            AddModerator addModerator = new AddModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(typeof(AddModerator).FullName, Msg.Serialize(addModerator));
        }
        
        public void RemoveModerator(User user)
        {
            if (!IsModerator)
                return;
            RemoveModerator removeModerator = new RemoveModerator
            {
                Auth = new JoinAuth
                {
                    UserId = APIPlayer.APIUser.Id,
                    TempToken = userIdToken
                },
                targetUserId = user.Id
            };
            SendMessage(typeof(RemovedModerator).FullName, Msg.Serialize(removeModerator));
        }

        private void LoadScene()
        {
            Security.RemoveOffendingItems(currentScene, SecurityTools.AdditionalAllowedWorldTypes.ToArray());
            Security.ApplyComponentRestrictions(currentScene);
            LocalPlayer.Instance.Camera.gameObject.AddComponent<AudioListener>();
            FocusedInstance = this;
            LocalScriptEvents = new ScriptEvents(SandboxRestriction.Local);
            AvatarScriptEvents = new ScriptEvents(SandboxRestriction.LocalAvatar);
            foreach (LocalScript ls in Object.FindObjectsByType<LocalScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Transform r = AnimationUtility.GetRootOfChild(ls.transform);
                if(r.GetComponent<LocalPlayer>() == null && r.GetComponent<NetPlayer>() == null)
                    sandboxes.Add(new Sandbox(ls.Script, this, ls.gameObject));
            }
            if (LocalPlayer.Instance.Dashboard.IsVisible)
                LocalPlayer.Instance.Dashboard.ToggleDashboard(LocalPlayer.Instance);
        }

        internal void FixedUpdate() => sandboxes.ForEach(x => x.InstanceContainer.Runtime.FixedUpdate());

        internal void Update()
        {
            sandboxes.ForEach(x => x.InstanceContainer.Runtime.Update());
            volumes.SelectVolume();
        }
        
        internal void LateUpdate() => sandboxes.ForEach(x => x.InstanceContainer.Runtime.LateUpdate());

        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            FocusedInstance = null;
            Physics.gravity = new Vector3(0, LocalPlayer.Instance.Gravity, 0);
            sandboxes.ForEach(x => x.Dispose());
            sandboxes.Clear();
            DynamicGI.UpdateEnvironment();
            Array.Empty<Volume>().SelectVolume();
            VolumeManager.instance.SetGlobalDefaultProfile(Init.Instance.DefaultVolumeProfile);
            if(gameInstances.Contains(this))
                gameInstances.Remove(this);
            OnGameInstanceDisconnect.Invoke(this);
        }
    }
}