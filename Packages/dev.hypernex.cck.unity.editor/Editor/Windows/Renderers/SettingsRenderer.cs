using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.CCK.Auth;
using Hypernex.CCK.Unity.Auth;
using HypernexSharp.APIObjects;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public class SettingsRenderer : IRenderer
    {
        public static CDNServer SelectedServer;
        public static List<DistanceObject> Servers = new List<DistanceObject>();
        
        public static async void GetServer(UserAuth auth)
        {
            if(auth == null) return;
            List<CDNServer> servers = await auth.GetCDNs();
            Servers.Clear();
            foreach (CDNServer cdnServer in servers)
                Servers.Add(cdnServer);
            DistanceObject.Sort(ref Servers, auth.Latitude, auth.Longitude);
            SelectedServer = (CDNServer) Servers[0];
        }

        private void RenderAdvancedConfig()
        {
            EditorUtils.DrawTitle("Hypernex Advanced Config");
            bool httpBefore = AuthConfig.LoadedConfig.UseHTTP;
            AuthConfig.LoadedConfig.UseHTTP =
                GUILayout.Toggle(AuthConfig.LoadedConfig.UseHTTP, "Allow HTTP Servers");
            if(httpBefore != AuthConfig.LoadedConfig.UseHTTP)
                AuthConfig.SaveConfig();
            if (AuthConfig.LoadedConfig.OverrideSecurity)
                EditorGUILayout.HelpBox(
                    "Component Security prevents you from uploading any invalid components. Any invalid components will be removed in-game.",
                    MessageType.Warning);
            bool securityBefore = AuthConfig.LoadedConfig.OverrideSecurity;
            AuthConfig.LoadedConfig.OverrideSecurity = GUILayout.Toggle(AuthConfig.LoadedConfig.OverrideSecurity,
                "Override Component Security");
            if(securityBefore != AuthConfig.LoadedConfig.OverrideSecurity)
                AuthConfig.SaveConfig();
            EditorGUILayout.Space();
            EditorUtils.DrawTitle("Hypernex Servers");
            if(UserAuth.Instance != null && UserAuth.Instance.IsAuth)
            {
                int index = Servers.IndexOf(SelectedServer);
                if (index < 0)
                {
                    index = 0;
                    GetServer(UserAuth.Instance);
                }
                SelectedServer = (CDNServer) Servers.ElementAt(EditorGUILayout.Popup(new GUIContent("CDN Server"), index,
                    Servers.Select(x => new GUIContent(new Uri(((CDNServer) x).Server).Host)).ToArray()));
            }
            else
                GUILayout.Label("Please Sign In to manage Servers", EditorStyles.miniBoldLabel);
        }
        
        public RendererResult OnGUI()
        {
            AuthConfig.GetConfig();
            RenderAdvancedConfig();
            return RendererResult.Rendered;
        }
    }
}