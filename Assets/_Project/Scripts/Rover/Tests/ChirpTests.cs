using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07 speaks machine (docs/design/feel-pass.md §7.2): the events that make it chirp, the cooldowns, one at a time,
    /// and the body's sync to scripted syllables.
    /// </summary>
    public sealed class ChirpTests
    {
        private readonly ChirpSettings _settings = new ChirpSettings();
        private readonly List<RoverChirpRequested> _chirps = new List<RoverChirpRequested>();
        private EventBus _events;
        private RoverVoice _voice;
        private float _now;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _chirps.Clear();
            _events.Subscribe<RoverChirpRequested>(_chirps.Add);
            _now = 100f;
            _voice = new RoverVoice(_events, _settings, () => _now);
        }

        [TearDown]
        public void TearDown()
        {
            _voice.Dispose();
        }

        private void Dock(FeelPhase phase)
        {
            _events.Publish(new FeelBeat(FeelKind.Dock, phase, Vector3.zero, Vector3.up, 1f, 0f));
        }

        private ChirpMood Last => _chirps[_chirps.Count - 1].Mood;

        [Test]
        public void TheDockLock_Affirms_OrIsRelievedWhenNearlyEmpty()
        {
            _events.Publish(new BatteryChanged(3, 4, false, false));
            Dock(FeelPhase.Impact);
            Assert.AreEqual(ChirpMood.Affirm, Last);

            _now += 60f;
            _events.Publish(new BatteryChanged(0, 8, false, false));
            Dock(FeelPhase.Impact);
            Assert.AreEqual(ChirpMood.Relieved, Last, "Arriving at <= 15%: phew.");
        }

        [Test]
        public void EachTrigger_ChirpsItsMood()
        {
            var cases = new (System.Action publish, ChirpMood mood)[]
            {
                (() => Dock(FeelPhase.Reward), ChirpMood.Happy),
                (() => _events.Publish(new RoverKitFitted(RoverKitPiece.LampBar, false, "warm_headlamp")),
                    ChirpMood.Proud),
                (() => _events.Publish(new BatteryWarned(BatteryWarning.Low, Vector3.zero)), ChirpMood.Worried),
                (() => _events.Publish(new LowPowerChanged(true, false)), ChirpMood.Tired),
                (() => _events.Publish(new SiteAnswered("site.depot", Vector3.zero, 10f, true)), ChirpMood.Curious),
                (() => _events.Publish(new FriendGreeted("bell")), ChirpMood.Greeting),
            };
            foreach (var (publish, mood) in cases)
            {
                _now += 60f;
                publish();
                Assert.AreEqual(mood, Last);
            }
        }

        [Test]
        public void LoadedState_AndRepeatAnswers_StayQuiet()
        {
            _events.Publish(new LowPowerChanged(true, true));
            Assert.IsEmpty(_chirps, "A loaded empty battery doesn't chirp.");
            _events.Publish(new SiteAnswered("site.depot", Vector3.zero, 10f, true));
            _now += 60f;
            _events.Publish(new SiteAnswered("site.depot", Vector3.zero, 10f, true));
            Assert.AreEqual(1, _chirps.Count, "Only the first answer of a kind.");
        }

        [Test]
        public void Cooldowns_Global6_PerMood30_Affirm3()
        {
            var voice = new ChirpVoice(_settings);
            Assert.IsTrue(voice.TryChirp(ChirpMood.Affirm, 0f));
            Assert.IsFalse(voice.TryChirp(ChirpMood.Happy, 5f), "Global cooldown.");
            Assert.IsTrue(voice.TryChirp(ChirpMood.Happy, 6.5f));
            Assert.IsTrue(voice.TryChirp(ChirpMood.Affirm, 13f), "Affirm's own cooldown is short.");
            Assert.IsFalse(voice.TryChirp(ChirpMood.Happy, 20f), "Happy waits 30 s.");
            Assert.IsTrue(voice.TryChirp(ChirpMood.Happy, 37f));
        }

        [Test]
        public void OneAtATime_TheLastSyllableEndsThePhrase()
        {
            var voice = new ChirpVoice(_settings);
            Assert.IsTrue(voice.TryChirp(ChirpMood.Tired, 0f));
            voice.Syllable(0, 2, 0.5f, 0.1f);
            voice.Syllable(1, 2, 7f, 0.6f);
            Assert.IsTrue(voice.IsSounding(7f), "Still sounding past the global cooldown.");
            Assert.IsFalse(voice.TryChirp(ChirpMood.Worried, 7f));
            Assert.IsTrue(voice.TryChirp(ChirpMood.Worried, 7.7f));
        }

        [Test]
        public void Syllables_PulseTheEye_AndTheMoodPosesTheHead()
        {
            var pose = new ChirpPose(_settings);
            pose.Syllable(ChirpMood.Curious, 0, 3, 0.15f);
            pose.Step(0.01f);
            Assert.AreEqual(1f + _settings.EyePulse, pose.EyeGlow, 1e-4f, "The eye pulses +25%.");
            pose.Step(0.1f);
            Assert.AreEqual(1f, pose.EyeGlow, "For 80 ms.");
            for (int i = 0; i < 10; i++)
            {
                pose.Step(0.01f);
            }

            Assert.Greater(pose.HeadRoll, 0.5f * _settings.CuriousTilt, "Curious tilts the head.");
            for (int i = 0; i < 300; i++)
            {
                pose.Step(0.01f);
            }

            Assert.Less(pose.HeadRoll, 0.05f, "And eases back after the phrase.");

            pose.Syllable(ChirpMood.Worried, 1, 2, 0.2f);
            for (int i = 0; i < 40; i++)
            {
                pose.Step(0.01f);
            }

            Assert.Greater(pose.HeadDip, 0.5f * _settings.WorriedDip, "Worried dips the head.");
        }
    }
}
