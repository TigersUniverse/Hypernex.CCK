using System.Collections.Generic;
using UnityEngine;

namespace Hypernex.CCK.Unity
{
    /// <summary>
    /// Describes how to set up the VideoPlayer at setup
    /// </summary>
    public class VideoPlayerDescriptor : MonoBehaviour
    {
        /// <summary>
        /// Whether or not to set the emissive map to the texture of the created Render Texture
        /// </summary>
        public bool IsEmissive = true;

        /// <summary>
        /// Output Renderers for the VideoPlayer
        /// </summary>
        public List<Renderer> VideoOutputs = new List<Renderer>();

        /// <summary>
        /// Output Audio for Audio linked to the VideoPlayer
        /// </summary>
        public AudioSource AudioOutput;

        /// <summary>
        /// The shader property for the emission map
        /// </summary>
        public string ShaderEmissionProperty = "_EmissionMap";
        
        [HideInInspector] public IVideoPlayer CurrentVideoPlayer;
    }
}