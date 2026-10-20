using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The first dusk heard: the beds turn toward evening with a soft swell, the charge hum slows; a restored
    /// dusk starts there silently.</summary>
    public sealed class DuskAudioTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunlit, 0f));
            yield return Settle(1f);
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        /// <summary>Night over day: the shared fade-in, soundscape and bus scale both beds alike and cancel.</summary>
        private float NightOverDay =>
            _rig.Ambience.NightSource.volume / Mathf.Max(1e-6f, _rig.Ambience.DaySource.volume);

        /// <summary>The evening blend's night-over-day ratio once settled: the night's share under the day's.</summary>
        private float SettledEvening
        {
            get
            {
                float share = _rig.Ambience.DuskNightShare;
                float night = CueVolume(AudioCueIds.AmbienceEarthlitNight);
                float day = CueVolume(AudioCueIds.AmbienceSunlitDay);
                return share * night / ((1f - share) * day);
            }
        }

        private float CueVolume(string id) => _rig.Director.Library.GetCue(_rig.Director.Resolve(id)).VolumeMax;

        private static IEnumerator Settle(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator TheFirstDusk_SwellsSoftly_AndTurnsTheBedsTowardEveningGradually()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new OpeningDuskBegan(false));
            Assert.AreEqual(plays + 1, _rig.Director.PlayCount, "one soft swell, no stinger");
            Assert.AreEqual("sky_dusk_swell", _rig.Director.LastClip.name);
            Assert.Greater(_rig.Ambience.DuskNightShare, 0f);

            yield return Settle(1.5f);
            Assert.Greater(Mathf.Abs(NightOverDay - SettledEvening), SettledEvening * 0.1f,
                "the beds glide toward evening over the dusk, not at once");
            Assert.IsTrue(_rig.Ambience.DaySource.isPlaying, "the day still holds at last light");
        }

        [UnityTest]
        public IEnumerator ARestoredDusk_StartsInTheEvening_Silently()
        {
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new OpeningDuskBegan(true));
            yield return null;
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no swell for a loaded dusk");
            Assert.AreEqual(SettledEvening, NightOverDay, SettledEvening * 0.05f,
                "in the evening from the first frame");
            yield return Settle(1f);
            Assert.AreEqual(SettledEvening, NightOverDay, SettledEvening * 0.05f, "and it stays there");
        }

        [UnityTest]
        public IEnumerator TheChargeHum_SlowsAStepAtDusk_AndRunsFreeAgainAtDawn()
        {
            FeelAudioTuning tuning = _rig.FeelTuning;
            float slowed = Mathf.Pow(2f, tuning.DuskChargeSemitones / 12f);
            _rig.Events.Publish(new OpeningDuskBegan(false));
            yield return null;
            Assert.Greater(_rig.Feel.ChargerPitch, 0.98f, "the slowing is a glide");
            yield return Settle(1.5f);
            Assert.Less(_rig.Feel.ChargerPitch, 1f);
            Assert.Greater(_rig.Feel.ChargerPitch, slowed);

            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunrise, 0f));
            yield return Settle(1.5f);
            float recovering = _rig.Feel.ChargerPitch;
            Assert.Greater(recovering, slowed, "a new day lifts it back");

            _rig.Events.Publish(new OpeningDuskBegan(true));
            yield return null;
            Assert.AreEqual(slowed, _rig.Feel.ChargerPitch, 0.01f, "a loaded dusk starts slowed");
        }
    }
}
