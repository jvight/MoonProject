using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    /// <summary>
    /// 07's machine voice (feel-pass section 7.2): the bank's syllable data, the variant picking and the rules that
    /// keep chirps one at a time.
    /// </summary>
    public sealed class ChirpBankTests
    {
        private const float Frame = 1f / 60f;
        private const float Hold = 3f;
        private const int SchedulerVariants = 4;
        private const string BankAsset = "Assets/_Project/Data/Audio/ChirpBank.asset";

        [Test]
        public void TheBank_HasEightMoods_EachItsPickedGesture_WithOrderedSyllables()
        {
            var bank = AssetDatabase.LoadAssetAtPath<ChirpBank>(BankAsset);
            Assert.IsNotNull(bank, $"{BankAsset} missing: run the Audio builders first");
            Assert.IsNull(bank.FindProblem());
            for (int m = 0; m < ChirpBank.MoodCount; m++)
            {
                var mood = (ChirpMood)m;
                Assert.Greater(bank.VariantCount(mood), 0, mood.ToString());
                Assert.AreEqual("chirp_" + mood.ToString().ToLowerInvariant(), bank.CueId(mood));
                for (int v = 0; v < bank.VariantCount(mood); v++)
                {
                    ChirpVariant variant = bank.GetVariant(mood, v);
                    Assert.Greater(variant.SyllableCount, 0, $"{mood} {v}: a gesture has at least one syllable");
                    Assert.AreEqual(0f, variant.Onset(0), $"{mood} {v} starts at once");
                    Assert.Less(variant.Seconds, 1.2f, $"{mood} {v} stays short");
                }
            }
        }

        [Test]
        public void TheBanksData_IsTheToolsCurrentOutput()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, "tools", "audio",
                "chirp_syllables.json");
            Assert.IsTrue(File.Exists(path), path);
            StringAssert.Contains("\"onsets\"", File.ReadAllText(path));
        }

        [Test]
        public void ABrokenVariant_IsReported()
        {
            var overlapping = new ChirpVariant(new[] { 0f, 0.05f }, new[] { 0.5f, 0.6f }, new[] { 0.1f, 0.1f });
            Assert.IsNotNull(overlapping.FindProblem());
            var outOfRange = new ChirpVariant(new[] { 0f }, new[] { 1.5f }, new[] { 0.1f });
            Assert.IsNotNull(outOfRange.FindProblem());
            var fine = new ChirpVariant(new[] { 0f, 0.13f }, new[] { 0.5f, 0.9f }, new[] { 0.1f, 0.12f });
            Assert.IsNull(fine.FindProblem());
            Assert.AreEqual(0.25f, fine.Seconds, 1e-5f);
        }

        [Test]
        public void Variants_NeverRepeatTwiceInARow_AndAllGetPlayed()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(7u));
            var seen = new bool[SchedulerVariants];
            int last = -1;
            for (int i = 0; i < 200; i++)
            {
                int variant = scheduler.PickVariant(ChirpMood.Happy, SchedulerVariants);
                Assert.AreNotEqual(last, variant, "never the same one twice in a row");
                seen[variant] = true;
                last = variant;
            }

            CollectionAssert.DoesNotContain(seen, false, "every variant is heard");
        }

        [Test]
        public void EachMood_RemembersItsOwnLastVariant()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(3u));
            int happy = scheduler.PickVariant(ChirpMood.Happy, SchedulerVariants);
            scheduler.PickVariant(ChirpMood.Worried, SchedulerVariants);
            Assert.AreNotEqual(happy, scheduler.PickVariant(ChirpMood.Happy, SchedulerVariants));
        }

        [Test]
        public void AChirp_SpeaksAtOnce_WhenNothingElseDoes()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(1u));
            scheduler.Request(ChirpMood.Affirm);
            Assert.IsTrue(scheduler.Step(Frame, false, Hold, out ChirpMood mood));
            Assert.AreEqual(ChirpMood.Affirm, mood);
            Assert.IsFalse(scheduler.Step(Frame, false, Hold, out _), "once");
        }

        [Test]
        public void AChirp_WaitsForTheOneSpeaking_ThenSpeaks()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(1u));
            scheduler.Request(ChirpMood.Curious);
            for (float t = 0f; t < Hold * 0.5f; t += Frame)
            {
                Assert.IsFalse(scheduler.Step(Frame, true, Hold, out _), "never over another chirp");
            }

            Assert.IsTrue(scheduler.Step(Frame, false, Hold, out ChirpMood mood));
            Assert.AreEqual(ChirpMood.Curious, mood);
        }

        [Test]
        public void AChirp_IsDropped_WhenTheOneSpeakingOutlastsTheHold()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(1u));
            scheduler.Request(ChirpMood.Worried);
            for (float t = 0f; t < Hold + 0.5f; t += Frame)
            {
                scheduler.Step(Frame, true, Hold, out _);
            }

            Assert.IsFalse(scheduler.IsWaiting);
            Assert.AreEqual(1, scheduler.Dropped);
            Assert.IsFalse(scheduler.Step(Frame, false, Hold, out _), "a dropped chirp never comes back");
        }

        [Test]
        public void ANewerRequest_ReplacesAWaitingOne()
        {
            var scheduler = new ChirpScheduler(new AudioRandom(1u));
            scheduler.Request(ChirpMood.Affirm);
            scheduler.Step(Frame, true, Hold, out _);
            scheduler.Request(ChirpMood.Happy);
            Assert.IsTrue(scheduler.Step(Frame, false, Hold, out ChirpMood mood));
            Assert.AreEqual(ChirpMood.Happy, mood, "one chirp at a time: the latest mood");
        }
    }
}
