using System;
using Hypernex.CCK.Editor.Editors.Tools;
using Hypernex.CCK.Unity;
using UnityEditor;

namespace Hypernex.CCK.Editor.Editors
{
    [CustomEditor(typeof(LocalScript))]
    public class LocalScriptEditor : UnityEditor.Editor
    {
        private LocalScript LocalScript;

        private void OnEnable() => LocalScript = target as LocalScript;

        public override void OnInspectorGUI()
        {
            if (LocalScript == null)
                return;
            LocalScript.NexboxScript ??= new NexboxScript(NexboxLanguage.Unknown, ""){Name = "MyScript"};
            LocalScript.NexboxScript.Script ??= "MyScript";
            string last = LocalScript.NexboxScript.Name;
            LocalScript.NexboxScript.Name = EditorGUILayout.TextField("Name", LocalScript.NexboxScript.Name);
            if(last != LocalScript.NexboxScript.Name)
                EditorUtility.SetDirty(LocalScript.gameObject);
            Enum val = LocalScript.NexboxScript.Language;
            Enum v = EditorGUILayout.EnumPopup("Language", val);
            if(!Equals(val, v))
                EditorUtility.SetDirty(LocalScript.gameObject);
            LocalScript.NexboxScript.Language = (NexboxLanguage) v;
            EditorTools.DrawScriptEditorOnCustomEvent(LocalScript, ref LocalScript.NexboxScript);
        }
    }
}