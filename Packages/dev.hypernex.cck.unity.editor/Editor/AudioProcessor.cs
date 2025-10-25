using UnityEditor;
using UnityEngine;

namespace Hypernex.CCK.Unity.Editor
{
    public class AudioProcessor : AssetPostprocessor
    {
        private void OnPostprocessAudio(AudioClip _)
        {
            AudioImporter audioImporter = (AudioImporter) assetImporter;
            audioImporter.defaultSampleSettings = new AudioImporterSampleSettings
            {
                preloadAudioData = true
            };
        }
    }
}