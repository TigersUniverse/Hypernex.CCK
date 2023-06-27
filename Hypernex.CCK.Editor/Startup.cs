using System;
using Hypernex.CCK.Editor.Editors;
using Hypernex.CCK.Unity;
using UnityEditor;

namespace Hypernex.CCK.Editor
{
    [InitializeOnLoad]
    internal class Startup
    {
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
        }
    }
}