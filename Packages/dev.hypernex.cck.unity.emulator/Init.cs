using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypernex.CCK;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Auth;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Game;
using Hypernex.Game.Avatar;
using Hypernex.Networking;
using Hypernex.Networking.Server;
using Hypernex.Sandboxing.SandboxedTypes;
using Hypernex.Tools;
using Hypernex.UI;
using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;

namespace Hypernex.CCK.Unity.Emulator
{
    [RequireComponent(typeof(DontDestroyMe))]
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class Init : MonoBehaviour, IDisposable
    {
        public static Init Instance { get; private set; }

        public Init() => Instance = this;

        public Material OutlineMaterial;
        public float SmoothingFrames = 0.1f;
        public RuntimeAnimatorController DefaultAvatarAnimatorController;
        public AudioMixerGroup AvatarGroup;
        public AudioMixerGroup WorldGroup;
        public VolumeProfile DefaultVolumeProfile;

        public HypernexInstance instance;
        private List<ScriptHandler> scriptHandlers = new List<ScriptHandler>();
        private GameInstance gameInstance;
        
        public readonly User[] users = new[] {UserAuth.Instance.user};
        public List<User> usersList;
        
        static Init()
        {
#if UNITY_EDITOR
            PackageManager.AddScriptingDefineSymbol("HYPERNEX_CCK_EMULATOR");
            EditorApplication.playModeStateChanged += LogPlayModeState;
#endif
        }

#if UNITY_EDITOR
        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if(state != PlayModeStateChange.EnteredPlayMode) return;
            GameObject init = new GameObject("Init");
            init.AddComponent<Init>();
        }
#endif
        
        private string GetYTDLLocation() => Path.Combine(AuthConfig.GetEditorConfigPath(), "ytdl");

        private void Start()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            World world = FindFirstObjectByType<World>(FindObjectsInactive.Include);
            Avatar avatar = null;
            bool clone = true;
#if UNITY_EDITOR
            string avatarGameObjectName = EditorPrefs.GetString("AvatarName");
            if (!string.IsNullOrEmpty(avatarGameObjectName))
            {
                clone = false;
                GameObject[] p = activeScene.GetRootGameObjects().Where(x => x.name == avatarGameObjectName).ToArray();
                foreach (GameObject possibleAvatar in p)
                {
                    avatar = possibleAvatar.GetComponent<Avatar>();
                    if(avatar != null) break;
                }
            }
#endif
            if (world == null && avatar == null) return;
            if (world == null)
            {
                GameObject worldObject = new GameObject("World");
                worldObject.transform.position = Vector3.up;
                world = worldObject.AddComponent<World>();
            }
            if (avatar == null)
                clone = true;
            usersList = users.ToList();
            OutlineMaterial = Resources.Load<Material>("OutlineMaterial");
            DefaultAvatarAnimatorController = Resources.Load<RuntimeAnimatorController>("CharacterController");
            AvatarGroup = Resources.Load<AudioMixer>("AvatarGroup").outputAudioMixerGroup;
            WorldGroup = Resources.Load<AudioMixer>("WorldGroup").outputAudioMixerGroup;
            DefaultVolumeProfile = Resources.Load<VolumeProfile>("DefaultVolumeProfile");
            GameObject thread = new GameObject("Thread");
            thread.AddComponent<DontDestroyMe>();
            thread.AddComponent<UnityMainThreadDispatcher>();
            thread.AddComponent<CoroutineRunner>();
            if (!Directory.Exists(GetYTDLLocation()))
                Directory.CreateDirectory(GetYTDLLocation());
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp.exe");
            Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg.exe");
#elif UNITY_MAC
        Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp_macos");
        Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg");
#else
        Streaming.ytdl.YoutubeDLPath = Path.Combine(GetYTDLLocation(), "yt-dlp");
        Streaming.ytdl.FFmpegPath = Path.Combine(GetYTDLLocation(), "ffmpeg");
#endif
            Streaming.ytdl.OutputFolder = Path.Combine(GetYTDLLocation(), "Downloads");
            YoutubeDLSharp.Utils.DownloadBinaries(true, GetYTDLLocation());
            SecurityTools.AllowExtraTypes();
            ExtraSandboxTools.ImplementRestrictions();
            kTools.Mirrors.Mirror.OnMirrorCreation += mirror => mirror.CustomCameraControl = true;
            // Create Events
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<DontDestroyMe>();
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<FirstPersonInputModule>();
            // Create LocalPlayer
            GameObject localPlayer = new GameObject("LocalPlayer");
            LocalPlayer player = localPlayer.AddComponent<LocalPlayer>();
            player.CharacterController = localPlayer.AddComponent<CharacterController>();
            player.CharacterController.height = AvatarCreator.CHARACTER_HEIGHT;
            player.CharacterController.center = new Vector3(0, 0.07f, 0);
            player.CharacterController.radius = 0.1f;
            player.CharacterController.minMoveDistance = 0;
            GameObject camerao = new GameObject("Camera");
            camerao.transform.parent = localPlayer.transform;
            camerao.transform.localPosition = new Vector3(0, 1, 0);
            Camera c = camerao.AddComponent<Camera>();
            player.Camera = c;
            Transform leftHand = new GameObject("LeftHand").transform;
            leftHand.parent = localPlayer.transform;
            leftHand.localPosition = new Vector3(-1, 0, 0);
            player.LeftHandReference = leftHand;
            Transform rightHand = new GameObject("RightHand").transform;
            rightHand.parent = localPlayer.transform;
            rightHand.localPosition = new Vector3(1, 0, 0);
            player.RightHandReference = rightHand;
            // Create Dashboard
            GameObject dashboardObject = new GameObject("Dashboard");
            dashboardObject.AddComponent<DontDestroyMe>();
            DashboardManager dashboardManager = dashboardObject.AddComponent<DashboardManager>();
            Transform container = new GameObject("Container").transform;
            container.parent = dashboardObject.transform;
            dashboardManager.Dashboard = container.gameObject;
            player.Dashboard = dashboardManager;
            // Init
            player.Refresh(activeScene);
            // Create game
            // TODO: Only do if testing world
            StartCoroutine(Create(activeScene, world, clone, avatar));
        }

        private IEnumerator Create(Scene scene, World world, bool clone, Avatar avatar = null)
        {
            yield return new WaitForSeconds(0.001f);
            if(avatar == null)
                avatar = Resources.Load<Avatar>("DefaultAvatar");
            LocalPlayer.Instance.Refresh(scene, avatar, false, clone);
            gameInstance = new GameInstance(world);
            instance = new HypernexInstance(_ => { }, scripts =>
            {
                foreach (NexboxScript nexboxScript in scripts.Item2)
                {
                    ScriptHandler s = new ScriptHandler(scripts.Item1);
                    scriptHandlers.Add(s);
                    s.LoadAndExecuteScript(nexboxScript, false);
                }
            });
            gameInstance.SetupClient();
        }

        private void FixedUpdate()
        {
            gameInstance?.FixedUpdate();
        }

        private void Update()
        {
            gameInstance?.Update();
        }

        private void LateUpdate()
        {
            gameInstance?.LateUpdate();
        }

        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            if(gameInstance != null)
                gameInstance.Dispose();
            foreach (ScriptHandler scriptHandler in scriptHandlers)
                scriptHandler.Dispose();
        }
    }
}