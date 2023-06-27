using UnityEngine;

namespace Hypernex.CCK.Editor.Editors.Tools
{
    public class ButtonThemes
    {
        private static readonly GUIStyle _f = new GUIStyle(GUI.skin.button);
        public static readonly GUIStyle FlatButtonStyle = new GUIStyle(GUI.skin.button)
        {
            active = new GUIStyleState
            {
                background = _f.active.background
            },
            margin = new RectOffset(0, 0, 0, 0)
        };
    }
}