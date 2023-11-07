using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using HypernexSharp.API;
using HypernexSharp.API.APIResults;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Avatar = Hypernex.CCK.Unity.Avatar;
using LoginResult = HypernexSharp.APIObjects.LoginResult;

// ReSharper disable ObjectCreationAsStatement

namespace Hypernex.CCK.Editor.Editors
{
    public class ContentBuilder : EditorWindow
    {
        private static readonly string[] AvatarPublicityOptions = {"Anyone", "OwnerOnly"};
        private static readonly string[] WorldPublicityOptions = { "Anyone", "OwnerOnly" };
        
        private static ContentBuilder Window;
        public static bool AllowBuild { get; private set; }

        [MenuItem("Hypernex.CCK/Content Builder")]
        internal static void ShowWindow()
        {
            Window = GetWindow<ContentBuilder>();
            Window.titleContent = new GUIContent("Content Builder");
            EditorConfig c = EditorConfig.GetConfig();
            if (!string.IsNullOrEmpty(c.TargetDomain) && !string.IsNullOrEmpty(c.SavedUserId) &&
                !string.IsNullOrEmpty(c.SavedToken) && AuthManager.Instance == null)
            {
                isLoggingIn = true;
                new AuthManager(c.TargetDomain, c.SavedUserId, c.SavedToken, HandleLogin);
            }
        }

        internal static 
#if !DEBUG
    readonly 
#endif 
            bool useHTTP
#if !DEBUG
        = false;
#else
            ;
#endif
        private static WarnStatus warnStatus;
        private static BanStatus banStatus;
        
        private static bool enteredDomain;
        private static string targetDomain;

        private void RenderDomain()
        {
            if (enteredDomain)
            {
                RenderLogin();
                return;
            }
            EditorGUILayout.HelpBox("Enter the domain to connect, authenticate, and upload to.", MessageType.Info);
#if DEBUG
            useHTTP = EditorGUILayout.Toggle("Use HTTP", useHTTP);
#endif
            targetDomain = GUILayout.TextField(targetDomain);
            if (GUILayout.Button("Next"))
            {
                if (string.IsNullOrEmpty(targetDomain))
                    throw new Exception("Domain cannot be Empty!");
                enteredDomain = true;
            }
        }

        private static bool isSignUp;
        private static string username;
        private static string password;
        private static string email;
        private static bool needsTwoFA;
        private static string twofa;
        private static string inviteCode = String.Empty;

        internal static bool isLoggingIn;
        
        private void RenderLogin()
        {
            if (!needsTwoFA)
            {
                username = EditorGUILayout.TextField("Username", username);
                if (isSignUp)
                    email = EditorGUILayout.TextField("Email", email);
                password = EditorGUILayout.PasswordField("Password", password);
                if (AuthManager.IsInviteCodeRequired(targetDomain) && isSignUp)
                    inviteCode = EditorGUILayout.TextField("Invite Code", inviteCode);
                EditorGUILayout.BeginHorizontal();
                if (!isLoggingIn && GUILayout.Button(isSignUp ? "Signup" : "Login"))
                {
                    isLoggingIn = true;
                    if (isSignUp)
                        new AuthManager(targetDomain, username, email, password, inviteCode,
                            () => isLoggingIn = false);
                    else
                        new AuthManager(targetDomain, username, password, twofa, HandleLogin);
                }
                if (!isLoggingIn && EditorGUILayout.LinkButton(isSignUp
                        ? "I want to Login to an existing account"
                        : "I want to create a new account"))
                    isSignUp = !isSignUp;
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                twofa = EditorGUILayout.TextField("2FA", twofa);
                if (!isLoggingIn && GUILayout.Button("Login"))
                {
                    isLoggingIn = true;
                    needsTwoFA = false;
                    new AuthManager(targetDomain, username, password, twofa, HandleLogin);
                }
            }
        }

        internal static void HandleLogin(LoginResult result, WarnStatus _warnStatus, BanStatus _banStatus)
        {
            isLoggingIn = false;
            switch (result)
            {
                case LoginResult.Missing2FA:
                    needsTwoFA = true;
                    break;
                case LoginResult.Warned:
                    warnStatus = _warnStatus;
                    break;
                case LoginResult.Banned:
                    banStatus = _banStatus;
                    break;
                case LoginResult.Incorrect:
                    Logger.CurrentLogger.Warn("Incorrect Username or Password");
                    break;
            }
            if (result != LoginResult.Correct)
            {
                AuthManager.Instance = null;
                AuthManager.CurrentUser = null;
                AuthManager.CurrentToken = null;
            }
        }
        
        // https://stackoverflow.com/a/250400/12968919
        private static DateTime UnixTimeStampToDateTime( double unixTimeStamp )
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds( unixTimeStamp ).ToLocalTime();
            return dateTime;
        }

        private void RenderBan()
        {
            EditorGUILayout.HelpBox("YOU HAVE BEEN BANNED", MessageType.Error);
            DateTime begin = UnixTimeStampToDateTime(banStatus.BanBegin).ToLocalTime();
            DateTime end = UnixTimeStampToDateTime(banStatus.BanEnd).ToLocalTime();
            GUILayout.Label("You were banned on: " + begin);
            GUILayout.Label("Your ban will end on: " + end);
            EditorGUILayout.HelpBox("You have been banned for the following reason(s)", MessageType.Info);
            GUILayout.Label(banStatus.BanReason);
            GUILayout.Label(banStatus.BanDescription);
        }
        
        private void RenderWarn()
        {
            EditorGUILayout.HelpBox("YOU HAVE BEEN WARNED", MessageType.Warning);
            DateTime begin = UnixTimeStampToDateTime(warnStatus.TimeWarned).ToLocalTime();
            GUILayout.Label("You were warned on: " + begin);
            EditorGUILayout.HelpBox("You have been warned for the following reason(s)", MessageType.Info);
            GUILayout.Label(warnStatus.WarnReason);
            GUILayout.Label(warnStatus.WarnDescription);
            if (GUILayout.Button("I Understand"))
                warnStatus = null;
        }

        internal static bool isBuilding;
        private World World;
        private Avatar SelectedAvatar;
        private AssetIdentifier SelectedAssetIdentifier;
        private List<Avatar> AvatarsInScene;

        private static bool ot = true;

        private static Dictionary<Avatar, bool> AvatarMetaLoaded = new Dictionary<Avatar, bool>();
        private static Dictionary<World, bool> WorldMetaLoaded = new Dictionary<World, bool>();

        private BuildPlatform GetBuildPlatformFromBuildTarget()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    return BuildPlatform.Windows;
                case BuildTarget.Android:
                    return BuildPlatform.Android;
                default:
                    throw new Exception("Invalid Build target " + EditorUserBuildSettings.activeBuildTarget);
            }
        }

        private void RenderBuildTarget()
        {
            EditorGUILayout.Separator();
            GUILayout.Label($"Selected Build Target: {GetBuildPlatformFromBuildTarget()} ({EditorUserBuildSettings.activeBuildTarget})",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch to Standalone"))
            {
                if(SelectedAvatar != null)
                {
                    //EditorUtility.SetDirty(SelectedAvatar.gameObject);
                    //AssetDatabase.SaveAssets();
                    EditorTools.MakeSave(SelectedAvatar.gameObject);
                }
                AssetDatabase.SaveAssets();
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64);
            }
            if (GUILayout.Button("Switch to Android"))
            {
                if(SelectedAvatar != null)
                {
                    //EditorUtility.SetDirty(SelectedAvatar.gameObject);
                    //AssetDatabase.SaveAssets();
                    EditorTools.MakeSave(SelectedAvatar.gameObject);
                }
                AssetDatabase.SaveAssets();
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,
                    BuildTarget.Android);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();
        }

        private Vector2 listScrollPos = new Vector2();
        private void RenderAvatarList()
        {
            GUILayout.BeginScrollView(listScrollPos);
            if (AvatarsInScene.Count <= 0)
                GUILayout.Label("No avatars in scene!", EditorStyles.miniBoldLabel);
            foreach (Avatar avatar in AvatarsInScene)
                if (GUILayout.Button(avatar.gameObject.name, ButtonThemes.FlatButtonStyle))
                    SelectedAvatar = avatar;
            GUILayout.EndScrollView();
        }

        private void OnAvatarUpload(CallbackResult<UploadResult> result)
        {
            isBuilding = false;
            if (result.success)
            {
                if(!string.IsNullOrEmpty(result.result.AvatarId))
                {
                    EditorTools.InvokeOnMainThread((Action) delegate
                    {
                        AssetIdentifier assetIdentifier = SelectedAvatar.gameObject.GetComponent<AssetIdentifier>();
                        assetIdentifier.SetId(result.result.AvatarId);
                        EditorTools.MakeSave(assetIdentifier);
                    });
                }
                EditorTools.InvokeOnMainThread((Action) (() =>
                    EditorUtility.DisplayDialog("Hypernex.CCK", "Avatar Uploaded!", "OK")));
            }
            else
            {
                EditorTools.InvokeOnMainThread((Action)(() =>
                {
                    Logger.CurrentLogger.Warn(result.message);
                    EditorUtility.DisplayDialog("Hypernex.CCK", "Failed to upload avatar!", "OK");
                }));
            }
        }

        private void FinishAvatarBuild(TempDir tempDir)
        {
            string assetPath = EditorTools.BuildAssetBundle(SelectedAvatar, tempDir);
            if (!string.IsNullOrEmpty(assetPath))
            {
                FileStream fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.Read,
                    FileShare.Delete | FileShare.Read);
                if (fileStream.Length > 1048576 * 90)
                {
                    string td = tempDir.CreateChildDirectory("fileParts");
                    new Thread(() =>
                    {
                        AuthManager.Instance.HypernexObject.UploadPart(result =>
                        {
                            EditorTools.InvokeOnMainThread((Action) delegate
                            {
                                OnAvatarUpload(result);
                                fileStream.Dispose();
                                tempDir.Dispose();
                            });
                        }, AuthManager.CurrentUser, AuthManager.CurrentToken, fileStream, td, SelectedAvatar.Meta);
                    }).Start();
                }
                else
                    AuthManager.Instance.HypernexObject.UploadAvatar(result =>
                        {
                            OnAvatarUpload(result);
                            fileStream.Dispose();
                            tempDir.Dispose();
                        }, AuthManager.CurrentUser, AuthManager.CurrentToken, fileStream,
                        SelectedAvatar.Meta);
            }
            else
                isBuilding = false;
        }

        private Vector2 avatarTagsScroll;

        private void RenderAvatar()
        {
            if (!AvatarMetaLoaded.ContainsKey(SelectedAvatar))
            {
                if (string.IsNullOrEmpty(SelectedAssetIdentifier.GetId()))
                {
                    SelectedAvatar.Meta = new AvatarMeta(String.Empty, AuthManager.CurrentUser.Id,
                        AvatarPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);
                    AvatarMetaLoaded.Add(SelectedAvatar, true);
                }
                else
                {
                    AuthManager.Instance.HypernexObject.GetAvatarMeta(result =>
                    {
                        if (result.success)
                        {
                            SelectedAvatar.Meta = result.result.Meta;
                            AvatarMetaLoaded[SelectedAvatar] = true;
                        }
                        else
                        {
                            Logger.CurrentLogger.Warn("Failed to get Avatar Meta for Avatar " + SelectedAvatar.gameObject.name +
                                                            " [" + SelectedAssetIdentifier.GetId() + "]! Assuming Serialized.");
                            if (SelectedAvatar.Meta == null)
                                SelectedAvatar.Meta = new AvatarMeta(String.Empty, AuthManager.CurrentUser.Id,
                                    AvatarPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);
                        }
                        AvatarMetaLoaded[SelectedAvatar] = true;
                    }, SelectedAssetIdentifier.GetId());
                    AvatarMetaLoaded.Add(SelectedAvatar, false);
                }
            }
            else if(!AvatarMetaLoaded[SelectedAvatar])
                GUILayout.Label("Loading Avatar Information...", EditorStyles.centeredGreyMiniLabel);
            else
            {
                SelectedAvatar.Meta.Name = EditorGUILayout.TextField("Avatar Name", SelectedAvatar.Meta.Name);
                GUILayout.Label("Avatar Description");
                SelectedAvatar.Meta.Description = EditorGUILayout.TextArea(SelectedAvatar.Meta.Description);
                SelectedAvatar.Meta.Publicity = (AvatarPublicity) EditorGUILayout.Popup("Avatar Publicity",
                    (int) SelectedAvatar.Meta.Publicity, AvatarPublicityOptions);
                avatarTagsScroll = EditorGUILayout.BeginScrollView(avatarTagsScroll);
                EditorTools.ADrawObjectList(SelectedAvatar.Meta.Tags, "Avatar Tags", ref ot,
                    () => EditorUtility.SetDirty(SelectedAvatar.gameObject));
                EditorGUILayout.EndScrollView();
                SelectedAvatar.ReplaceImage = EditorGUILayout.Toggle("Replace Image", SelectedAvatar.ReplaceImage);
                if(SelectedAvatar.ReplaceImage)
                    SelectedAvatar.Image =
                        (Sprite) EditorGUILayout.ObjectField("Avatar Image", SelectedAvatar.Image, typeof(Sprite), false);
                if(!isBuilding)
                    RenderBuildTarget();
                if (string.IsNullOrEmpty(SelectedAvatar.Meta.Name))
                {
                    EditorGUILayout.HelpBox("Avatar Meta Incomplete! Make sure you fill out all required fields.",
                        MessageType.Warning);
                    AllowBuild = false;
                }
                BuildPlatform buildPlatform = GetBuildPlatformFromBuildTarget();
                EditorGUILayout.BeginHorizontal();
                if (!isBuilding && AllowBuild)
                {
                    if (GUILayout.Button("Build Avatar!"))
                    {
                        isBuilding = true;
                        //EditorUtility.SetDirty(SelectedAvatar.gameObject);
                        //AssetDatabase.SaveAssets();
                        EditorTools.MakeSave(SelectedAvatar.gameObject);
                        if (string.IsNullOrEmpty(SelectedAvatar.Meta.Name))
                        {
                            isBuilding = false;
                            return;
                        }
                        SelectedAvatar.Meta.BuildPlatform = buildPlatform;
                        TempDir tempDir = new TempDir(true);
                        TempDir thumbnailTempDir = new TempDir();
                        if (SelectedAvatar.ReplaceImage && SelectedAvatar.Image != null)
                        {
                            string spriteAssetPath = AssetDatabase.GetAssetPath(SelectedAvatar.Image);
                            /*TextureImporter textureImporter = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
                            if (textureImporter != null)
                            {
                                if (!textureImporter.isReadable)
                                {
                                    Logger.CurrentLogger.Warn("Sprite at " + spriteAssetPath +
                                                                    " is not readable! Making readable and continuing.");
                                    textureImporter.isReadable = true;
                                    AssetDatabase.ImportAsset(spriteAssetPath);
                                    AssetDatabase.Refresh();
                                }
                            }*/
                            (FileStream, Texture2D)? b = Imaging.GetBitmapFromAsset(SelectedAvatar.Image);
                            if (b == null)
                            {
                                Logger.CurrentLogger.Warn("Failed to upload sprite at " + spriteAssetPath);
                                EditorTools.InvokeOnMainThread(new Action(() => FinishAvatarBuild(tempDir)));
                            }
                            else
                            {
                                MemoryStream ms = new MemoryStream(b.Value.Item2.EncodeToPNG());
                                FileStream g = thumbnailTempDir.CreateFile("thumbnail.png", ms.ToArray());
                                AuthManager.Instance.HypernexObject.UploadFile(result =>
                                {
                                    if (result.success)
                                    {
                                        SelectedAvatar.Meta.ImageURL =
                                            $"{AuthManager.Instance.HypernexObject.Settings.APIURL}file/{result.result.UploadData.UserId}/{result.result.UploadData.FileId}";
                                        EditorTools.InvokeOnMainThread(new Action(() => FinishAvatarBuild(tempDir)));
                                    }
                                    else
                                        Logger.CurrentLogger.Warn("Sprite at " + spriteAssetPath + " failed to upload!");
                                    ms.Dispose();
                                    g.Dispose();
                                    b.Value.Item1.Dispose();
                                    thumbnailTempDir.Dispose();
                                }, AuthManager.CurrentUser, AuthManager.CurrentToken, g);
                            }
                        }
                        else
                            EditorTools.InvokeOnMainThread(new Action(() => FinishAvatarBuild(tempDir)));
                    }

                    if (GUILayout.Button("Return"))
                    {
                        SelectedAvatar = null;
                        SelectedAssetIdentifier = null;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (!AllowBuild)
                {
                    if (GUILayout.Button("Return"))
                        SelectedAvatar = null;
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label("You cannot build! Please fix any problems relating to the project or avatar!",
                        EditorStyles.centeredGreyMiniLabel);
                }
                else if (isBuilding)
                {
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Label("Please wait while your Avatar is being Built...",
                        EditorStyles.centeredGreyMiniLabel);
                }
                else
                    EditorGUILayout.EndHorizontal();
            }
        }
        
        private static void UploadAllScripts(List<NexboxScript> serverScripts, Action<List<string>> onDone, TempDir tempDir, List<string> a = null, List<NexboxScript> b = null)
        {
            List<string> scripts;
            List<NexboxScript> ss;
            if (a != null)
                scripts = a;
            else
                scripts = new List<string>();
            if (b != null)
                ss = b;
            else
                ss = new List<NexboxScript>(serverScripts);
            if (ss.Count <= 0)
            {
                onDone.Invoke(scripts);
                return;
            }
            NexboxScript script = ss.ElementAt(0);
            FileStream fs = tempDir.CreateFile(script.Name + script.GetExtensionFromLanguage(),
                TempDir.GetByteFromString(script.Script));
            AuthManager.Instance.HypernexObject.UploadFile(result =>
            {
                fs.Dispose();
                if (result.success)
                {
                    scripts.Add(
                        $"{AuthManager.Instance.HypernexObject.Settings.APIURL}file/{result.result.UploadData.UserId}/{result.result.UploadData.FileId}");
                    ss.RemoveAt(0);
                    if (ss.Count <= 0)
                    {
                        EditorTools.InvokeOnMainThread((Action)(() => onDone.Invoke(scripts)));
                    }
                    else
                        UploadAllScripts(serverScripts, onDone, tempDir, scripts, ss);
                }
                else
                {
                    Logger.CurrentLogger.Warn("Failed to upload script " + script.Name + script.GetExtensionFromLanguage());
                    UploadAllScripts(serverScripts, onDone, tempDir, scripts, ss);
                }
            }, AuthManager.CurrentUser, AuthManager.CurrentToken, fs);
        }

        private void OnWorldUpload(CallbackResult<UploadResult> result, List<NexboxScript> oldServerScripts)
        {
            isBuilding = false;
            if (result.success)
            {
                EditorTools.InvokeOnMainThread((Action)(() =>
                {
                    if(!string.IsNullOrEmpty(result.result.WorldId))
                    {
                        AssetIdentifier assetIdentifier = World.gameObject.GetComponent<AssetIdentifier>();
                        assetIdentifier.SetId(result.result.WorldId);
                        EditorTools.MakeSave(assetIdentifier);
                    }
                    EditorUtility.DisplayDialog("Hypernex.CCK", "World Uploaded!", "OK");
                    World.ServerScripts = oldServerScripts;
                    EditorTools.MakeSave(World);
                }));
            }
            else
            {
                EditorTools.InvokeOnMainThread((Action)(() =>
                {
                    Logger.CurrentLogger.Warn(result.message);
                    EditorUtility.DisplayDialog("Hypernex.CCK", "Failed to upload world!", "OK");
                    World.ServerScripts = oldServerScripts;
                    EditorTools.MakeSave(World);
                }));
            }
        }

        private WorldMeta CloneWorldMeta(WorldMeta f)
        {
            WorldMeta w = new WorldMeta(f.Id, f.OwnerId, f.Publicity, f.Name, f.Description, f.ThumbnailURL)
            {
                BuildPlatform = f.BuildPlatform
            };
            f.Tags.ForEach(x => w.Tags.Add(x));
            f.IconURLs.ForEach(x => w.IconURLs.Add(x));
            f.ServerScripts.ForEach(x => w.ServerScripts.Add(x));
            return w;
        }

        private static WorldMeta wmClone;

        private void FinishWorldBuild(TempDir tempDir)
        {
            UploadAllScripts(World.ServerScripts, scriptsURLs =>
            {
                foreach (string scriptsUrl in scriptsURLs)
                    World.Meta.ServerScripts.Add(scriptsUrl);
                AssetIdentifier assetIdentifier = World.GetComponent<AssetIdentifier>();
                World.Meta.Id = assetIdentifier.GetId();
                wmClone = CloneWorldMeta(World.Meta);
                World.Meta.ServerScripts.Clear();
                (string, List<NexboxScript>) r = EditorTools.BuildAssetBundle(World, tempDir);
                string assetPath = r.Item1;
                if (!string.IsNullOrEmpty(assetPath))
                {
                    FileStream fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.Read,
                        FileShare.Delete | FileShare.Read);
                    if(fileStream.Length > 1048576 * 90)
                    {
                        string td = tempDir.CreateChildDirectory("fileParts");
                        new Thread(() =>
                        {
                            AuthManager.Instance.HypernexObject.UploadPart(result =>
                            {
                                EditorTools.InvokeOnMainThread((Action)delegate
                                {
                                    OnWorldUpload(result, r.Item2);
                                    fileStream.Dispose();
                                    tempDir.Dispose();
                                });
                            }, AuthManager.CurrentUser, AuthManager.CurrentToken, fileStream, td, null, wmClone);
                        }).Start();
                    }
                    else
                        AuthManager.Instance.HypernexObject.UploadWorld(result =>
                            {
                                OnWorldUpload(result, r.Item2);
                                fileStream.Dispose();
                                tempDir.Dispose();
                            }, AuthManager.CurrentUser, AuthManager.CurrentToken, fileStream,
                            wmClone);
                }
                else
                    isBuilding = false;
            }, tempDir);
        }

        private void UploadAllWorldIcons(TempDir t, List<Sprite> icons, Action<List<string>> onFiles, List<string> files = null)
        {
            List<string> result;
            if (files != null)
                result = files;
            else
            {
                result = new List<string>();
                t.CreateChildDirectory("Icons");
            }
            if (icons.Count <= 0)
            {
                onFiles.Invoke(result);
                return;
            }
            Sprite icon = icons.ElementAt(0);
            string spriteAssetPath = AssetDatabase.GetAssetPath(icon);
            /*TextureImporter textureImporter = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
            if (textureImporter != null)
            {
                if (!textureImporter.isReadable)
                {
                    Logger.CurrentLogger.Warn("Sprite at " + spriteAssetPath +
                                                    " is not readable! Making readable and continuing.");
                    textureImporter.isReadable = true;
                    AssetDatabase.ImportAsset(spriteAssetPath);
                    AssetDatabase.Refresh();
                }
            }
            string file = Path.Combine(t.GetPath(), "Icons", icon.texture.name + ".png");*/
            (FileStream, Texture2D)? b = Imaging.GetBitmapFromAsset(icon);
            if (b == null)
            {
                icons.RemoveAt(0);
                if(icons.Count > 0)
                    UploadAllWorldIcons(t, icons, onFiles, result);
                else
                    EditorTools.InvokeOnMainThread((Action)(() => onFiles.Invoke(result)));
            }
            else
            {
                MemoryStream ms = new MemoryStream(b.Value.Item2.EncodeToPNG());
                FileStream g = t.CreateFile(Path.Combine("Icons", "icon" + result.Count + ".png"), ms.ToArray());
                AuthManager.Instance.HypernexObject.UploadFile(r =>
                {
                    if (r.success)
                        result.Add(
                            $"{AuthManager.Instance.HypernexObject.Settings.APIURL}file/{r.result.UploadData.UserId}/{r.result.UploadData.FileId}");
                    icons.RemoveAt(0);
                    ms.Dispose();
                    g.Dispose();
                    b.Value.Item1.Dispose();
                    if(icons.Count > 0)
                        UploadAllWorldIcons(t, icons, onFiles, result);
                    else
                        EditorTools.InvokeOnMainThread((Action)(() => onFiles.Invoke(result)));
                }, AuthManager.CurrentUser, AuthManager.CurrentToken, g);
            }
        }

        private Vector2 worldTagsScroll;
        private Vector2 worldIconsScroll;
        private static bool wiu;
        private static bool weit;

        private void RenderWorld()
        {
            if (!WorldMetaLoaded.ContainsKey(World))
            {
                if (string.IsNullOrEmpty(SelectedAssetIdentifier.GetId()))
                {
                    World.Meta = new WorldMeta(String.Empty, AuthManager.CurrentUser.Id,
                        WorldPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);
                    WorldMetaLoaded.Add(World, true);
                }
                else
                {
                    AuthManager.Instance.HypernexObject.GetWorldMeta(result =>
                    {
                        if (result.success)
                        {
                            World.Meta = result.result.Meta;
                            WorldMetaLoaded.Add(World, true);
                        }
                        else
                        {
                            Logger.CurrentLogger.Warn("Failed to get World Meta for World " + World.gameObject.name +
                                                            " [" + SelectedAssetIdentifier.GetId() + "]! Assuming Serialized.");
                            if (World.Meta == null)
                                World.Meta = new WorldMeta(String.Empty, AuthManager.CurrentUser.Id,
                                    WorldPublicity.OwnerOnly, String.Empty, String.Empty, String.Empty);
                            WorldMetaLoaded.Add(World, true);
                        }
                    }, SelectedAssetIdentifier.GetId());
                }
            }
            else if(!WorldMetaLoaded[World])
                GUILayout.Label("Loading World Information...", EditorStyles.centeredGreyMiniLabel);
            else
            {
                World.Meta.Name = EditorGUILayout.TextField("World Name", World.Meta.Name);
                GUILayout.Label("World Description");
                World.Meta.Description = EditorGUILayout.TextArea(World.Meta.Description);
                World.Meta.Publicity = (WorldPublicity) EditorGUILayout.Popup("World Publicity",
                    (int) World.Meta.Publicity, WorldPublicityOptions);
                worldTagsScroll = EditorGUILayout.BeginScrollView(worldTagsScroll);
                EditorTools.ADrawObjectList(World.Meta.Tags, "World Tags", ref ot,
                    () => EditorUtility.SetDirty(World.gameObject));
                EditorGUILayout.EndScrollView();
                World.ReplaceImage = EditorGUILayout.Toggle("Replace Image", World.ReplaceImage);
                if(World.ReplaceImage)
                    World.Thumbnail =
                        (Sprite) EditorGUILayout.ObjectField("World Image", World.Thumbnail, typeof(Sprite), false);
                worldIconsScroll = EditorGUILayout.BeginScrollView(worldIconsScroll);
                EditorTools.DrawObjectList(ref World.Icons, "Icons", ref wiu,
                    () => EditorUtility.SetDirty(World.gameObject));
                EditorTools.ADrawObjectList(World.Meta.IconURLs, "Existing Icons", ref weit,
                    () => EditorUtility.SetDirty(World.gameObject), OnAdd: i => World.Meta.IconURLs.RemoveAt(i));
                EditorGUILayout.EndScrollView();
                if(!isBuilding)
                    RenderBuildTarget();
                if (string.IsNullOrEmpty(World.Meta.Name))
                {
                    EditorGUILayout.HelpBox("World Meta Incomplete! Make sure you fill out all required fields.",
                        MessageType.Warning);
                    AllowBuild = false;
                }
                BuildPlatform buildPlatform = GetBuildPlatformFromBuildTarget();
                if (!isBuilding && AllowBuild)
                {
                    if (GUILayout.Button("Build world!"))
                    {
                        isBuilding = true;
                        EditorTools.MakeSave();
                        if (string.IsNullOrEmpty(World.Meta.Name))
                        {
                            isBuilding = false;
                            return;
                        }
                        World.Meta.BuildPlatform = buildPlatform;
                        TempDir tempDir = new TempDir();
                        if (World.ReplaceImage && World.Thumbnail != null)
                        {
                            string spriteAssetPath = AssetDatabase.GetAssetPath(World.Thumbnail);
                            /*TextureImporter textureImporter = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
                            if (textureImporter != null)
                            {
                                if (!textureImporter.isReadable)
                                {
                                    Logger.CurrentLogger.Warn("Sprite at " + spriteAssetPath +
                                                                    " is not readable! Making readable and continuing.");
                                    textureImporter.isReadable = true;
                                    AssetDatabase.ImportAsset(spriteAssetPath);
                                    AssetDatabase.Refresh();
                                }
                            }
                            string imagePath = Path.Combine(tempDir.GetPath(), "thumbnail.png");*/
                            (FileStream, Texture2D)? b = Imaging.GetBitmapFromAsset(World.Thumbnail);
                            if (b == null)
                            {
                                Logger.CurrentLogger.Warn("Failed to get sprite at " + spriteAssetPath);
                                EditorTools.InvokeOnMainThread(new Action(() => FinishWorldBuild(tempDir)));
                            }
                            else
                            {
                                MemoryStream gg = new MemoryStream(b.Value.Item2.EncodeToPNG());
                                FileStream g = tempDir.CreateFile("thumbnail.png", gg.ToArray());
                                AuthManager.Instance.HypernexObject.UploadFile(result =>
                                {
                                    if (result.success)
                                    {
                                        World.Meta.ThumbnailURL =
                                            $"{AuthManager.Instance.HypernexObject.Settings.APIURL}file/{result.result.UploadData.UserId}/{result.result.UploadData.FileId}";
                                        EditorTools.InvokeOnMainThread(new Action(() => FinishWorldBuild(tempDir)));
                                    }
                                    else
                                        Logger.CurrentLogger.Warn("Sprite at " + spriteAssetPath + " failed to upload!");
                                    b.Value.Item1.Dispose();
                                    g.Dispose();
                                    gg.Dispose();
                                }, AuthManager.CurrentUser, AuthManager.CurrentToken, g);
                            }
                        }
                        else if (World.Icons.Count > 0)
                            UploadAllWorldIcons(tempDir, World.Icons, fileURLs =>
                            {
                                foreach (string fileUrl in fileURLs)
                                    World.Meta.IconURLs.Add(fileUrl);
                                EditorTools.InvokeOnMainThread(new Action(() => FinishWorldBuild(tempDir)));
                            });
                        else
                            EditorTools.InvokeOnMainThread(new Action(() => FinishWorldBuild(tempDir)));
                    }
                }
                else if (!AllowBuild)
                    GUILayout.Label("You cannot build! Please fix any problems relating to the project or world!",
                        EditorStyles.centeredGreyMiniLabel);
                else if (isBuilding)
                    GUILayout.Label("Please wait while your World is being Built...",
                        EditorStyles.centeredGreyMiniLabel);
            }
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
            return true;
        }
        
        private void Update()
        {
            if(World != null || SelectedAvatar != null)
                return;
            if (World == null && SelectedAvatar == null)
                SelectedAssetIdentifier = null;
            World = FindObjectOfType<World>();
            if (World != null)
                SelectedAssetIdentifier = World.gameObject.GetComponent<AssetIdentifier>();
            AvatarsInScene = FindObjectsOfType<Avatar>().ToList();
        }

        private void OnGUI()
        {
            if (Window != null)
            {
                // must end area after header!
                Imaging.DrawHeader(new Vector2(Window.position.width, Window.position.height));
                if (banStatus != null)
                    RenderBan();
                else if(warnStatus != null)
                    RenderWarn();
                else if (AuthManager.Instance == null || isLoggingIn)
                {
                    if(!isLoggingIn)
                        RenderDomain();
                    else
                    {
                        GUILayout.Label("Logging In...", EditorStyles.centeredGreyMiniLabel);
                        if (GUILayout.Button("Cancel"))
                        {
                            AuthManager.Instance = null;
                            AuthManager.CurrentUser = null;
                            AuthManager.CurrentToken = null;
                            isLoggingIn = false;
                        }
                    }
                }
                else if (!VerifyProject())
                    AllowBuild = false;
                else if (World != null)
                {
                    AllowBuild = true;
                    try
                    {
                        RenderWorld();
                        if(GUILayout.Button("Refresh User"))
                            AuthManager.Instance.Refresh();
                        if (GUILayout.Button($"Sign Out ({AuthManager.CurrentUser.Username})"))
                        {
                            EditorConfig.LoadedConfig.TargetDomain = String.Empty;
                            EditorConfig.LoadedConfig.SavedUserId = String.Empty;
                            EditorConfig.LoadedConfig.SavedToken = String.Empty;
                            EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
                            AuthManager.Instance.HypernexObject.Logout(_ => { }, AuthManager.CurrentUser,
                                AuthManager.CurrentToken);
                            AuthManager.Instance = null;
                            AuthManager.CurrentUser = null;
                            AuthManager.CurrentToken = null;
                            isLoggingIn = false;
                        }
                    }catch(ArgumentException){}
                }
                else if (SelectedAvatar != null)
                {
                    if (SelectedAssetIdentifier == null)
                        SelectedAssetIdentifier = SelectedAvatar.gameObject.GetComponent<AssetIdentifier>();
                    AllowBuild = true;
                    try{RenderAvatar();}catch(ArgumentException){}
                }
                else if (AvatarsInScene != null)
                {
                    AllowBuild = VerifyProject();
                    RenderAvatarList();
                    if(GUILayout.Button("Refresh User"))
                        AuthManager.Instance.Refresh();
                    if (GUILayout.Button($"Sign Out ({AuthManager.CurrentUser.Username})"))
                    {
                        EditorConfig.LoadedConfig.TargetDomain = String.Empty;
                        EditorConfig.LoadedConfig.SavedUserId = String.Empty;
                        EditorConfig.LoadedConfig.SavedToken = String.Empty;
                        EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
                        AuthManager.Instance.HypernexObject.Logout(_ => { }, AuthManager.CurrentUser,
                            AuthManager.CurrentToken);
                        AuthManager.Instance = null;
                        AuthManager.CurrentUser = null;
                        AuthManager.CurrentToken = null;
                        isLoggingIn = false;
                    }
                }
                else
                    AllowBuild = VerifyProject();
                GUILayout.EndArea();
            }
        }
    }
}