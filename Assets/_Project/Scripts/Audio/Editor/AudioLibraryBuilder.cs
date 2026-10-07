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
    /// Builds <c>Assets/_Project/Data/Audio/AudioLibrary.asset</c> from tools/audio/sfx_manifest.json plus the music
    /// box's friend jingles (tools/music/jingles.json: 3D cues named after them, e.g. bell_jingle_short). Every file is
    /// checked against its manifest's sha256 first, so a library can never point at stale renders.
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

            MusicJinglesManifest jingles = ReadJingles();
            var cues = new AudioCue[manifest.cues.Length + jingles.jingles.Length];
            for (int i = 0; i < manifest.cues.Length; i++)
            {
                cues[i] = ToCue(manifest.cues[i]);
            }

            for (int i = 0; i < jingles.jingles.Length; i++)
            {
                cues[manifest.cues.Length + i] = ToJingleCue(jingles.jingles[i]);
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

        private static MusicJinglesManifest ReadJingles()
        {
            string file = AudioAssetPaths.ProjectFile(AudioAssetPaths.MusicJingles);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(
                    $"{BuilderPath}: {AudioAssetPaths.MusicJingles} is missing; render the jingles with tools/music.",
                    file);
            }

            var jingles = JsonUtility.FromJson<MusicJinglesManifest>(File.ReadAllText(file));
            if (jingles == null || jingles.jingles == null || jingles.version != MusicJinglesManifest.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.MusicJingles} " +
                                                    $"(expected version {MusicJinglesManifest.SupportedVersion}).");
            }

            return jingles;
        }

        /// <summary>A music-box jingle as a 3D voice cue of its friend (Sfx bus, no variance: it is a
        /// melody).</summary>
        private static AudioCue ToJingleCue(MusicJinglesManifest.Jingle jingle)
        {
            AudioClip clip = LoadVerified(jingle.file, jingle.sha256);
            return new AudioCue(jingle.id, new[] { clip }, new[] { jingle.id }, AudioBus.Sfx, true, false, 1f, 1f, 1f,
                1f);
        }

        private static AudioCue ToCue(SfxManifest.Cue entry)
        {
            if (!Enum.TryParse(entry.bus, out AudioBus bus))
            {
                throw new InvalidOperationException($"{BuilderPath}: cue '{entry.id}' has unknown bus '{entry.bus}'.");
            }

            if (entry.files == null || entry.sha256 == null || entry.variantLabels == null ||
                entry.files.Length != entry.sha256.Length || entry.files.Length != entry.variantLabels.Length)
            {
                throw new InvalidOperationException(
                    $"{BuilderPath}: cue '{entry.id}' has mismatched files/hashes/variant labels.");
            }

            var clips = new AudioClip[entry.files.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = LoadVerified(entry.files[i], entry.sha256[i]);
            }

            return new AudioCue(entry.id, clips, entry.variantLabels, bus, entry.spatial, entry.loop,
                entry.volumeMin, entry.volumeMax, entry.pitchMin, entry.pitchMax);
        }

        /// <summary>Loads the clip at <paramref name="path"/> after checking its file against the manifest's
        /// hash.</summary>
        private static AudioClip LoadVerified(string path, string expectedSha256)
        {
            string actual = Sha256Of(AudioAssetPaths.ProjectFile(path));
            if (!string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{BuilderPath}: {path} does not match its manifest (stale " +
                                                    "render); re-run tools/audio/build_sfx.py or tools/music.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {path} is not an imported AudioClip.");
            }

            return clip;
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
