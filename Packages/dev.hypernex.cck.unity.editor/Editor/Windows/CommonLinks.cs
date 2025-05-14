using System;
using Hypernex.CCK.Unity.Auth;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Windows
{
    public static class CommonLinks
    {
        private const string DEFAULT_DASHBOARD_URL = "https://play.hypernex.dev";
        private const string DOCS_URL = "https://docs.hypernex.dev";
        private const string COMMUNITY_URL = "https://forum.hypernex.dev";
        
        [MenuItem("Hypernex.CCK.Unity/Dashboard", false, 15)]
        static void OpenDashboard()
        {
            if(UserAuth.Instance == null || string.IsNullOrEmpty(UserAuth.Instance.APIURL))
            {
                Application.OpenURL(DEFAULT_DASHBOARD_URL);
                return;
            }
            Uri uri = new Uri(UserAuth.Instance.APIURL);
            string newUrl = uri.Scheme + "://" + uri.Host;
            Application.OpenURL(newUrl);
        }


        [MenuItem("Hypernex.CCK.Unity/Documentation", false, 16)]
        static void OpenDocs() => Application.OpenURL(DOCS_URL);

        [MenuItem("Hypernex.CCK.Unity/Community", false, 17)]
        static void OpenForum() => Application.OpenURL(COMMUNITY_URL);
    }
}