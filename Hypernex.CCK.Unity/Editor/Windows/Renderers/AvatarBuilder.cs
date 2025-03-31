using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Hypernex.CCK.Auth;
using Hypernex.CCK.Unity.Assets;
using Hypernex.CCK.Unity.Auth;
using Hypernex.CCK.Unity.Internals;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Avatar = Hypernex.CCK.Unity.Assets.Avatar;
using Object = UnityEngine.Object;
using Security = Hypernex.CCK.Unity.Internals.Security;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public class AvatarBuilder : BuilderRenderer<Avatar, AvatarMeta>
    {
        private static AvatarMeta Empty(UserAuth auth) => new AvatarMeta(String.Empty, auth.Id,
            AvatarPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);
        private static readonly AllowedAvatarComponent AllowedAvatarComponent =
            new AllowedAvatarComponent(true, true, true, true, true, true);

        public override bool ValidMeta { get; protected set; }
        protected override bool InAssets => true;
        private static readonly Dictionary<string, (string, Func<string, string>)> extraTags =
            new Dictionary<string, (string, Func<string, string>)>()
            {
                ["anime"] = ("Anime", s => $"{s}s that are based on anime characters"),
                ["furry"] = ("Furry", s => $"{s}s that are based on furry characters"),
                ["cartoon"] = ("Cartoon", s => $"{s}s what have a cartoon look"),
                ["realistic"] = ("Realistic", s => $"{s}s that are very realistic looking"),
                ["funny"] = ("Funny", s => $"{s}s that are made for comedic purposes")
            };
        protected override Dictionary<string, (string, Func<string, string>)> ExtraTags => extraTags;
        
        public (Avatar, AssetIdentifier)[] AllAvatars;
        public (Avatar, AssetIdentifier)? SelectedAvatar;

        private Dictionary<AssetIdentifier, AvatarMeta> avatarMetaCache = new Dictionary<AssetIdentifier, AvatarMeta>();

        private bool initializedMeta;
        private List<string> metaTags = new List<string>();
        private ReorderableList tags;

        private bool replaceTexture;
        private Texture2D avatarTexture;
        private Camera camera;
        
        public AvatarBuilder(UserAuth auth) : base(auth, "Avatar")
        {
            Reload();
            EditorUtils.SimpleDrawList(ref metaTags, ref tags, "User Assigned Tags", (rect, i, _, _) =>
            {
                metaTags[i] = GUI.TextField(rect, metaTags[i]);
            }, _ => EditorGUIUtility.singleLineHeight);
            GetServer();
        }

        private void Reload()
        {
            Avatar[] avatars = Object.FindObjectsByType<Avatar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AllAvatars = avatars.Select(x => (x, x.gameObject.GetComponent<AssetIdentifier>())).ToArray();
        }

        private void Return(ref AvatarMeta avatarMeta)
        {
            avatarMeta.Tags.AddRange(contentTags);
            contentTags.Clear();
            initializedMeta = false;
            SelectedAvatar = null;
        }
        
        private void Return()
        {
            initializedMeta = false;
            SelectedAvatar = null;
        }

        private async Task<FileData[]> UploadExtraAssets(CDNServer cdnServer, TempDir tempDir)
        {
            List<FileStream> fileStreams = new List<FileStream>();
            if (replaceTexture)
            {
                byte[] data = avatarTexture.EncodeToJPG();
                FileStream fs = tempDir.CreateFile("image.jpg", data);
                fileStreams.Add(fs);
            }
            return await UploadAssets(cdnServer, fileStreams.ToArray());
        }

        protected override async void Build(Avatar avatar, AvatarMeta meta, AssetIdentifier assetIdentifier, string[] realTags)
        {
            if(UpdateComponents(CheckComponents()) && !AuthConfig.LoadedConfig.OverrideSecurity) return;
            UpdateBuild(true, false);
            using TempDir tempDir = new TempDir(InAssets);
            CDNServer cdnServer = GetServer();
            FileData[] uploadedExtras = await UploadExtraAssets(cdnServer, tempDir);
            if(uploadedExtras.Length >= 1)
                meta.ImageURL = $"{auth.APIURL}file/{uploadedExtras[0].UserId}/{uploadedExtras[0].FileId}";
            string path = Builder.BuildAssetBundle(avatar, assetIdentifier, tempDir);
            if(string.IsNullOrEmpty(path))
            {
                UpdateBuild(false, false);
                return;
            }
            meta.BuildPlatform = ActivePlatform;
            // Upload File
            UpdateBuild(false, true);
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite,
                FileShare.Delete | FileShare.ReadWrite);
            CallbackResult<UploadResult> uploadResult =
                await auth.Upload(fileStream, cdnServer, i => UploadProgress = i);
            if (!uploadResult.success)
            {
                UpdateBuild(false, false);
                Logger.CurrentLogger.Error("Failed to upload file! " + uploadResult.message);
                EditorUtils.SimpleDialog("Could not upload file! Check logs for more information.");
                return;
            }
            meta.Tags.Clear();
            meta.Tags.AddRange(realTags);
            CallbackResult<AvatarUpdateResult> updateResult = await auth.UpdateAvatar(uploadResult.result, meta);
            if (!updateResult.success)
            {
                UpdateBuild(false, false);
                Logger.CurrentLogger.Error($"Failed to update {assetType.ToLower()}! " + updateResult.message);
                EditorUtils.SimpleDialog(
                    $"Could not update {assetType.ToLower()}! Check logs for more information.");
                return;
            }
            UpdateBuild(false, false);
            Return();
            assetIdentifier.Id = updateResult.result.AvatarId;
            assetIdentifier.UserInputId = updateResult.result.AvatarId;
            EditorUtility.SetDirty(assetIdentifier.gameObject);
            avatarMetaCache.Clear();
            EditorUtils.SimpleDialog($"Uploaded {assetType.ToLower()}!");
        }

        protected override async void Update(AvatarMeta meta, string[] realTags)
        {
            UpdateBuild(false, false, true);
            using TempDir tempDir = new TempDir(InAssets);
            CDNServer cdnServer = GetServer();
            FileData[] uploadedExtras = await UploadExtraAssets(cdnServer, tempDir);
            if(uploadedExtras.Length >= 1)
                meta.ImageURL = $"{auth.APIURL}file/{uploadedExtras[0].UserId}/{uploadedExtras[0].FileId}";
            meta.Tags.Clear();
            meta.Tags.AddRange(realTags);
            meta.BuildPlatform = ActivePlatform;
            CallbackResult<EmptyResult> updateResult = await auth.UpdateAvatar(meta);
            if (!updateResult.success)
            {
                UpdateBuild(false, false);
                Logger.CurrentLogger.Error($"Failed to update {assetType.ToLower()}! " + updateResult.message);
                EditorUtils.SimpleDialog(
                    $"Could not update {assetType.ToLower()}! Check logs for more information.");
                return;
            }
            UpdateBuild(false, false);
            Return();
            EditorUtils.SimpleDialog($"Updated {assetType.ToLower()}!");
        }

        private async void RequestAvatarMeta(string id)
        {
            CallbackResult<MetaCallback<AvatarMeta>> metaCallback = await auth.GetAvatarMeta(id);
            AssetIdentifier assetIdentifier = AllAvatars.First(x => x.Item2.Id == id).Item2;
            avatarMetaCache.Remove(assetIdentifier);
            if (!metaCallback.success)
            {
                Logger.CurrentLogger.Error($"Failed to get {assetType}Meta for Id " + id + "! " + metaCallback.message);
                avatarMetaCache.Add(assetIdentifier, Empty(auth));
                return;
            }
            avatarMetaCache.Add(assetIdentifier, metaCallback.result.Meta);
        }

        private void GetAvatarMeta(AssetIdentifier assetIdentifier)
        {
            if(avatarMetaCache.ContainsKey(assetIdentifier)) return;
            if (string.IsNullOrEmpty(assetIdentifier.Id))
            {
                avatarMetaCache.Add(assetIdentifier, Empty(auth));
                return;
            }
            avatarMetaCache.Add(assetIdentifier, null);
            RequestAvatarMeta(assetIdentifier.Id);
        }

        private async void DoTextureLoad(string url)
        {
            using HttpClient client = new HttpClient();
            byte[] data = await client.GetByteArrayAsync(url);
            Texture2D texture2D = new Texture2D(0, 0);
            texture2D.LoadImage(data, false);
            avatarTexture = texture2D;
        }

        protected override void RenderBuild(ref Avatar avatar, ref AvatarMeta meta, ref AssetIdentifier assetIdentifier)
        {
            if (EditorUtils.LeftButton("Build", ButtonIcon.Build))
            {
                string[] realTags = meta.Tags.Union(contentTags).ToArray();
                Build(avatar, meta, assetIdentifier, realTags);
            }
            if (!string.IsNullOrEmpty(assetIdentifier.Id) && EditorUtils.LeftButton("Update Information", ButtonIcon.Update))
            {
                string[] realTags = meta.Tags.Union(contentTags).ToArray();
                Update(meta, realTags);
            }
        }

        protected override void Render(ref Avatar avatar, ref AvatarMeta meta, ref AssetIdentifier assetIdentifier)
        {
            EditorUtils.DrawTitle($"{assetType} Information");
            EditorGUILayout.Space();
            meta.Name = EditorUtils.DrawExtraTextField("Name", meta.Name, "Enter a memorable name for your avatar!",
                true);
            meta.Description = EditorUtils.DrawProperTextArea("Description", meta.Description,
                "Enter a nice description for your avatar!");
            meta.Publicity =
                (AvatarPublicity) EditorGUILayout.Popup("Publicity", (int) meta.Publicity, PublicityOptions);
            if (!initializedMeta)
            {
                metaTags.Clear();
                metaTags.AddRange(meta.Tags);
                if (!string.IsNullOrEmpty(meta.ImageURL))
                {
                    DoTextureLoad(meta.ImageURL);
                    replaceTexture = false;
                }
                initializedMeta = true;
                contentTags.Clear();
            }
            EditorGUILayout.Space();
            RenderTags(ref metaTags);
            tags.DoLayoutList();
            EditorGUILayout.Separator();
            RenderExtraTags(ref metaTags);
            meta.Tags.Clear();
            meta.Tags.AddRange(metaTags);
            EditorGUILayout.Separator();
            EditorUtils.SimpleThumbnail(ref avatarTexture, ref camera, ref replaceTexture, assetType);
            EditorGUILayout.Space();
            EditorUtils.Line();
            EditorGUILayout.Space();
            int sel = selectedBuildPlatform;
            selectedBuildPlatform = EditorUtils.IconPopout("Build Platform", ButtonIcon.Controller,
                selectedBuildPlatform, AvailableBuildPlatforms);
            if (sel != selectedBuildPlatform)
            {
                bool r = UpdateBuildTarget((BuildPlatform) selectedBuildPlatform);
                if (!r)
                    selectedBuildPlatform = sel;
            }
#if HYPERNEX_CCK_EMULATOR
            if (EditorUtils.LeftButton("Test", ButtonIcon.Experiment))
            {
                EditorPrefs.SetString("AvatarName", avatar.gameObject.name);
                EditorApplication.EnterPlaymode();
            }
#endif
            EditorGUILayout.Space();
            EditorUtils.Line();
            EditorGUILayout.Space();
            ValidMeta = !string.IsNullOrEmpty(meta.Name);
            InjectBuild(ref avatar, ref meta, ref assetIdentifier);
            if (EditorUtils.LeftButton("Return", ButtonIcon.LeftArrow))
                Return(ref meta);
        }

        protected override Component[] CheckComponents()
        {
            if (SelectedAvatar == null)
                return Array.Empty<Component>();
            return Security.GetOffendingComponents(SelectedAvatar.Value.Item1, AllowedAvatarComponent,
                SecurityTools.AdditionalAllowedAvatarTypes.ToArray());
        }

        protected override RendererResult RequestBuildGUI()
        {
            if (SelectedAvatar == null)
            {
                GUILayout.Label("Select an Avatar", EditorStyles.boldLabel);
                GUILayout.Label("Choose from your Avatars in the current scene to manage and upload.", EditorStyles.miniLabel);
                EditorGUILayout.Space();
                Reload();
                if (AllAvatars.Length <= 0)
                {
                    GUILayout.Label("No avatars in scene!");
                    return RendererResult.RenderedOnTop;
                }
                foreach ((Avatar, AssetIdentifier) avatar in AllAvatars)
                {
                    if (EditorUtils.RightButton(avatar.Item1.gameObject.name, ButtonIcon.RightArrow))
                        SelectedAvatar = avatar;
                }
            }
            else
            {
                Avatar avatar = SelectedAvatar.Value.Item1;
                AssetIdentifier assetIdentifier = SelectedAvatar.Value.Item2;
                GetAvatarMeta(assetIdentifier);
                if (!avatarMetaCache.TryGetValue(assetIdentifier, out AvatarMeta avatarMeta) || avatarMeta == null)
                {
                    GUILayout.Label("Loading Avatar Information...", EditorStyles.centeredGreyMiniLabel);
                    return RendererResult.Rendered;
                }
                Render(ref avatar, ref avatarMeta, ref assetIdentifier);
            }
            return RendererResult.Rendered;
        }
    }
}