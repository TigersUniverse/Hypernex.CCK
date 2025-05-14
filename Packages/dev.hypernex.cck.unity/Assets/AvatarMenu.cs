using System;
using UnityEngine;

namespace Hypernex.CCK.Unity.Assets
{
    [Serializable]
    [CreateAssetMenu(fileName = "Avatar Menu", menuName = "Hypernex/Avatars/Menu")]
    public class AvatarMenu : ScriptableObject
    {
        public AvatarParameters Parameters;
        
        public AvatarControl[] Controls = Array.Empty<AvatarControl>();
    }

    [Serializable]
    public class AvatarControl
    {
        public string ControlName;
        public Sprite ControlSprite;
        public ControlType ControlType;
        public string[] DropdownOptions = Array.Empty<string>();
        public int TargetParameterIndex;
        public int TargetParameterIndex2;
        public AvatarMenu SubMenu;
    }

    public enum ControlType
    {
        Toggle,
        Slider,
        Dropdown,
        TwoDimensionalAxis,
        SubMenu
    }
}