using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Unity.Auth;
using Hypernex.CCK.Unity.Editor.Windows.Renderers;
using Hypernex.CCK.Unity.Internals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hypernex.CCK.Unity.Editor.Windows
{
    using AvatarBuilder = Renderers.AvatarBuilder;

    [InitializeOnLoad]
    public class BuilderWindow : EditorWindow
    {
        internal static BuilderWindow _instance;
        private static string[] TabOptions = {"Authentication", "Building", "Settings"};
        
        [MenuItem("Hypernex.CCK.Unity/Content Builder", false, 0)]
        private static void ShowWindow()
        {
            BuilderWindow window = GetWindow<BuilderWindow>();
            _instance = window;
            AttachUpdateLoop();
        }

        static BuilderWindow()
        {
            SecurityTools.AllowExtraTypes();
            EditorApplication.playModeStateChanged += state =>
            {
                if(_instance == null) return;
                if(state != PlayModeStateChange.ExitingPlayMode) return;
                _instance.Reset();
            };
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) =>
            {
                if (_instance == null) return;
                bool isBuilding = false;
                foreach (IRenderer renderer in builders)
                {
                    if (renderer is WorldBuilder)
                    {
                        WorldBuilder worldBuilder = renderer as WorldBuilder;
                        isBuilding = worldBuilder.IsBuilding || worldBuilder.IsUploading || worldBuilder.IsUpdating;
                        break;
                    }
                    if (renderer is AvatarBuilder)
                    {
                        AvatarBuilder avatarBuilder = renderer as AvatarBuilder;
                        isBuilding = avatarBuilder.IsBuilding || avatarBuilder.IsUploading || avatarBuilder.IsUpdating;
                        break;
                    }
                }
                if(isBuilding) return;
                _instance.Reset();
            };
        }
        
        private static void ForceRepaint()
        {
            if(_instance == null) return;
            _instance.Repaint();
        }

        private static int selectedTab;
        private static readonly IRenderer header = new HeaderRenderer();
        private static readonly List<IRenderer> auths = new List<IRenderer>();
        private static readonly List<IRenderer> builders = new List<IRenderer>();
        private static readonly List<IRenderer> settings = new List<IRenderer>();

        private Vector2 s;

        internal void Reset()
        {
            auths.Clear();
            builders.Clear();
            settings.Clear();
            selectedTab = (UserAuth.Instance != null && UserAuth.Instance.IsAuth) ? 1 : 0;
            auths.Add(new AuthRenderer());
            AvatarBuilder avatarBuilder = new AvatarBuilder(UserAuth.Instance);
            builders.Add(new WorldBuilder(UserAuth.Instance, avatarBuilder));
            builders.Add(avatarBuilder);
            settings.Add(new SettingsRenderer());
        }

        private void OnEnable()
        {
            _instance = this;
            AttachUpdateLoop();
            Reset();
        }

        private void OnGUI()
        {
            Texture2D t = EditorUtils.PullFromCache("buildicon", () =>
            {
                Texture2D t2 = Resources.Load<Texture2D>("build");
                t2 = EditorUtils.Resize(t2, 16, 16);
                return t2;
            });
            _instance.titleContent = new GUIContent("Content Builder", t);
            header.OnGUI();
            selectedTab = GUILayout.Toolbar(selectedTab, TabOptions);
            EditorGUILayout.Space();
            EditorUtils.Line();
            EditorGUILayout.Space();
            List<IRenderer> renderers;
            bool scroll = false;
            switch (selectedTab)
            {
                case 0:
                    renderers = auths;
                    break;
                case 1:
                    if (UserAuth.Instance == null || !UserAuth.Instance.IsAuth)
                    {
                        renderers = auths;
                        selectedTab = 0;
                        break;
                    }
                    renderers = builders;
                    scroll = true;
                    s = EditorGUILayout.BeginScrollView(s);
                    break;
                case 2:
                    renderers = settings;
                    break;
                default:
                    renderers = new();
                    break;
            }
            for (int i = 0; i < renderers.Count; i++)
            {
                RendererResult result = RendererResult.DidNotRender;
                try
                {
                    result = renderers[i].OnGUI();
                } catch(ArgumentException){}
                if(result == RendererResult.Rendered) break;
                if(i == renderers.Count - 1 || result == RendererResult.DidNotRender) continue;
                EditorUtils.Line();
            }
            if(scroll)
                EditorGUILayout.EndScrollView();
        }
        
        [InitializeOnLoadMethod]
        private static void OnProjectReload()
        {
            EditorApplication.delayCall += () =>
            {
                _instance = Resources.FindObjectsOfTypeAll<BuilderWindow>().Length > 0
                    ? GetWindow<BuilderWindow>()
                    : null;
                if (_instance != null)
                    AttachUpdateLoop();
            };
        }

        private static void AttachUpdateLoop()
        {
            EditorApplication.update -= ForceRepaint;
            EditorApplication.update += ForceRepaint;
        }

        private void OnDisable() => EditorApplication.update -= ForceRepaint;
    }
}