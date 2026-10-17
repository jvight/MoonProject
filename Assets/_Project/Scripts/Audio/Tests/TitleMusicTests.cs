using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class TitleMusicTests
    {
        private const float Frame = 1f / 60f;
        private const float FadeIn = 2f;
        private const float FadeOut = 2.5f;
        private const float BeatsPerBar = 4f;
        private const float SecondsPerMinute = 60f;
        private const double MaxSamplesOffTheBar = 2.0;

        [Test]
        public void Fader_IsSilentBeforeTitle_AndFadeOutBeforeTitleChangesNothing()
        {
            var fader = new TitleMusicFader();
            Assert.IsFalse(fader.IsPlaying);
            Assert.AreEqual(0f, fader.Level);

            fader.FadeOut();
            Step(fader, 2f);
            Assert.IsFalse(fader.IsPlaying);
            Assert.AreEqual(0f, fader.Level);
        }

        [Test]
        public void Fader_FadesInOverTwoSeconds_AndHoldsFullLevel()
        {
            var fader = new TitleMusicFader();
            fader.ShowTitle();
            Assert.IsTrue(fader.IsPlaying);
            Assert.AreEqual(0f, fader.Level);

            Step(fader, 1f);
            Assert.AreEqual(0.5f, fader.Level, 0.03f, "halfway through the 2 s fade-in");

            Step(fader, 1.1f);
            Assert.AreEqual(1f, fader.Level, 1e-5f, "full level after 2 s");
            Assert.IsTrue(fader.IsPlaying);
        }

        [Test]
        public void Fader_FadesOutOverTwoPointFiveSeconds_AndStops()
        {
            var fader = new TitleMusicFader();
            fader.ShowTitle();
            Step(fader, 2.2f);
            Assert.AreEqual(1f, fader.Level, 1e-5f);

            fader.FadeOut();
            fader.FadeOut(); // Idempotent when NewGameRequested is followed by SessionStarted.
            Assert.IsTrue(fader.IsFadingOut);

            Step(fader, 1.25f);
            Assert.AreEqual(0.5f, fader.Level, 0.03f, "halfway through the 2.5 s fade-out");
            Assert.IsTrue(fader.IsPlaying);

            Step(fader, 1.35f);
            Assert.AreEqual(0f, fader.Level, 1e-5f);
            Assert.IsFalse(fader.IsPlaying);
            Assert.IsFalse(fader.IsFadingOut);
        }

        [Test]
        public void TitleMusicLibraryAsset_HasAllThreeVariants_DefaultsToVariantA_AndLoopsOnTheBar()
        {
            var library = AssetDatabase.LoadAssetAtPath<TitleMusicLibrary>(
                "Assets/_Project/Data/Audio/TitleMusicLibrary.asset");
            Assert.IsNotNull(library, "TitleMusicLibrary.asset must exist");
            Assert.IsNull(library.FindProblem());
            Assert.AreEqual(3, library.Count);
            Assert.AreEqual(TitleMusicLibrary.DefaultTrackId, library.SelectedId);
            Assert.AreEqual(2f, library.FadeInSeconds, 1e-4f);
            Assert.AreEqual(2.5f, library.FadeOutSeconds, 1e-4f);
            Assert.AreEqual("title_a_turning_moon", library.SelectedTrack.Id);

            string manifestPath = Path.Combine(Application.dataPath, "..", "tools", "music", "title.json");
            Assert.IsTrue(File.Exists(manifestPath), $"missing {manifestPath}");
            var manifest = JsonUtility.FromJson<RadioTapeImportTests.TapesManifest>(File.ReadAllText(manifestPath));
            Assert.AreEqual(3, manifest.tracks.Length);

            for (int i = 0; i < manifest.tracks.Length; i++)
            {
                RadioTapeImportTests.TapesManifest.Track expected = manifest.tracks[i];
                RadioTrack actual = library.GetTrack(i);
                Assert.AreEqual(expected.id, actual.Id);
                Assert.IsNotNull(actual.Clip, expected.id);

                double samplesPerBar = actual.Clip.frequency * BeatsPerBar * SecondsPerMinute / expected.bpm;
                double bars = actual.Clip.samples / samplesPerBar;
                double off = (bars - Math.Round(bars)) * samplesPerBar;
                Assert.LessOrEqual(Math.Abs(off), MaxSamplesOffTheBar,
                    $"{expected.id}: {actual.Clip.samples} samples is {bars:F4} bars ({off:F1} samples off the bar)");
            }
        }

        [Test]
        public void TitleMusicLibrary_VariantSelectionIsDataDriven()
        {
            var clipA = AudioClip.Create("a", 480, 1, 48000, false);
            var clipB = AudioClip.Create("b", 480, 1, 48000, false);
            var clipC = AudioClip.Create("c", 480, 1, 48000, false);
            var library = ScriptableObject.CreateInstance<TitleMusicLibrary>();
            try
            {
                library.Populate(new[]
                {
                    new RadioTrack("title_a_turning_moon", "A", clipA, 66f),
                    new RadioTrack("title_b_earthlight_cradle", "B", clipB, 70f),
                    new RadioTrack("title_c_quiet_horizon", "C", clipC, 60f),
                });
                Assert.AreEqual("title_a_turning_moon", library.SelectedTrack.Id);

                library.SelectVariant("title_b_earthlight_cradle");
                Assert.AreEqual("title_b_earthlight_cradle", library.SelectedTrack.Id);
                Assert.AreSame(clipB, library.SelectedTrack.Clip);

                library.SelectVariant("title_c_quiet_horizon");
                Assert.AreEqual("title_c_quiet_horizon", library.SelectedTrack.Id);
                Assert.AreSame(clipC, library.SelectedTrack.Clip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(library);
                UnityEngine.Object.DestroyImmediate(clipA);
                UnityEngine.Object.DestroyImmediate(clipB);
                UnityEngine.Object.DestroyImmediate(clipC);
            }
        }

        private static void Step(TitleMusicFader fader, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                fader.Step(Frame, FadeIn, FadeOut);
            }
        }
    }
}
