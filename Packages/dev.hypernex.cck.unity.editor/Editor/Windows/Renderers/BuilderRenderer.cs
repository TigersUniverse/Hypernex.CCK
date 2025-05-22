using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hypernex.CCK.Auth;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Auth;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public abstract class BuilderRenderer<T1, T2> : IRenderer
    {
        public bool IsBuilding { get; private set; }
        public bool IsUploading { get; private set; }
        public int UploadProgress { get; protected set; }
        public bool IsUpdating { get; protected set; }
        public abstract bool ValidMeta { get; protected set; }
        protected bool AcceptedRights { get; private set; }
        protected bool AcceptedTags { get; private set; }
        protected abstract bool InAssets { get; }
        protected abstract Dictionary<string, (string, Func<string, string>)> ExtraTags { get; }

        public BuildPlatform ActivePlatform => EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
            ? BuildPlatform.Android
            : BuildPlatform.Windows;
        
        private readonly string[] pubOptions = new string[] {"Anyone", "OwnerOnly"};
        protected virtual string[] PublicityOptions => pubOptions;

        protected readonly string[] AvailableBuildPlatforms =
            {"Desktop", "Mobile"};

        private static readonly Dictionary<string, (string, Func<string, string>)> ContentTagsTop =
            new Dictionary<string, (string, Func<string, string>)>
            {
                ["gore"] = ("Gore", s => $"{s}s featuring graphic injuries or significant bloodshed"),
                ["violence"] = ("Violence", s => $"{s}s which display acts of physical aggression or harm"),
                ["nsfw"] = ("NSFW", s => $"{s}s which may contain suggestive content or nudity")
            };
        private static readonly Dictionary<string, (string, Func<string, string>)> ContentTagsBottom =
            new Dictionary<string, (string, Func<string, string>)>
            {
                ["shadereffects"] = ("Shader Effects", s => $"{s}s with Shaders that may cause a player visual discomfort"),
                ["seizure"] = ("Seizure Inducing", s => $"{s}s that have effects which may cause seizures")
            };
        
        protected UserAuth auth;
        protected string assetType;
        protected int selectedBuildPlatform;
        protected List<string> contentTags = new List<string>();

        private Component[] DeniedComponents = Array.Empty<Component>();
        private string[] lastDeniedString = Array.Empty<string>();

        protected BuilderRenderer(UserAuth auth, string assetType)
        {
            this.auth = auth;
            this.assetType = assetType;
            selectedBuildPlatform = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? 1 : 0;
            UserAuth.OnLoginHandled += OnLoginHandled;
        }
        
        ~BuilderRenderer() => UserAuth.OnLoginHandled -= OnLoginHandled;

        private void OnLoginHandled() => auth = UserAuth.Instance;

        protected bool UpdateBuildTarget(BuildPlatform buildPlatform)
        {
            bool accept = EditorUtility.DisplayDialog("Hypernex.CCK.Unity",
                "Switching Build Targets may take a while and consume a lot of resources. Are you sure you would like to switch?",
                "Yes", "No");
            if (!accept) return false;
            AssetDatabase.SaveAssets();
            BuildTargetGroup buildTargetGroup = buildPlatform == BuildPlatform.Windows
                ? BuildTargetGroup.Standalone
                : BuildTargetGroup.Android;
            BuildTarget buildTarget = buildPlatform == BuildPlatform.Windows
                ? BuildTarget.StandaloneWindows64
                : BuildTarget.Android;
            EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
            return true;
        }
        
        protected CDNServer GetServer()
        {
            if (SettingsRenderer.SelectedServer == null)
                SettingsRenderer.GetServer(auth);
            return SettingsRenderer.SelectedServer;
        }
        
        private void CheckButton(string tagName, string displayName, string tooltip)
        {
            if(EditorUtils.LeftToggleButton(new GUIContent(displayName, tooltip), contentTags.Contains(tagName) ? ButtonIcon.Check : ButtonIcon.NoCheck))
            {
                if (contentTags.Contains(tagName))
                {
                    contentTags.Remove(tagName);
                    return;
                }
                contentTags.Add(tagName);
            }
        }

        protected void RenderTags(ref List<string> metaTags)
        {
            EditorUtils.DrawTitle($"{assetType} Tagging");
            EditorGUILayout.Space();
            GUILayout.Label("Content Gated Tags");
            if(!IsBuilding && !IsUploading && !IsUpdating)
            {
                List<string> combined = ContentTagsTop.Keys.Union(ContentTagsBottom.Keys).ToList();
                foreach (string metaTag in metaTags)
                {
                    if (!combined.Contains(metaTag)) continue;
                    contentTags.Add(metaTag);
                }
                metaTags.RemoveAll(x => combined.Contains(x));
            }
            EditorGUILayout.BeginHorizontal();
            foreach (KeyValuePair<string,(string, Func<string,string>)> keyValuePair in ContentTagsTop)
                CheckButton(keyValuePair.Key, keyValuePair.Value.Item1, keyValuePair.Value.Item2.Invoke(assetType));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            foreach (KeyValuePair<string,(string, Func<string,string>)> keyValuePair in ContentTagsBottom)
                CheckButton(keyValuePair.Key, keyValuePair.Value.Item1, keyValuePair.Value.Item2.Invoke(assetType));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        protected void RenderExtraTags(ref List<string> metaTags)
        {
            GUILayout.Label("Official Tags");
            EditorGUILayout.BeginHorizontal();
            foreach (KeyValuePair<string,(string, Func<string,string>)> keyValuePair in ExtraTags)
            {
                if (GUILayout.Button(new GUIContent(keyValuePair.Value.Item1, keyValuePair.Value.Item2.Invoke(assetType))))
                {
                    if (metaTags.Contains(keyValuePair.Key))
                        metaTags.Remove(keyValuePair.Key);
                    else
                        metaTags.Add(keyValuePair.Key);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        internal void UpdateBuild(bool build, bool upload, bool update = false)
        {
            IsBuilding = build;
            IsUploading = upload;
            UploadProgress = 0;
            IsUpdating = update;
        }

        protected void FinishBuild()
        {
            if(BuilderWindow._instance == null) return;
            BuilderWindow._instance.Reset();
        }

        protected async Task<FileData[]> UploadAssets(CDNServer cdnServer, params FileStream[] files)
        {
            FileData[] fileDatas = new FileData[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                FileStream fileStream = files[i];
                CallbackResult<UploadResult> uploadResult = await auth.Upload(fileStream, cdnServer);
                if (!uploadResult.success)
                {
                    Logger.CurrentLogger.Error("Could not upload file " + fileStream.Name + "! " + uploadResult.message);
                    fileDatas[i] = null;
                    continue;
                }
                fileDatas[i] = uploadResult.result.UploadData;
            }
            return fileDatas;
        }

        protected void InjectBuild(ref T1 asset, ref T2 meta, ref AssetIdentifier assetIdentifier)
        {
            AcceptedRights =
                EditorUtils.LargeToggle("By checking, you verify that you own all assets that are uploaded.",
                    AcceptedRights);
            AcceptedTags = EditorUtils.LargeToggle("By checking, you agree that all tags selected are accurate.",
                AcceptedTags);
            EditorGUI.BeginDisabledGroup(!AcceptedRights || !AcceptedTags || !SafeVerifyProject());
            RenderBuild(ref asset, ref meta, ref assetIdentifier);
            EditorGUI.EndDisabledGroup();
        }

        protected abstract void Build(T1 asset, T2 meta, AssetIdentifier assetIdentifier, string[] realTags);
        protected abstract void Update(T2 meta, string[] realTags);
        protected abstract void Render(ref T1 asset, ref T2 meta, ref AssetIdentifier assetIdentifier);
        protected abstract void RenderBuild(ref T1 asset, ref T2 meta, ref AssetIdentifier assetIdentifier);
        protected abstract Component[] CheckComponents();

        private bool SafeVerifyProject()
        {
            if (!VerifyProject()) return false;
            return ValidMeta;
        }
        
        private bool VerifyProject()
        {
            if (!GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipelineAsset"))
            {
                EditorGUILayout.HelpBox("URP is not installed! You must use URP in order to build any asset!",
                    MessageType.Error);
                EditorGUILayout.HelpBox("If you're using Unity Hub, to create a 3D URP template from Projects, " +
                                        "click New Project, Select the correct Editor Version, and from the All templates " +
                                        "tab on the left, select 3D (URP), then re-import all your assets.",
                    MessageType.Info);
                return false;
            }
            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.Instancing)
            {
                EditorGUILayout.HelpBox("Rendering is not Instanced!", MessageType.Error);
                if(GUILayout.Button("Fix!"))
                    PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
                return false;
            }
            if (lastDeniedString.Length > 0)
            {
                bool allow = AuthConfig.LoadedConfig.OverrideSecurity;
                EditorGUILayout.HelpBox(
                    allow
                        ? "Blacklisted components found! You can still upload these, but they may not function in game. It is recommended to remove/replace these components before continuing."
                        : "Blacklisted components found! You can still upload these with OverrideSecurity enabled, but they may not function in game. It is recommended to remove/replace these components before continuing.",
                    allow ? MessageType.Warning : MessageType.Error);
                if (allow) return true;
                if (GUILayout.Button("Check Again"))
                    UpdateComponents(CheckComponents());
                foreach (string componentString in lastDeniedString)
                    EditorGUILayout.HelpBox(componentString, MessageType.Error);
                return false;
            }
            return true;
        }

        protected bool UpdateComponents(Component[] components)
        {
            DeniedComponents = components;
            lastDeniedString = GetComponentsListString();
            return DeniedComponents.Length > 0;
        }
        
        private string[] GetComponentsListString()
        {
            List<string> componentsStrings = new List<string>();
            foreach (Component deniedComponent in DeniedComponents)
            {
                if (deniedComponent == null) continue;
                string n = deniedComponent.GetType().FullName ?? deniedComponent.name;
                GameObject gameObject = deniedComponent.gameObject;
                string componentsString = String.Empty;
                componentsString += gameObject == null ? n : n + " on " + deniedComponent.gameObject.name;
                componentsStrings.Add(componentsString);
            }
            return componentsStrings.ToArray();
        }

        public RendererResult OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                GUILayout.Label("Please exit Play Mode to continue building.", EditorStyles.centeredGreyMiniLabel);
                return RendererResult.Rendered;
            }
            if (!VerifyProject())
                return RendererResult.Rendered;
            if (IsBuilding)
            {
                GUILayout.Label($"Building {assetType}", EditorStyles.centeredGreyMiniLabel);
                return RendererResult.Rendered;
            }
            if (IsUploading)
            {
                GUILayout.Label($"Uploading {assetType}", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false), UploadProgress/100f, $"{UploadProgress}%");
                return RendererResult.Rendered;
            }
            if (IsUpdating)
            {
                GUILayout.Label($"Updating {assetType}", EditorStyles.centeredGreyMiniLabel);
                return RendererResult.Rendered;
            }
            return RequestBuildGUI();
        }

        protected abstract RendererResult RequestBuildGUI();
    }
}