using UnityEditor;
using UnityEngine;
namespace PKBall.Prototype.Editor {
    public sealed class PrototypeSoundImport : AssetPostprocessor {
        void OnPreprocessAudio(){if(!assetPath.StartsWith("Assets/PKBall/Resources/Audio/Sfx/"))return;var importer=(AudioImporter)assetImporter;importer.forceToMono=true;importer.loadInBackground=false;var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;}
    }
}
