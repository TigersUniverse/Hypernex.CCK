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
using Hypernex.CCK.Unity.Scripting;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public class WorldBuilder : BuilderRenderer<World, WorldMeta>
    {
        private static WorldMeta Empty(UserAuth auth) => new WorldMeta(String.Empty, auth.Id,
            WorldPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);

        public override bool ValidMeta { get; protected set; }
        protected override bool InAssets => false;

        private static readonly Dictionary<string, (string, Func<string, string>)> extraTags =
            new Dictionary<string, (string, Func<string, string>)>()
            {
                ["club"] = ("Club", s => $"{s}s that are for partying and clubbing"),
                ["game"] = ("Game", s => $"{s}s with games"),
                ["funny"] = ("Funny", s => $"{s}s that are made for comedic purposes"),
                ["chill"] = ("Relaxing", s => $"{s}s that are made for relaxing"),
                ["media"] = ("Media", s => $"{s}s made for watching or listening to media"),
                ["story"] = ("Story", s => $"{s}s with stories and experiences")
            };
        protected override Dictionary<string, (string, Func<string, string>)> ExtraTags => extraTags;

        public (World, AssetIdentifier)? CurrentWorld;
        private AvatarBuilder avatarBuilder;
        private Dictionary<AssetIdentifier, WorldMeta> worldMetaCache = new Dictionary<AssetIdentifier, WorldMeta>();

        private bool initializedMeta;
        private List<string> metaTags = new List<string>();
        private ReorderableList tags;

        private bool replaceTexture;
        private Texture2D worldThumbnailTexture;
        private Camera camera;

        // Texture, API URL, Changed
        private List<(Texture2D, string, bool)> worldIcons = new List<(Texture2D, string, bool)>();
        private ReorderableList worldIconsList;
        
        private WorldServerScripts serverScripts;
        
        public WorldBuilder(UserAuth auth, AvatarBuilder avatarBuilder) : base(auth, "World")
        {
            this.avatarBuilder = avatarBuilder;
            Reload();
            EditorUtils.SimpleDrawList(ref metaTags, ref tags, "User Assigned Tags", (rect, i, _, _) =>
            {
                metaTags[i] = GUI.TextField(rect, metaTags[i]);
            }, _ => EditorGUIUtility.singleLineHeight);
            worldIconsList = new ReorderableList(worldIcons, typeof(Texture2D), true, true, true, true);
            worldIconsList.drawElementCallback += (r, index, active, focused) =>
            {
                Rect textureR = new Rect(r);
                float initWidth = r.width;
                textureR.width = r.height * 16f/9f;
                EditorGUI.DrawPreviewTexture(textureR, worldIcons[index].Item1);
                r.width -= textureR.height - 5;
                r.x += textureR.width + 5;
                bool isHovered = r.Contains(Event.current.mousePosition);
                if (GUI.Button(r, isHovered ? "Select Image (16:9)" : worldIcons[index].Item1.name, new GUIStyle(EditorStyles.linkLabel)))
                {
                    Texture2D selected = EditorUtils.SelectImage();
                    if (selected != null)
                        worldIcons[index] = (selected, worldIcons[index].Item2, true);
                }
                r.width = r.height;
                r.x = initWidth - r.width;
                if(!string.IsNullOrEmpty(worldIcons[index].Item2))
                    GUI.DrawTexture(r, EditorUtils.GetResource<Texture2D>(ButtonIcon.Cloud.ButtonIconToString()));
            };
            worldIconsList.onAddCallback += _ =>
            {
                Texture2D texture2D = new Texture2D(5, 5);
                texture2D.name = "No Image Selected";
                worldIcons.Add((texture2D, String.Empty, false));
            };
            worldIconsList.drawHeaderCallback += rect => EditorUtils.DrawReorderableListHeader(rect, "World Pictures", 5);
            GetServer();
            if (EditorPrefs.HasKey("WorldServerScript"))
                serverScripts =
                    AssetDatabase.LoadAssetAtPath<WorldServerScripts>(EditorPrefs.GetString("WorldServerScript"));
        }

        private void Reload()
        {
            World w = Object.FindFirstObjectByType<World>(FindObjectsInactive.Include);
            if (w != null)
            {
                AssetIdentifier assetIdentifier = w.gameObject.GetComponent<AssetIdentifier>();
                if (assetIdentifier != null)
                    CurrentWorld = (w, assetIdentifier);
                else
                    CurrentWorld = null;
            }
            else
                CurrentWorld = null;
        }

        private async Task<(FileData, (FileData, string)[], FileData[])> UploadExtraAssets(CDNServer cdnServer, TempDir tempDir)
        {
            FileStream thumbnail = null;
            if (replaceTexture)
            {
                byte[] data = worldThumbnailTexture.EncodeToJPG();
                FileStream fs = tempDir.CreateFile("image.jpg", data);
                thumbnail = fs;
            }
            List<(FileStream, string)> icons = new List<(FileStream, string)>();
            foreach ((Texture2D, string, bool) tuple in worldIcons)
            {
                if (!string.IsNullOrEmpty(tuple.Item2))
                {
                    icons.Add((null, tuple.Item2));
                    continue;
                }
                if(!tuple.Item3) continue;
                byte[] data = tuple.Item1.EncodeToJPG();
                FileStream fs = tempDir.CreateFile(Path.GetFileNameWithoutExtension(tuple.Item1.name) + ".jpg", data);
                icons.Add((fs, String.Empty));
            }
            List<FileStream> ss = new List<FileStream>();
            if (serverScripts != null)
            {
                foreach (ModuleScript moduleScript in serverScripts.ServerScripts)
                {
                    FileStream fs = new FileStream(AssetDatabase.GetAssetPath(moduleScript), FileMode.Open, FileAccess.ReadWrite,
                        FileShare.ReadWrite | FileShare.Delete);
                    ss.Add(fs);
                }
            }
            (FileData, (FileData, string)[], FileData[]) ret = new ();
            ret.Item1 = thumbnail != null ? (await UploadAssets(cdnServer, thumbnail))[0] : null;
            ret.Item2 = new (FileData, string)[icons.Count];
            for (int i = 0; i < icons.Count; i++)
            {
                (FileStream, string) tuple = icons[i];
                if (!string.IsNullOrEmpty(tuple.Item2))
                {
                    ret.Item2[i] = (null, tuple.Item2);
                    continue;
                }
                FileData fileData = (await UploadAssets(cdnServer, icons[i].Item1))[0];
                ret.Item2[i] = (fileData, String.Empty);
            }
            ret.Item3 = await UploadAssets(cdnServer, ss.ToArray());
            return ret;
        }

        private async Task<WorldMeta> UploadExtraAssets(CDNServer cdnServer, TempDir tempDir, WorldMeta meta)
        {
            (FileData, (FileData, string)[], FileData[]) uploadedExtras = await UploadExtraAssets(cdnServer, tempDir);
            if(uploadedExtras.Item1 != null)
                meta.ThumbnailURL = $"{auth.APIURL}file/{uploadedExtras.Item1.UserId}/{uploadedExtras.Item1.FileId}";
            if (uploadedExtras.Item2.Length > 0)
            {
                meta.IconURLs.Clear();
                foreach ((FileData, string) tuple in uploadedExtras.Item2)
                {
                    if (!string.IsNullOrEmpty(tuple.Item2))
                    {
                        meta.IconURLs.Add(tuple.Item2);
                        continue;
                    }
                    FileData fileData = tuple.Item1;
                    meta.IconURLs.Add($"{auth.APIURL}file/{fileData.UserId}/{fileData.FileId}");
                }
            }
            if(uploadedExtras.Item3.Length > 0)
            {
                meta.ServerScripts.Clear();
                for (int i = 0; i < uploadedExtras.Item3.Length; i++)
                {
                    FileData fileData = uploadedExtras.Item3[i];
                    if (fileData == null) continue;
                    meta.ServerScripts.Add($"{auth.APIURL}file/{fileData.UserId}/{fileData.FileId}");
                }
            }
            return meta;
        }

        protected override async void Build(World world, WorldMeta meta, AssetIdentifier assetIdentifier, string[] realTags)
        {
            if(UpdateComponents(CheckComponents()) && !AuthConfig.LoadedConfig.OverrideSecurity) return;
            UpdateBuild(true, false);
            using TempDir tempDir = new TempDir(InAssets);
            CDNServer cdnServer = GetServer();
            meta = await UploadExtraAssets(cdnServer, tempDir, meta);
            string path = Builder.BuildAssetBundle(world, assetIdentifier, tempDir);
            if(string.IsNullOrEmpty(path))
            {
                UpdateBuild(false, false);
                return;
            }
            // Upload File
            meta.BuildPlatform = ActivePlatform;
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
            CallbackResult<WorldUpdateResult> updateResult = await auth.UpdateWorld(uploadResult.result, meta);
            if (!updateResult.success)
            {
                UpdateBuild(false, false);
                Logger.CurrentLogger.Error($"Failed to update {assetType.ToLower()}! " + updateResult.message);
                EditorUtils.SimpleDialog(
                    $"Could not update {assetType.ToLower()}! Check logs for more information.");
                return;
            }
            UpdateBuild(false, false);
            Reload();
            CurrentWorld.Value!.Item2.Id = updateResult.result.WorldId;
            CurrentWorld.Value!.Item2.UserInputId = updateResult.result.WorldId;
            EditorUtility.SetDirty(CurrentWorld.Value!.Item2.gameObject);
            worldMetaCache.Clear();
            EditorUtils.SimpleDialog($"Uploaded {assetType.ToLower()}!");
        }

        protected override async void Update(WorldMeta meta, string[] realTags)
        {
            UpdateBuild(false, false, true);
            using TempDir tempDir = new TempDir(InAssets);
            CDNServer cdnServer = GetServer();
            meta = await UploadExtraAssets(cdnServer, tempDir, meta);
            meta.Tags.Clear();
            meta.Tags.AddRange(realTags);
            meta.BuildPlatform = ActivePlatform;
            CallbackResult<EmptyResult> updateResult = await auth.UpdateWorld(meta);
            if (!updateResult.success)
            {
                UpdateBuild(false, false);
                Logger.CurrentLogger.Error($"Failed to update {assetType.ToLower()}! " + updateResult.message);
                EditorUtils.SimpleDialog(
                    $"Could not update {assetType.ToLower()}! Check logs for more information.");
                return;
            }
            UpdateBuild(false, false);
            worldMetaCache.Clear();
            EditorUtils.SimpleDialog($"Updated {assetType.ToLower()}!");
        }

        private async void RequestWorldMeta(string id)
        {
            CallbackResult<MetaCallback<WorldMeta>> metaCallback = await auth.GetWorldMeta(id);
            AssetIdentifier assetIdentifier = CurrentWorld.Value!.Item2;
            worldMetaCache.Remove(assetIdentifier);
            if (!metaCallback.success)
            {
                Logger.CurrentLogger.Error($"Failed to get {assetType}Meta for Id " + id + "! " + metaCallback.message);
                worldMetaCache.Add(assetIdentifier, Empty(auth));
                return;
            }
            worldMetaCache.Add(assetIdentifier, metaCallback.result.Meta);
        }

        private void GetWorldMeta(AssetIdentifier assetIdentifier)
        {
            if(worldMetaCache.ContainsKey(assetIdentifier)) return;
            if (string.IsNullOrEmpty(assetIdentifier.Id))
            {
                worldMetaCache.Add(assetIdentifier, Empty(auth));
                return;
            }
            worldMetaCache.Add(assetIdentifier, null);
            RequestWorldMeta(assetIdentifier.Id);
        }

        private async void DoTextureLoad(string url)
        {
            using HttpClient client = new HttpClient();
            byte[] data = await client.GetByteArrayAsync(url);
            Texture2D texture2D = new Texture2D(0, 0);
            texture2D.LoadImage(data, false);
            worldThumbnailTexture = texture2D;
        }
        
        private async void DoIconLoad(string url)
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage data = await client.GetAsync(url);
            Texture2D texture2D = new Texture2D(0, 0);
            if(data.Content.Headers.ContentDisposition != null)
                texture2D.name = data.Content.Headers.ContentDisposition.FileName;
            texture2D.LoadImage(await data.Content.ReadAsByteArrayAsync(), false);
            worldIcons.Add((texture2D, url, false));
        }
        
        protected override void RenderBuild(ref World world, ref WorldMeta meta, ref AssetIdentifier assetIdentifier)
        {
            if (EditorUtils.LeftButton("Build", ButtonIcon.Build))
            {
                string[] realTags = meta.Tags.Union(contentTags).ToArray();
                Build(world, meta, assetIdentifier, realTags);
            }
            if (!string.IsNullOrEmpty(assetIdentifier.Id) && EditorUtils.LeftButton("Update Information", ButtonIcon.Update))
            {
                string[] realTags = meta.Tags.Union(contentTags).ToArray();
                Update(meta, realTags);
            }
        }

        protected override void Render(ref World world, ref WorldMeta meta, ref AssetIdentifier assetIdentifier)
        {
            EditorUtils.DrawTitle($"{assetType} Information");
            EditorGUILayout.Space();
            meta.Name = EditorUtils.DrawExtraTextField("Name", meta.Name, "Enter a memorable name for your world!",
                true);
            meta.Description = EditorUtils.DrawProperTextArea("Description", meta.Description,
                "Enter a nice description for your world!");
            meta.Publicity =
                (WorldPublicity) EditorGUILayout.Popup("Publicity", (int) meta.Publicity, PublicityOptions);
            if (!initializedMeta)
            {
                metaTags.Clear();
                metaTags.AddRange(meta.Tags);
                if (!string.IsNullOrEmpty(meta.ThumbnailURL))
                {
                    DoTextureLoad(meta.ThumbnailURL);
                    replaceTexture = false;
                }
                foreach (string metaIconUrL in meta.IconURLs)
                    DoIconLoad(metaIconUrL);
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
            EditorUtils.SimpleThumbnail(ref worldThumbnailTexture, ref camera, ref replaceTexture, assetType);
            EditorUtils.DrawTitle("Pictures");
            GUILayout.Label($"Attach any additional pictures for your {assetType}");
            EditorGUILayout.Space();
            worldIconsList.DoLayoutList();
            EditorGUILayout.Separator();
            EditorUtils.DrawTitle("Scripting");
            EditorGUILayout.Space();
            if (serverScripts != null)
                EditorGUILayout.HelpBox(
                    "Your previous ServerScripts will be overwritten by the ones in your WorldServerScripts object.",
                    MessageType.Info);
            serverScripts = (WorldServerScripts) EditorGUILayout.ObjectField("Server Scripts", serverScripts,
                typeof(WorldServerScripts), false);
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
                if(serverScripts != null)
                    EditorPrefs.SetString("WorldServerScript", AssetDatabase.GetAssetPath(serverScripts));
                else
                    EditorPrefs.DeleteKey("WorldServerScript");
                EditorApplication.EnterPlaymode();
            }
#endif
            EditorGUILayout.Space();
            EditorUtils.Line();
            EditorGUILayout.Space();
            ValidMeta = !string.IsNullOrEmpty(meta.Name);
            InjectBuild(ref world, ref meta, ref assetIdentifier);
        }

        protected override Component[] CheckComponents() =>
            Internals.Security.GetOffendingComponents(SceneManager.GetActiveScene(),
                SecurityTools.AdditionalAllowedWorldTypes.ToArray());
        
        protected override RendererResult RequestBuildGUI()
        {
            if (avatarBuilder.SelectedAvatar != null) return RendererResult.DidNotRender;
            if (CurrentWorld.HasValue && CurrentWorld.Value.Item1 == null) CurrentWorld = null;
            if (CurrentWorld == null)
            {
                GUILayout.Label("Create a World", EditorStyles.boldLabel);
                GUILayout.Label("No world was found in the scene. Create a world object to get started!", EditorStyles.miniLabel);
                if (EditorUtils.RightButton("Create a World Object!", ButtonIcon.Add))
                {
                    GameObject worldObject = new GameObject("World");
                    AssetIdentifier assetIdentifier = worldObject.GetComponent<AssetIdentifier>();
                    if (assetIdentifier == null)
                        assetIdentifier = worldObject.AddComponent<AssetIdentifier>();
                    CurrentWorld = (worldObject.AddComponent<World>(), assetIdentifier);
                }
                EditorGUILayout.Space();
                Reload();
                return RendererResult.RenderedOnTop;
            }
            else
            {
                World world = CurrentWorld.Value.Item1;
                AssetIdentifier assetIdentifier = CurrentWorld.Value.Item2;
                GetWorldMeta(assetIdentifier);
                if (!worldMetaCache.TryGetValue(assetIdentifier, out WorldMeta worldMeta) || worldMeta == null)
                {
                    GUILayout.Label("Loading World Information...", EditorStyles.centeredGreyMiniLabel);
                    return RendererResult.Rendered;
                }
                Render(ref world, ref worldMeta, ref assetIdentifier);
            }
            return RendererResult.Rendered;
        }
    }
}