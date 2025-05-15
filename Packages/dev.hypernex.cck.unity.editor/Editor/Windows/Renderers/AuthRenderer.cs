using System;
using Hypernex.CCK.Unity.Auth;
using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Windows.Renderers
{
    public class AuthRenderer : IRenderer
    {
        private UserAuth auth;
        private bool fromConfig;

        private string domain;
        private string username;
        private string password;
        private string twofa;

        private async void RenderInput()
        {
            domain = EditorGUILayout.TextField("Domain", domain);
            username = EditorGUILayout.TextField("Username", username);
            password = EditorGUILayout.PasswordField("Password", password);
            if(UserAuth.Instance != null && UserAuth.Instance.Needs2FA)
                twofa = EditorGUILayout.TextField("2FA", twofa);
            if (GUILayout.Button("Login"))
            {
                auth = new UserAuth(domain);
                await auth.Login(username, password, twofa);
            }
        }

        private async void SignOut()
        {
            fromConfig = false;
            auth = null;
            if (UserAuth.Instance != null && UserAuth.Instance.IsAuth)
                await UserAuth.Instance.Logout();
            UserAuth.Instance = null;
            AuthConfig authConfig = AuthConfig.GetConfig();
            authConfig.TargetDomain = String.Empty;
            authConfig.SavedUserId = String.Empty;
            authConfig.SavedToken = String.Empty;
            AuthConfig.SaveConfig();
        }

        private void RenderWaiting()
        {
            try
            {
                GUILayout.Label("Signing in...", EditorStyles.centeredGreyMiniLabel);
                if (GUILayout.Button("Cancel"))
                    SignOut();
            }
            catch (Exception){}
        }

        private void RenderSignOut()
        {
            GUILayout.Label("Welcome, " + UserAuth.Instance.Username, EditorStyles.centeredGreyMiniLabel);
            if(EditorUtils.LeftButton("Sign Out", ButtonIcon.Logout))
                SignOut();
        }
        
        public RendererResult OnGUI()
        {
            if (UserAuth.Instance != null)
            {
                if (UserAuth.Instance.IsAuth)
                {
                    RenderSignOut();
                    return RendererResult.RenderedOnTop;
                }
                if (fromConfig)
                {
                    RenderWaiting();
                    return RendererResult.Rendered;
                }
                RenderInput();
                return RendererResult.Rendered;
            }
            AuthConfig authConfig = AuthConfig.GetConfig();
            if (authConfig.SavedAuth)
            {
                auth = EditorUtils.Auth(authConfig);
                fromConfig = true;
                return RendererResult.Rendered;
            }
            RenderInput();
            return RendererResult.Rendered;
        }
    }
}