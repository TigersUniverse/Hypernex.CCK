using System;
using System.Collections.Generic;
using Hypernex.CCK;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Internals;
using Hypernex.Databasing.Objects;
using Hypernex.Game.Networking;
using Hypernex.Networking.Messages;
using Hypernex.Sandboxing;
using Hypernex.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hypernex.Game.Avatar
{
    public class LocalAvatarCreator : AvatarCreator
    {
        private readonly AllowedAvatarComponent allowedAvatarComponent = new(true, true, true, true, true, true);
        public AvatarConfiguration AvatarConfiguration = new AvatarConfiguration
        {
            Id = String.Empty
        };

        public bool IsCrouched { get; private set; }
        public bool IsCrawling { get; private set; }
        public FingerCalibration fingerCalibration;

        public LocalAvatarCreator(LocalPlayer localPlayer, CCK.Unity.Assets.Avatar a, bool isVR, bool clone)
        {
            if (clone)
                a = Object.Instantiate(a.gameObject).GetComponent<CCK.Unity.Assets.Avatar>();
            else
                a.transform.parent = localPlayer.transform;
            Avatar = a;
            MainAnimator = a.GetComponent<Animator>();
            MainAnimator.updateMode = AnimatorUpdateMode.Normal;
            HeadAlign = new GameObject("headalign_" + Guid.NewGuid());
            HeadAlign.transform.SetParent(a.ViewPosition.transform);
            HeadAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            VoiceAlign = new GameObject("voicealign_" + Guid.NewGuid());
            VoiceAlign.transform.SetParent(a.SpeechPosition.transform);
            VoiceAlign.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            audioSource = VoiceAlign.AddComponent<AudioSource>();
            OnCreate(Avatar, 7, allowedAvatarComponent);
            fingerCalibration = new FingerCalibration(this);
            a.gameObject.name = "avatar";
            a.transform.SetParent(localPlayer.transform, true);
            AlignAvatar(isVR);
            SetupAnimators();
            Calibrated = true;
            GameInstance.OnGameInstanceLoaded += OnGameInstanceLoaded;
            GameInstance.OnGameInstanceDisconnect += OnGameInstanceDisconnect;
            LoadScripts();
        }
        
        private void OnGameInstanceLoaded(GameInstance arg1, World arg2, Scene arg3) => LoadScripts();
        private void OnGameInstanceDisconnect(GameInstance arg1) => LoadScripts();

        private List<(GameObject, NexboxScript)> localAvatarScripts;

        private void LoadScripts()
        {
            DisposeScripts();
            if (localAvatarScripts == null)
            {
                localAvatarScripts = new List<(GameObject, NexboxScript)>();
                foreach (LocalScript ls in Avatar.gameObject.GetComponentsInChildren<LocalScript>())
                    localAvatarScripts.Add((ls.gameObject, ls.Script));
            }
            foreach ((GameObject, NexboxScript) avatarScript in localAvatarScripts)
                localAvatarSandboxes.Add(new Sandbox(avatarScript.Item2, LocalPlayer.Instance.transform, avatarScript.Item1));
        }

        /// <summary>
        /// Sorts Trackers from 0 by how close they are to the Body, LeftFoot, and RightFoot
        /// </summary>
        /// <returns>Sorted Tracker Transforms</returns>
        private Transform[] FindClosestTrackers(Transform body, Transform leftFoot, Transform rightFoot, GameObject[] ts)
        {
            Dictionary<Transform, (float, GameObject)?> distances = new Dictionary<Transform, (float, GameObject)?>
            {
                [body] = null,
                [leftFoot] = null,
                [rightFoot] = null
            };
            foreach (GameObject tracker in ts)
            {
                Vector3 p = tracker.transform.position;
                float bodyDistance = Vector3.Distance(body.position, p);
                float leftFootDistance = Vector3.Distance(leftFoot.position, p);
                float rightFootDistance = Vector3.Distance(rightFoot.position, p);
                if (distances[body] == null || bodyDistance < distances[body].Value.Item1)
                    distances[body] = (bodyDistance, tracker);
                if (distances[leftFoot] == null || leftFootDistance < distances[leftFoot].Value.Item1)
                    distances[leftFoot] = (leftFootDistance, tracker);
                if (distances[rightFoot] == null || rightFootDistance < distances[rightFoot].Value.Item1)
                    distances[rightFoot] = (rightFootDistance, tracker);
            }
            List<Transform> newTs = new();
            if(distances[body] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[body].Value.Item2.transform.GetChild(0));
            if(distances[leftFoot] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[leftFoot].Value.Item2.transform.GetChild(0));
            if(distances[rightFoot] == null)
                newTs.Add(null);
            else
                newTs.Add(distances[rightFoot].Value.Item2.transform.GetChild(0));
            return newTs.ToArray();
        }

        internal void Update(bool areTwoTriggersClicked, Transform cameraTransform, Transform LeftHandReference, 
            Transform RightHandReference, bool isMoving, LocalPlayer localPlayer)
        {
            Update();
            switch (Calibrated)
            {
                case false:
                {
                    Transform t = HeadAlign.transform;
                    if (t == null)
                        break;
                    cameraTransform.position = t.position;
                    cameraTransform.rotation = t.rotation;
                    break;
                }
                case true:
                {
                    Transform t = LocalPlayer.Instance.Camera.transform;
                    cameraTransform.position = t.position;
                    cameraTransform.rotation = t.rotation;
                    break;
                }
            }
            MainAnimator.runtimeAnimatorController = animatorController;
            MainAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            fingerCalibration?.Update(localPlayer, localPlayer.GetLeftHandCurler(), localPlayer.GetRightHandCurler());
        }

        internal void LateUpdate(bool isVR, Transform cameraTransform, bool lockCamera, bool driveCamera)
        {
            LateUpdate();
            if (!isVR && HeadAlign != null && !lockCamera)
            {
                cameraTransform.position = HeadAlign.transform.position;
                if(driveCamera) DriveCamera(cameraTransform);
            }
            if (!isVR)
            {
                new List<PathDescriptor>(LocalPlayer.Instance.SavedTransforms).ForEach(pathDescriptor =>
                {
                    if (pathDescriptor == null)
                        LocalPlayer.Instance.SavedTransforms.Remove(pathDescriptor);
                });
            }
        }

        protected sealed override void SetupAnimators()
        {
            base.SetupAnimators();
            if(string.IsNullOrEmpty(AvatarConfiguration.SelectedWeight)) return;
            if (!AvatarConfiguration.SavedWeights.TryGetValue(AvatarConfiguration.SelectedWeight,
                   out WeightedObjectUpdate[] weights)) return;
            SetParameters(weights);
        }

        internal void SetMove(Vector2 move, bool isRunning)
        {
            if (MainAnimator == null || !MainAnimator.isInitialized)
                return;
            MainAnimator.SetFloat("MoveX", move.x);
            MainAnimator.SetFloat("MoveY", move.y);
            MainAnimator.SetBool("Walking", !isRunning && !IsCrawling && !IsCrouched && (move.x != 0 || move.y != 0));
        }

        internal void SetRun(bool isRunning) => MainAnimator.SetBool("Running", isRunning);

        internal void Jump(bool isJumping) => MainAnimator.SetBool("Jump", isJumping);

        internal void SetCrouch(bool v)
        {
            if (IsCrawling) SetCrawl(false);
            MainAnimator.SetBool("Crouching", v);
            IsCrouched = v;
        }

        internal void SetCrawl(bool v)
        {
            if(IsCrouched) SetCrouch(false);
            MainAnimator.SetBool("Crawling", v);
            IsCrawling = v;
        }

        internal void SetIsGrounded(bool g)
        {
            if (MainAnimator == null || !MainAnimator.isInitialized)
                return;
            // Grounded (2)
            MainAnimator.SetBool("Grounded", g);
            // FreeFall (3)
            MainAnimator.SetBool("FreeFall", !g);
        }

        public override void Dispose()
        {
            LocalPlayerSyncController.CalibrationData = null;
            LocalPlayerSyncController.calibratedFBT = false;
            foreach (string s in new List<string>(Sandboxing.SandboxedTypes.Player.AssignedTags))
            {
                foreach (string morePlayerAssignedTag in new List<string>(LocalPlayer.MorePlayerAssignedTags))
                {
                    if (s == morePlayerAssignedTag)
                        LocalPlayer.MorePlayerAssignedTags.Remove(morePlayerAssignedTag);
                }
            }
            foreach (string s in new List<string>(Sandboxing.SandboxedTypes.Player.ExtraneousKeys))
            {
                foreach (KeyValuePair<string, object> extraneousObject in new Dictionary<string, object>(LocalPlayer
                             .MoreExtraneousObjects))
                    if (s == extraneousObject.Key)
                        LocalPlayer.MoreExtraneousObjects.Remove(extraneousObject.Key);
            }
            GameInstance.OnGameInstanceLoaded -= OnGameInstanceLoaded;
            GameInstance.OnGameInstanceDisconnect -= OnGameInstanceDisconnect;
            base.Dispose();
        }
    }
}