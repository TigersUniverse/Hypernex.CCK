using System.Reflection;
using UnityEngine;

namespace Hypernex.CCK.Unity.Libs.Editor
{
    public static class ComponentUtilityHelper
    {
        private delegate bool MoveComponentDelegate(Component aTarget, Component aRelative, bool aMoveAbove);
        private static MoveComponentDelegate m_MoveComponent = null;
        static ComponentUtilityHelper()
        {
            var componentUtilityType = typeof(UnityEditorInternal.ComponentUtility);
            var moveComponentMI = componentUtilityType.GetMethod(
                "MoveComponentRelativeToComponent",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] {
                    typeof(Component),
                    typeof(Component),
                    typeof(bool)
                }, null);
            if (moveComponentMI == null)
                throw new System.Exception("Internal method MoveComponentRelativeToComponent was not found");
            m_MoveComponent = (MoveComponentDelegate)System.Delegate.CreateDelegate(typeof(MoveComponentDelegate), moveComponentMI);
        }
        public static bool MoveComponent(this Component aTarget, Component aRelative, bool aMoveAbove)
        {
            if (m_MoveComponent == null)
                throw new System.Exception("Internal method MoveComponentRelativeToComponent was not found");
            return m_MoveComponent(aTarget, aRelative, aMoveAbove);
        }
    }
}