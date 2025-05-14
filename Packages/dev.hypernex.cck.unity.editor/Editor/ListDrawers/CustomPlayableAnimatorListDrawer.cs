using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor.ListDrawers
{
    public static class CustomPlayableAnimatorListDrawer
    {
        public static float GetListHeight(int _) => 40f;

        public static void DrawList(SerializedProperty array, Rect rect, int index, bool isactive, bool isfocused)
        {
            SerializedProperty targetCustomAnimator = array.GetArrayElementAtIndex(index);
            SerializedProperty animatorController = targetCustomAnimator.FindPropertyRelative("AnimatorController");
            SerializedProperty animatorOverrideController =
                targetCustomAnimator.FindPropertyRelative("AnimatorOverrideController");
            Rect current = new Rect(rect.x, rect.y, rect.width, rect.height / 2 - 2);
            EditorGUI.PropertyField(current, animatorController, new GUIContent("Animator Controller"));
            current.y += rect.height / 2;
            EditorGUI.PropertyField(current, animatorOverrideController, new GUIContent("Override Controller"));
        }
    }
}