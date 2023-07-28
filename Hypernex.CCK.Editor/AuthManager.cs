using System;
using System.Linq;
using HypernexSharp;
using HypernexSharp.APIObjects;

namespace Hypernex.CCK.Editor
{
    internal class AuthManager
    {
        internal static AuthManager Instance;
        internal static User CurrentUser;
        internal static Token CurrentToken;
        
        private HypernexSettings HypernexSettings { get; }
        internal HypernexObject HypernexObject { get; }

        private static int _isInviteCodeRequired = -1;
        internal static bool IsInviteCodeRequired(string targetDomain)
        {
            switch (_isInviteCodeRequired)
            {
                case -1:
                {
                    new HypernexObject(new HypernexSettings {TargetDomain = targetDomain}).IsInviteCodeRequired(r =>
                    {
                        if (r.success)
                        {
                            bool required = r.result.inviteCodeRequired;
                            _isInviteCodeRequired = required ? 1 : 0;
                        }
                    });
                    return false;
                }
                case 1:
                    return true;
            }
            return false;
        }

        internal AuthManager(string targetDomain, string username, string email, string password, string inviteCode,
            Action onDone = null)
        {
            HypernexSettings = new HypernexSettings(username, email, password, inviteCode)
            {
                TargetDomain = targetDomain
#if DEBUG
                ,IsHTTP = Editors.ContentBuilder.useHTTP
#endif
            };
            HypernexObject = new HypernexObject(HypernexSettings);
            HypernexObject.CreateUser(result =>
            {
                if (result.success)
                {
                    CurrentUser = result.result.UserData;
                    CurrentToken = result.result.UserData.AccountTokens.First();
                    SaveToConfig();
                    onDone?.Invoke();
                }
                else
                    throw new Exception("Failed to create an account!");
            });
            Instance = this;
        }

        internal AuthManager(string targetDomain, string username, string password, string twofa,
            Action<LoginResult, WarnStatus, BanStatus> onDone = null)
        {
            HypernexSettings = new HypernexSettings(username, password, twofa)
            {
                TargetDomain = targetDomain
#if DEBUG
                ,IsHTTP = Editors.ContentBuilder.useHTTP
#endif
            };
            HypernexObject = new HypernexObject(HypernexSettings);
            HypernexObject.Login(result =>
            {
                if (result.success)
                {
                    if (result.result.Result == LoginResult.Correct)
                    {
                        HypernexObject.GetUser(result.result.Token, r =>
                        {
                            if (r.success)
                            {
                                CurrentToken = result.result.Token;
                                CurrentUser = r.result.UserData;
                                SaveToConfig();
                                onDone?.Invoke(LoginResult.Correct, null, null);
                            }
                            else
                                throw new Exception("Failed to get User Data");
                        });
                    }
                    else
                        onDone?.Invoke(result.result.Result, result.result.WarnStatus, result.result.BanStatus);
                }
                else
                    throw new Exception("Failed to Login");
            });
            Instance = this;
        }

        internal AuthManager(string targetDomain, string userId, string tokenContent,
            Action<LoginResult, WarnStatus, BanStatus> onDone = null)
        {
            HypernexSettings = new HypernexSettings(userId, tokenContent)
            {
                TargetDomain = targetDomain
#if DEBUG
                ,IsHTTP = Editors.ContentBuilder.useHTTP || (EditorConfig.LoadedConfig?.UseHTTP ?? false)
#endif
            };
            HypernexObject = new HypernexObject(HypernexSettings);
            HypernexObject.Login(result =>
            {
                if (result.success)
                {
                    if (result.result.Result == LoginResult.Correct)
                    {
                        HypernexObject.GetUser(result.result.Token, r =>
                        {
                            if (r.success)
                            {
                                CurrentToken = result.result.Token;
                                CurrentUser = r.result.UserData;
                                onDone?.Invoke(LoginResult.Correct, null, null);
                            }
                            else
                                throw new Exception("Failed to get User Data");
                        });
                    }
                    else
                        onDone?.Invoke(result.result.Result, result.result.WarnStatus, result.result.BanStatus);
                }
                else
                    throw new Exception("Failed to Login");
            });
            Instance = this;
        }

        internal void Refresh()
        {
            HypernexObject.GetUser(CurrentToken, r =>
            {
                if (r.success)
                    CurrentUser = r.result.UserData;
                else
                    throw new Exception("Failed to get User Data");
            });
        }

        private void SaveToConfig()
        {
            EditorConfig c = EditorConfig.GetConfig();
            c.TargetDomain = HypernexSettings.TargetDomain;
            c.SavedUserId = CurrentUser.Id;
            c.SavedToken = CurrentToken.content;
#if DEBUG
            c.UseHTTP = Editors.ContentBuilder.useHTTP;
#endif
            EditorConfig.SaveConfig(EditorConfig.GetEditorConfigLocation());
        }
    }
}