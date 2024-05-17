using System;

namespace Hypernex.CCK.Unity
{
    public interface IVideoPlayer : IDisposable
    {
        /// <summary>
        /// Defines if the Video Player can be used on the current Platform at all
        /// </summary>
        /// <returns>Usable on platform</returns>
        bool CanBeUsed();
        /// <summary>
        /// Defines if the Video Player can be used for the specified source
        /// </summary>
        /// <param name="source">The source URL/Path</param>
        /// <returns>Media usable on Video Player</returns>
        bool CanBeUsed(Uri source);
        
        /// <summary>
        /// If the Media is playing
        /// </summary>
        bool IsPlaying { get; }
        /// <summary>
        /// Controls if the audio is muted
        /// </summary>
        bool Muted { get; set; }
        /// <summary>
        /// Controls of the video is looping
        /// </summary>
        bool Looping { get; set; }
        /// <summary>
        /// Controls the pitch of the audio
        /// </summary>
        float Pitch { get; set; }
        /// <summary>
        /// Controls the volume of the audio
        /// </summary>
        float Volume { get; set; }
        /// <summary>
        /// Controls the position of the media
        /// </summary>
        double Position { get; set; }
        /// <summary>
        /// Gets the length of the media
        /// </summary>
        double Length { get; }
        /// <summary>
        /// Sets the source media of the Video
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// Plays the video
        /// </summary>
        void Play();
        /// <summary>
        /// Pauses the video
        /// </summary>
        void Pause();
        /// <summary>
        /// Stops the video
        /// </summary>
        void Stop();
    }
}