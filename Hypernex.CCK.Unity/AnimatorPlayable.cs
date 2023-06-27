using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Hypernex.CCK.Unity
{
    public struct AnimatorPlayable
    {
        public CustomPlayableAnimator CustomPlayableAnimator;
        public PlayableGraph PlayableGraph;
        public AnimatorControllerPlayable AnimatorControllerPlayable;
        public PlayableOutput PlayableOutput;
        public List<AnimatorControllerParameter> AnimatorControllerParameters;
    }
}