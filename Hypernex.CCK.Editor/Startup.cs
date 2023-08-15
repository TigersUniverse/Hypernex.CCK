using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Hypernex.CCK.Editor.Editors;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using UnityEditor;
using UnityEngine.Rendering;

namespace Hypernex.CCK.Editor
{
    [InitializeOnLoad]
    internal class Startup
    {
        private static readonly ReadOnlyDictionary<BuildTarget, GraphicsDeviceType[]> GraphicsAPIs =
            new ReadOnlyDictionary<BuildTarget, GraphicsDeviceType[]>(new Dictionary<BuildTarget, GraphicsDeviceType[]>
            {
                [BuildTarget.StandaloneWindows64] = new[]
                {
                    GraphicsDeviceType.Direct3D12,
                    GraphicsDeviceType.Direct3D11,
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.StandaloneWindows] = new[]
                {
                    GraphicsDeviceType.Direct3D12,
                    GraphicsDeviceType.Direct3D11,
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.EmbeddedLinux] = new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.StandaloneLinux64] = new[]
                {
                    GraphicsDeviceType.Vulkan,
                    GraphicsDeviceType.OpenGLCore
                },
                [BuildTarget.Android] = new []
                {
                    GraphicsDeviceType.Vulkan
                }
            });

        private static bool Compare(GraphicsDeviceType[] a, GraphicsDeviceType[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                GraphicsDeviceType ag = a[i];
                GraphicsDeviceType bg = b[i];
                if (ag != bg)
                    return false;
            }
            return true;
        }
        
        static Startup()
        {
            UnityLogger unityLogger = new UnityLogger();
            unityLogger.SetLogger();
            ScriptEditorInstance.Stop();
            EditorConfig c = EditorConfig.GetConfig();
            if (!string.IsNullOrEmpty(c.TargetDomain) && !string.IsNullOrEmpty(c.SavedUserId) &&
                !string.IsNullOrEmpty(c.SavedToken) && AuthManager.Instance == null)
            {
                ContentBuilder.isLoggingIn = true;
                new AuthManager(c.TargetDomain, c.SavedUserId, c.SavedToken, ContentBuilder.HandleLogin);
            }
            Type tmp_text = WhitelistedComponents.GetTMPType(WhitelistedComponents.TMPTypes.TMP_Text);
            if(tmp_text != null)
                WhitelistedComponents.AllowedAvatarTypes.Add(tmp_text);
            foreach (KeyValuePair<BuildTarget,GraphicsDeviceType[]> graphicsAPI in GraphicsAPIs)
            {
                bool compareA = Compare(PlayerSettings.GetGraphicsAPIs(graphicsAPI.Key), graphicsAPI.Value);
                bool compareB = PlayerSettings.GetUseDefaultGraphicsAPIs(graphicsAPI.Key);
                PlayerSettings.SetUseDefaultGraphicsAPIs(graphicsAPI.Key, false);
                PlayerSettings.SetGraphicsAPIs(graphicsAPI.Key, graphicsAPI.Value);
                if(!compareA || compareB)
                {
                    EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                    new Thread(() =>
                    {
                        // TODO: Make this so it knows when to exit?
                        Thread.Sleep(7000);
                        EditorTools.InvokeOnMainThread(new Action(() => EditorApplication.Exit(0)));
                    }).Start();
                }
            }
        }
    }
}