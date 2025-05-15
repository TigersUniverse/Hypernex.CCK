using UnityEngine;

namespace Hypernex.Tools
{
    public static class CursorTools
    {
        private static Texture2D newMouse;
        private static Texture2D newCircle;
        
        public static void ToggleMouseVisibility(bool value) => Cursor.visible = value;

        public static void ToggleMouseLock(bool value)
        {
            if ((Cursor.lockState == CursorLockMode.None) != value)
                Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}