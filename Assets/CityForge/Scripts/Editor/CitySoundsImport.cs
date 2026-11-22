using System.IO;
using UnityEditor;
using UnityEngine;

public static class CitySoundsImport
{
    public const string Root = "Assets/CityForge/Resources/Audio/CitySounds";
    [MenuItem("CityForge/Audio/Configure city sound clips")]
    public static void Configure()
    {
        foreach (string path in Directory.GetFiles(Root, "*.mp3"))
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (!importer) continue;
            bool ambience = Path.GetFileNameWithoutExtension(path) is "nature" or "night-time-summer" or "commercial-area" or "stream-longer";
            importer.forceToMono = !ambience;
            importer.loadInBackground = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = ambience ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .65f;
            settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
            settings.sampleRateOverride = ambience ? 44100u : 22050u;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
        Debug.Log("CITY SOUNDS IMPORT READY: MP3 sources, preloaded mono actor effects and compressed stereo ambience.");
    }
}
