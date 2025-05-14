using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Auth;
using Hypernex.Game.Avatar;
using Hypernex.Game.Avatar.FingerInterfacing;
using Hypernex.Game.Networking;
using Hypernex.Tools;
using Hypernex.UI;
using HypernexSharp.APIObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.Game
{
    [RequireComponent(typeof(DontDestroyMe))]
    public class LocalPlayer : MonoBehaviour, IPlayer
    {
        public static LocalPlayer Instance { get; private set; }
        public static bool IsVR => false;
        public static readonly List<string> MorePlayerAssignedTags = new();
        public static readonly Dictionary<string, object> MoreExtraneousObjects = new();

        public LocalPlayer() => Instance = this;

        public CharacterController CharacterController;
        public Camera Camera;
        public Transform LeftHandReference;
        public Transform RightHandReference;
        public List<IBinding> Bindings = new();
        public List<PathDescriptor> SavedTransforms = new List<PathDescriptor>();
        public LocalPlayerSyncController LocalPlayerSyncController;
        public DashboardManager Dashboard;
        public float LowestPointRespawnThreshold = 50f;
        public DesktopFingerCurler.Left LeftDesktopCurler = new();
        public DesktopFingerCurler.Right RightDesktopCurler = new();
        
        public Vector3 LowestPoint;
        public LocalAvatarCreator avatar;
        public AvatarMeta avatarMeta;

        private Scene currentScene;

        public void Start()
        {
            currentScene = SceneManager.GetActiveScene();
            LowestPoint = AnimationUtility.GetLowestObject(currentScene).position;
            LocalPlayerSyncController = new LocalPlayerSyncController(this, i => StartCoroutine(i));
            CreateDesktopBindings();
        }

        public bool IsLocal => true;
        public string Id => UserAuth.Instance.user.Id;
        public AvatarCreator AvatarCreator { get; }
        public bool IsLoadingAvatar => false;
        public float AvatarDownloadPercentage => 0;

        internal void Refresh(Scene s, CCK.Unity.Assets.Avatar a = null, bool refresh = true, bool clone = true)
        {
            if (a != null)
            {
                avatar?.Dispose();
                Scale(1);
                avatar = new LocalAvatarCreator(this, a, IsVR, clone);
                Dashboard.PositionDashboard(this);
            }
            SavedTransforms.Clear();
            foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
            {
                if(SavedTransforms.Count(x => x.transform == t) > 0) continue;
                PathDescriptor pathDescriptor = t.gameObject.GetComponent<PathDescriptor>();
                if (pathDescriptor == null)
                    pathDescriptor = t.gameObject.AddComponent<PathDescriptor>();
                pathDescriptor.root = transform;
                SavedTransforms.Add(pathDescriptor);
            }
            if(refresh)
                Respawn(s);
        }

        public void LoadAvatar(string avatarId)
        {
            
        }

        public void Respawn(Scene? s = null)
        {
            Vector3 spawnPosition = new Vector3(0, 1, 0);
            if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World.SpawnPoints.Count > 0)
            {
                Transform spT = GameInstance.FocusedInstance.World
                    .SpawnPoints[new System.Random().Next(0, GameInstance.FocusedInstance.World.SpawnPoints.Count - 1)]
                    .transform;
                spawnPosition = spT.position;
            }
            else
            {
                GameObject searchSpawn;
                if (s == null)
                    searchSpawn = SceneManager.GetActiveScene().GetRootGameObjects()
                        .FirstOrDefault(x => x.name.ToLower() == "spawn");
                else
                    searchSpawn = s.Value.GetRootGameObjects().FirstOrDefault(x => x.name.ToLower() == "spawn");
                if (searchSpawn != null)
                    spawnPosition = searchSpawn.transform.position;
                else if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    spawnPosition = GameInstance.FocusedInstance.World.transform.position;
            }
            CharacterController.enabled = false;
            transform.position = spawnPosition.AddOneUp();
            if(Dashboard.IsVisible)
                Dashboard.PositionDashboard(this);
            CharacterController.enabled = true;
        }
        
        public void Scale(float f)
        {
            transform.localScale = new Vector3(f, f, f);
        }
        
        private void CreateDesktopBindings()
        {
            Bindings.Add(new Bindings.Keyboard()
                .RegisterCustomKeyDownEvent(KeyCode.C, () =>
                {
                    if(LockMovement || !groundedPlayer) return;
                    avatar?.SetCrouch(!avatar?.IsCrouched ?? false);
                })
                .RegisterCustomKeyDownEvent(KeyCode.X, () =>
                {
                    if(LockMovement || !groundedPlayer) return;
                    avatar?.SetCrawl(!avatar?.IsCrawling ?? false);
                }));
            Bindings.Add(new Bindings.Mouse());
            Bindings[1].Button2Click += () => Dashboard.ToggleDashboard(this);
        }
        
        private float _walkSpeed = 5f;
        public float WalkSpeed
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.WalkSpeed;
                return _walkSpeed;
            }
            set => _walkSpeed = value;
        }

        private float _runSpeed = 10f;
        public float RunSpeed
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.RunSpeed;
                return _runSpeed;
            }
            set => _runSpeed = value;
        }
        
        private float _jumpHeight = 1.0f;
        public float JumpHeight
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.JumpHeight;
                return _jumpHeight;
            }
            set => _jumpHeight = value;
        }
        
        private float _gravity = -9.87f;
        public float Gravity
        {
            get
            {
                if (GameInstance.FocusedInstance != null && GameInstance.FocusedInstance.World != null)
                    return GameInstance.FocusedInstance.World.Gravity;
                return _gravity;
            }
            set => _gravity = value;
        }
        
        public bool LockMovement { get; set; }
        public bool LockCamera { get; set; }
        
        public IGestureIdentifier GestureIdentifier => FingerCalibration.DefaultGestures;
        
        internal IFingerCurler GetLeftHandCurler()
        {
            return LeftDesktopCurler;
        }

        internal IFingerCurler GetRightHandCurler()
        {
            return RightDesktopCurler;
        }
        
        private float rotx;
        private float s_;
        private bool isRunning;
        private bool groundedPlayer;
        private float verticalVelocity;
        private float groundedTimer;
        private bool CanRun => (!avatar?.IsCrawling ?? true) && (!avatar?.IsCrouched ?? true);
        
        private (Vector3, bool, bool, Vector2)? HandleLeftBinding(IBinding binding, bool vr)
        {
            // Left-Hand
            Vector3 move;
            if (vr)
                move = Camera.transform.forward * (binding.Up + binding.Down * -1) +
                       Camera.transform.right * (binding.Left * -1 + binding.Right);
            else
                move = transform.forward * (binding.Up + binding.Down * -1) +
                       transform.right * (binding.Left * -1 + binding.Right);
            move = Vector3.ClampMagnitude(move, 1);
            if(!vr)
            {
                isRunning = binding.Button2 && CanRun;
                s_ = isRunning ? RunSpeed : WalkSpeed;
            }
            if (GameInstance.FocusedInstance != null)
                if(GameInstance.FocusedInstance.World != null)
                    if (!GameInstance.FocusedInstance.World.AllowRunning)
                        s_ = WalkSpeed;
            return (move * s_, binding.Button,
                binding.Up > 0.01f || binding.Down > 0.01f || binding.Left > 0.01f || binding.Right > 0.01f,
                new(binding.Right - binding.Left, binding.Up - binding.Down));
        }

        private (Vector3, bool, bool)? HandleRightBinding(IBinding binding, bool vr)
        {
            if (!LockCamera && binding.Id == "Mouse" && !IsVR)
            {
                transform.Rotate(0, (binding.Left * -1 + binding.Right) * ((Bindings.Mouse)binding).Sensitivity, 0);
                rotx += -(binding.Up + binding.Down * -1) * ((Bindings.Mouse) binding).Sensitivity;
                rotx = Mathf.Clamp(rotx, -90f, 90f);
                Camera.transform.localEulerAngles = new Vector3(rotx, 0, 0);
                return (Vector3.zero, binding.Button, false);
            }
            /*if (!LockCamera)
            {
                const float NEEDED_TURN = 0.8f;
                // Right-Hand
                if (ConfigManager.SelectedConfigUser != null && ConfigManager.SelectedConfigUser.UseSnapTurn)
                {
                    float amountTurn = binding.Left * -1 + binding.Right;
                    if (!didSnapTurn && (amountTurn > NEEDED_TURN || amountTurn < -NEEDED_TURN))
                    {
                        float val = 1f;
                        if (amountTurn < 0)
                            val = -1f;
                        float turnDegree = 45f;
                        if (ConfigManager.SelectedConfigUser != null)
                            turnDegree = ConfigManager.SelectedConfigUser.SnapTurnAngle;
                        transform.Rotate(0, turnDegree * val, 0);
                        didSnapTurn = true;
                    }
                    else if (didSnapTurn && (amountTurn < NEEDED_TURN && amountTurn > -NEEDED_TURN))
                        didSnapTurn = false;
                }
                else
                {
                    float turnSpeed = 1;
                    if (ConfigManager.SelectedConfigUser != null)
                        turnSpeed = ConfigManager.SelectedConfigUser.SmoothTurnSpeed;
                    transform.Rotate(0, (binding.Left * -1 + binding.Right) * Time.deltaTime * 100 * turnSpeed, 0);
                }
                if(vr)
                {
                    isRunning = (binding.Up > NEEDED_TURN || binding.Down > NEEDED_TURN) && CanRun;
                    s_ = isRunning ? RunSpeed : WalkSpeed;
                }
                if (LockMovement)
                    return null;
                return (Vector3.zero, binding.Button, false);
            }*/
            if (LockMovement)
                return null;
            return (Vector3.zero, binding.Button, false);
        }

        private bool areTwoTriggersClicked()
        {
            bool left = false;
            bool right = false;
            foreach (IBinding binding in Bindings)
            {
                if (binding.IsLook && binding.Trigger >= 0.8f)
                    left = true;
                if (!binding.IsLook && binding.Trigger >= 0.8f)
                    right = true;
            }
            return left && right;
        }

        private void FixedUpdate() => avatar?.FixedUpdate();

        private void Update()
        {
            // TODO: Reduce GC
            bool vr = IsVR;
            CursorTools.ToggleMouseLock(vr || LockCamera);
            CursorTools.ToggleMouseVisibility(!vr || LockCamera);
            groundedPlayer = CharacterController.isGrounded;
            if (!LockMovement)
            {
                if (groundedPlayer)
                    groundedTimer = 0.2f;
                if (groundedTimer > 0)
                    groundedTimer -= Time.deltaTime;
                if (groundedPlayer && verticalVelocity < 0)
                    verticalVelocity = 0f;
                verticalVelocity += Gravity * Time.deltaTime;
            }
            (Vector3, bool, bool, Vector2)? left_m = null;
            (Vector3, bool, bool)? right_m = null;
            foreach (IBinding binding in Bindings)
            {
                binding.Update();
                bool g = !binding.IsLook;
                if (vr)
                    g = binding.IsLook;
                if (g)
                {
                    (Vector3, bool, bool, Vector2)? r = HandleLeftBinding(binding, vr);
                    if (r != null)
                        left_m = r.Value;
                }
                else
                {
                    (Vector3, bool, bool)? r = HandleRightBinding(binding, vr);
                    if (r != null)
                        right_m = r.Value;
                }
            }
            Vector3 move = new Vector3();
            bool isJumping = false;
            if (right_m != null)
            {
                isJumping = right_m.Value.Item2 && (!avatar?.IsCrouched ?? true) && (!avatar?.IsCrawling ?? true);
                if (isJumping && groundedTimer > 0)
                {
                    groundedTimer = 0;
                    verticalVelocity += Mathf.Sqrt(JumpHeight * 2 * -Gravity);
                }
            }
            if (left_m != null && !LockMovement)
            {
                if(left_m.Value.Item3)
                {
                    move = left_m.Value.Item1;
                }
            }
            if (!LockMovement)
            {
                move.y = verticalVelocity;
                CharacterController.Move(move * Time.deltaTime);
                avatar?.SetMove(left_m?.Item4 ?? Vector2.zero, isRunning);
                avatar?.SetIsGrounded(groundedPlayer);
                avatar?.SetRun(isRunning);
                avatar?.Jump(isJumping && !groundedPlayer);
            }
            else
            {
                avatar?.SetMove(Vector2.zero, false);
                avatar?.SetIsGrounded(true);
                avatar?.SetRun(false);
                avatar?.Jump(false);
            }
            bool isMoving = left_m?.Item3 ?? false;
            if (!vr) DesktopFingerCurler.Update(ref LeftDesktopCurler, ref RightDesktopCurler, GestureIdentifier);
            avatar?.Update(areTwoTriggersClicked(), Camera.transform, LeftHandReference, RightHandReference,
                isMoving, this);
            if(transform.position.y < LowestPoint.y - Mathf.Abs(LowestPointRespawnThreshold))
                Respawn();
            isRunning = false;
        }

        private void LateUpdate()
        {
            avatar?.LateUpdate(IsVR, Camera.transform, LockCamera, !avatar?.IsCrawling ?? true);
        }
        
        private void OnDestroy() => Dispose();

        public void Dispose()
        {
            avatar?.Dispose();
            foreach (IBinding binding in Bindings)
            {
                if(binding.GetType() == typeof(Bindings.Keyboard))
                    ((Bindings.Keyboard)binding).Dispose();
                if(binding.GetType() == typeof(Bindings.Mouse))
                    ((Bindings.Mouse)binding).Dispose();
            }
            LocalPlayerSyncController?.Dispose();
        }
    }
}