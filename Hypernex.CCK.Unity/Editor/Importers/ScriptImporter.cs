using System.IO;
using Hypernex.CCK.Unity.Scripting;
using TriInspector;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.Importers
{
    [ScriptedImporter(1, new string[2]{"lua", "js"})]
    [HideMonoScript]
    public class ScriptImporter : ScriptedImporter
    {
        private static Texture2D JavaScriptIcon;
        private static Texture2D LuaIcon;

        private ModuleScript script;
        
        private void OnValidate()
        {
            JavaScriptIcon = Resources.Load<Texture2D>("jsicon");
            LuaIcon = Resources.Load<Texture2D>("luaicon");
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            string path = ctx.assetPath;
            string ext = Path.GetExtension(path);
            bool isJs = ext == ".js";
            if(script == null)
                script = isJs
                    ? ScriptableObject.CreateInstance<JavaScript>()
                    : ScriptableObject.CreateInstance<Lua>();
            script.FileName = Path.GetFileName(path);
            script.Text = File.ReadAllText(path);
            script.hideFlags = HideFlags.None;
            ctx.AddObjectToAsset("script", script, isJs ? JavaScriptIcon : LuaIcon);
            ctx.SetMainObject(script);
        }
    }
}