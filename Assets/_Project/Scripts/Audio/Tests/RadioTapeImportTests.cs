using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    /// <summary>
    /// The Tape Deck loops a cassette on repeat, so each imported tape must still be a whole number of bars: any
    /// padding the import added would push the wrap off beat 1 and be heard as a stumble on every lap.
    /// </summary>
    public sealed class RadioTapeImportTests
    {
        private const float BeatsPerBar = 4f;
        private const float SecondsPerMinute = 60f;
        private const double MaxSamplesOffTheBar = 2.0;

        [Test]
        public void EachTape_ImportsAsAWholeNumberOfBars_SoItLoopsOnTheBeat()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "tools", "music", "tapes.json");
            Assert.IsTrue(File.Exists(manifestPath), $"missing {manifestPath}");
            var manifest = JsonUtility.FromJson<TapesManifest>(File.ReadAllText(manifestPath));
            Assert.IsNotNull(manifest.tracks);
            Assert.Greater(manifest.tracks.Length, 0);

            foreach (TapesManifest.Track tape in manifest.tracks)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(tape.file);
                Assert.IsNotNull(clip, tape.file);
                double samplesPerBar = clip.frequency * BeatsPerBar * SecondsPerMinute / tape.bpm;
                double bars = clip.samples / samplesPerBar;
                double off = (bars - Math.Round(bars)) * samplesPerBar;
                Assert.LessOrEqual(Math.Abs(off), MaxSamplesOffTheBar,
                    $"{tape.id}: {clip.samples} samples is {bars:F4} bars ({off:F1} samples off the bar line)");
            }
        }

        /// <summary>JsonUtility mirror of the tapes.json keys this test reads (names = JSON keys).</summary>
        [Serializable]
        internal sealed class TapesManifest
        {
            public Track[] tracks;

            [Serializable]
            public sealed class Track
            {
                public string id;
                public string file;
                public float bpm;
            }
        }
    }
}
