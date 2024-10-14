using System;
using System.Collections.Generic;
using System.IO;
using Tomlet;
using Tomlet.Attributes;
using Tomlet.Models;

namespace Hypernex.CCK.Editor
{
    public class EditorConfig
    {
        public static EditorConfig LoadedConfig { get; set; }
        
        public static readonly List<Type> AdditionalAllowedAvatarTypes = new List<Type>();
        public static readonly List<Type> AdditionalAllowedWorldTypes = new List<Type>();
        
        [TomlProperty("ScriptEditorLocation")]
        [TomlPrecedingComment("Location of the Script Editor Application")]
        public string ScriptEditorLocation { get; set; } = String.Empty;
        
        [TomlProperty("UseHTTP")]
        [TomlPrecedingComment("Used to Debug local servers. Please do not edit manually!")]
        public bool UseHTTP { get; set; }
        
        [TomlProperty("OverrideSecurity")]
        [TomlPrecedingComment("Bypass any Component Security preventing you from building")]
        public bool OverrideSecurity { get; set; }
        
        [TomlProperty("TargetDomain")]
        [TomlPrecedingComment("Domain to Authenticate to")]
        public string TargetDomain { get; set; } = String.Empty;
        
        [TomlProperty("SavedUserId")]
        [TomlPrecedingComment("UserId to authenticate")]
        public string SavedUserId { get; set; } = String.Empty;
        
        [TomlProperty("SavedToken")]
        [TomlPrecedingComment("Token to use to authenticate the linked UserId. DO NOT SHARE UNDER ANY CIRCUMSTANCES. " +
                              "NO ONE NEEDS THIS, NOT EVEN DEVELOPERS.")]
        public string SavedToken { get; set; } = String.Empty;

        public static EditorConfig GetConfig()
        {
            if (LoadedConfig == null)
                LoadConfig(GetEditorConfigLocation());
            return LoadedConfig;
        }

        internal static string GetEditorConfigLocation()
        {
            string ad = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string t = Path.Combine(ad, "Hypernex.CCK");
            if (!Directory.Exists(t))
                Directory.CreateDirectory(t);
            return Path.Combine(t, "editorconfig.cfg");
        }
        
        public static bool LoadConfig(string fileLocation)
        {
            if (File.Exists(fileLocation))
            {
                string fileData = File.ReadAllText(fileLocation);
                if (!string.IsNullOrEmpty(fileLocation))
                    LoadedConfig = TomletMain.To<EditorConfig>(fileData);
                return true;
            }
            SaveConfig(fileLocation);
            return false;
        }

        public static void SaveConfig(string fileLocation)
        {
            if (LoadedConfig == null)
                LoadedConfig = new EditorConfig();
            TomlDocument tomlDocument = TomletMain.DocumentFrom(typeof(EditorConfig), LoadedConfig);
            string s = tomlDocument.SerializedValue;
            File.WriteAllText(fileLocation, s);
        }
    }
}