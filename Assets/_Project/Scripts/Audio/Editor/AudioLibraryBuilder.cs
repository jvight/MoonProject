using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/AudioLibrary.asset</c> from tools/audio/sfx_manifest.json. Every WAV is
    /// checked against the manifest's sha256 first, so a library can never point at stale renders.
    /// </summary>
    internal static class AudioLibraryBuilder
    {
        public const string BuilderPath = "Audio/Library";
        public const int BuilderOrder = 410;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string manifestFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.SfxManifest);
            if (!File.Exists(manifestFile))
            {
                throw new FileNotFoundException($"{BuilderPath}: {AudioAssetPaths.SfxManifest} is missing; " +
                                                "run python tools/audio/build_sfx.py.", manifestFile);
            }

            var manifest = JsonUtility.FromJson<SfxManifest>(File.ReadAllText(manifestFile));
            if (manifest == null || manifest.version != SfxManifest.SupportedVersion || manifest.cues == null)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: unsupported manifest (expected version {SfxManifest.SupportedVersion}).");
            }

            var cues = new AudioCue[manifest.cues.Length];
            for (int i = 0; i < cues.Length; i++)
            {
                cues[i] = ToCue(manifest.cues[i]);
            }

            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            library.Populate(cues);
            library = GeneratedAssets.CreateOrReplace(library, AudioAssetPaths.Library);
            string problem = library.FindProblem();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {problem}.");
            }

            Debug.Log($"{BuilderPath}: {cues.Length} cues -> {AudioAssetPaths.Library}");
        }

        private static AudioCue ToCue(SfxManifest.Cue entry)
        {
            if (!Enum.TryParse(entry.bus, out AudioBus bus))
            {
                throw new InvalidOperationException($"{BuilderPath}: cue '{entry.id}' has unknown bus '{entry.bus}'.");
            }

            if (entry.files == null || entry.sha256 == null || entry.files.Length != entry.sha256.Length)
            {
                throw new InvalidOperationException($"{BuilderPath}: cue '{entry.id}' has mismatched files/hashes.");
            }

            var clips = new AudioClip[entry.files.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                string path = entry.files[i];
                string actual = Sha256Of(AudioAssetPaths.ProjectFile(path));
                if (!string.Equals(actual, entry.sha256[i], StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"{BuilderPath}: {path} does not match the manifest " +
                                                        "(stale render); run python tools/audio/build_sfx.py.");
                }

                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clips[i] == null)
                {
                    throw new InvalidOperationException($"{BuilderPath}: {path} is not an imported AudioClip.");
                }
            }

            return new AudioCue(entry.id, clips, bus, entry.spatial, entry.loop, entry.volumeMin, entry.volumeMax,
                entry.pitchMin, entry.pitchMax);
        }

        private static string Sha256Of(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"{BuilderPath}: missing audio file.", file);
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(file))
            {
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
