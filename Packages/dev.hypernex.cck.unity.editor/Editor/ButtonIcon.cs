using System;

namespace Hypernex.CCK.Unity.Editor
{
    public enum ButtonIcon
    {
        LeftArrow,
        RightArrow,
        Build,
        Controller,
        Check,
        NoCheck,
        Logout,
        Update,
        Add,
        Cloud,
        Experiment
    }

    internal static class ButtonIconExtensions
    {
        internal static string ButtonIconToString(this ButtonIcon icon)
        {
            switch (icon)
            {
                case ButtonIcon.LeftArrow:
                    return "left_arrow";
                case ButtonIcon.RightArrow:
                    return "right_arrow";
                case ButtonIcon.Build:
                    return "build";
                case ButtonIcon.Controller:
                    return "controller";
                case ButtonIcon.Check:
                    return "check";
                case ButtonIcon.NoCheck:
                    return "nocheck";
                case ButtonIcon.Logout:
                    return "logout";
                case ButtonIcon.Update:
                    return "update";
                case ButtonIcon.Add:
                    return "add";
                case ButtonIcon.Cloud:
                    return "cloud";
                case ButtonIcon.Experiment:
                    return "experiment";
            }
            throw new Exception("Invalid enum!");
        }
    }
}