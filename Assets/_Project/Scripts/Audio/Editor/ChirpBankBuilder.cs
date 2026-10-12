using System;
using System.IO;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Builds <c>Assets/_Project/Data/Audio/ChirpBank.asset</c> (07's chirp syllables per mood and variant) from
    /// tools/audio/chirp_syllables.json, written alongside the chirp clips by tools/audio. A missing or broken file
    /// fails the build: 07's eye and antenna follow these onsets.
    /// </summary>
    internal static class ChirpBankBuilder
    {
        public const string BuilderPath = "Audio/Chirp Bank";
        public const int BuilderOrder = 427;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string dataFile = AudioAssetPaths.ProjectFile(AudioAssetPaths.ChirpData);
            if (!File.Exists(dataFile))
            {
                throw new FileNotFoundException($"{BuilderPath}: {AudioAssetPaths.ChirpData} is missing; run " +
                                                "tools/audio/build_chirp_data.py.", dataFile);
            }

            var data = JsonUtility.FromJson<ChirpSyllablesData>(File.ReadAllText(dataFile));
            if (data == null || data.moods == null || data.version != ChirpSyllablesData.SupportedVersion)
            {
                throw new InvalidOperationException($"{BuilderPath}: unsupported {AudioAssetPaths.ChirpData} " +
                                                    $"(expected version {ChirpSyllablesData.SupportedVersion}).");
            }

            var moods = new ChirpBank.Mood[data.moods.Length];
            for (int m = 0; m < moods.Length; m++)
            {
                ChirpSyllablesData.Mood entry = data.moods[m];
                var variants = new ChirpVariant[entry.variants.Length];
                for (int v = 0; v < variants.Length; v++)
                {
                    ChirpSyllablesData.Variant variant = entry.variants[v];
                    variants[v] = new ChirpVariant(variant.onsets, variant.pitches, variant.lengths);
                }

                moods[m] = new ChirpBank.Mood(entry.cue, variants);
            }

            var bank = ScriptableObject.CreateInstance<ChirpBank>();
            bank.Populate(moods);
            bank = GeneratedAssets.CreateOrReplace(bank, AudioAssetPaths.Chirps);
            string problem = bank.FindProblem();
            if (problem != null)
            {
                throw new InvalidOperationException($"{BuilderPath}: {problem}.");
            }

            Debug.Log($"{BuilderPath}: {moods.Length} mood(s) -> {AudioAssetPaths.Chirps}");
        }
    }
}
