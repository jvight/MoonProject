using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Each gameplay event, published on the bus, plays the right cue (and loops start and stop).</summary>
    public sealed class GameplayAudioTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private AudioSource Last => _rig.Director.LastVoice;

        private void AssertLastPlayed(string clipName, Vector3 position)
        {
            Assert.IsNotNull(Last);
            Assert.AreEqual(clipName, Last.clip.name);
            Assert.IsTrue(Last.isPlaying, clipName);
            Assert.Greater(Last.volume, 0f, clipName);
            Assert.AreEqual(1f, Last.spatialBlend, clipName);
            Assert.Less(Vector3.Distance(position, Last.transform.position), 1e-4f, clipName);
        }

        [Test]
        public void SonarPinged_PlaysThePingAtTheOrigin()
        {
            var origin = new Vector3(1f, 2f, 3f);
            _rig.Events.Publish(new SonarPinged(origin, 80f));
            AssertLastPlayed("sonar_ping", origin);
        }

        [Test]
        public void RelicAnswered_EachRelicSingsItsOwnNote()
        {
            string[] relics =
            {
                "astronaut_boot", "cassette_player", "garden_gnome", "golden_record", "rubber_duck", "teapot",
            };
            foreach (string relic in relics)
            {
                var at = new Vector3(relic.Length, 0f, 3f);
                _rig.Events.Publish(new RelicAnswered(at, 20f, relic));
                AssertLastPlayed("relic_answer_" + relic, at);
                Assert.AreEqual(1f, Last.pitch, 1e-6f, "answers stay in key: no pitch variance");
            }
        }

        [Test]
        public void RelicAnswered_UnknownRelic_IsReportedAndSilent()
        {
            _rig.Events.Publish(new SonarPinged(Vector3.zero, 80f));
            AudioSource before = Last;
            LogAssert.Expect(LogType.Error, new Regex("no answer voice for relic 'mystery_box'"));

            _rig.Events.Publish(new RelicAnswered(Vector3.zero, 10f, "mystery_box"));

            Assert.AreSame(before, Last);
        }

        [Test]
        public void RelicAnswered_PlaysAtTheRelic_SofterAndDarkerWhenFar()
        {
            var near = new Vector3(5f, 0f, 0f);
            _rig.Events.Publish(new RelicAnswered(near, 5f, "teapot"));
            AssertLastPlayed("relic_answer_teapot", near);
            AudioSource nearVoice = Last;
            float nearVolume = nearVoice.volume;
            float nearCutoff = nearVoice.GetComponent<AudioLowPassFilter>().cutoffFrequency;

            var far = new Vector3(120f, 0f, 0f);
            _rig.Events.Publish(new RelicAnswered(far, 120f, "teapot"));
            AssertLastPlayed("relic_answer_teapot", far);
            Assert.AreNotSame(nearVoice, Last);
            Assert.Less(Last.volume, nearVolume);
            Assert.Less(Last.GetComponent<AudioLowPassFilter>().cutoffFrequency, nearCutoff * 0.5f);
            Assert.Greater(Last.minDistance, 10f, "answers carry further than ordinary 3D cues");
        }

        [Test]
        public void ScrapCollected_ClimbsThePentatonic_ThenWeavesOverTheTop()
        {
            string[] expected =
            {
                "scrap_chime_D5", "scrap_chime_E5", "scrap_chime_Fs5", "scrap_chime_A5", "scrap_chime_B5",
                "scrap_chime_D6", "scrap_chime_E6", "scrap_chime_Fs6", "scrap_chime_E6", "scrap_chime_D6",
                "scrap_chime_B5", "scrap_chime_D6", "scrap_chime_E6", "scrap_chime_Fs6", "scrap_chime_E6",
            };

            for (int step = 0; step < expected.Length; step++)
            {
                _rig.Events.Publish(new ScrapCollected(Vector3.one * step, 1, step));
                AssertLastPlayed(expected[step], Vector3.one * step);
                Assert.AreEqual(1f, Last.pitch, 1e-6f, "chimes stay in key: no pitch variance");
            }
        }

        [UnityTest]
        public IEnumerator Tether_PlucksAndHums_FollowsTheEmitter_ThenReleasesAndFallsSilent()
        {
            var attach = new Vector3(4f, 0f, 2f);
            _rig.Events.Publish(new TetherAttached(attach, 8f));
            AssertLastPlayed("tether_attach", attach);

            yield return new WaitForSecondsRealtime(0.6f);
            AudioSource hum = _rig.Gameplay.TetherHumSource;
            Assert.IsTrue(hum.isPlaying, "hum loop while attached");
            Assert.IsTrue(hum.loop);
            Assert.Greater(hum.volume, 0f);

            _rig.Rover.TetherOrigin.position = new Vector3(30f, 2f, -7f);
            yield return null;
            Assert.Less(Vector3.Distance(hum.transform.position, _rig.Rover.TetherOrigin.position), 1e-4f);

            var release = new Vector3(6f, 0f, 1f);
            _rig.Events.Publish(new TetherReleased(release, false));
            AssertLastPlayed("tether_release", release);

            yield return new WaitForSecondsRealtime(1f);
            Assert.IsFalse(hum.isPlaying, "hum stops once its fade-out ends");
            Assert.IsFalse(_rig.Gameplay.TetherHumAudible);
        }

        [Test]
        public void TetherSnapped_PlaysTheSofterSigh()
        {
            _rig.Events.Publish(new TetherAttached(Vector3.zero, 3f));
            var at = new Vector3(0f, 0f, 9f);
            _rig.Events.Publish(new TetherReleased(at, true));
            AssertLastPlayed("tether_snap", at);
        }

        [UnityTest]
        public IEnumerator Excavation_RumblesAtTheDigSite_StopsAfterward_AndSparklesWhenTheRelicSurfaces()
        {
            var site = new Vector3(-12f, 0f, 20f);
            _rig.Events.Publish(new ExcavationStarted(site));
            yield return new WaitForSecondsRealtime(1f);

            AudioSource rumble = _rig.Gameplay.RumbleSource;
            Assert.IsTrue(rumble.isPlaying);
            Assert.Greater(rumble.volume, 0f);
            Assert.Less(Vector3.Distance(site, rumble.transform.position), 1e-4f);

            _rig.Events.Publish(new ExcavationStopped(site, true));
            _rig.Events.Publish(new RelicSurfaced(site, "relic_test"));
            AssertLastPlayed("surfacing_sparkle", site);

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsFalse(rumble.isPlaying, "rumble stops once its fade-out ends");
        }

        [UnityTest]
        public IEnumerator InterruptedExcavation_AlsoStopsTheRumble()
        {
            _rig.Events.Publish(new ExcavationStarted(Vector3.zero));
            yield return new WaitForSecondsRealtime(0.3f);
            _rig.Events.Publish(new ExcavationStopped(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsFalse(_rig.Gameplay.RumbleSource.isPlaying);
            Assert.IsFalse(_rig.VoicePlaying("surfacing_sparkle"));
        }

        [Test]
        public void RelicDeposited_PlaysThePlacedCueAtTheShelf()
        {
            var shelf = new Vector3(2f, 1f, -3f);
            _rig.Events.Publish(new RelicDeposited("relic_test", shelf, 1));
            AssertLastPlayed("relic_placed", shelf);
        }

        [Test]
        public void UpgradePurchased_PlaysTheArpeggioFlat()
        {
            _rig.Events.Publish(new UpgradePurchased("radio_tower", 2));
            Assert.AreEqual("upgrade_arpeggio", Last.clip.name);
            Assert.IsTrue(Last.isPlaying);
            Assert.AreEqual(0f, Last.spatialBlend, "a 2D stinger");
        }

        [Test]
        public void CassetteCollected_ClicksAndSpinsWhereTheTapeArrives()
        {
            var socket = new Vector3(3f, 1.2f, -2f);
            _rig.Events.Publish(new CassetteCollected("slow_orbit", socket, 2, 3));
            AssertLastPlayed("cassette_pickup", socket);
        }

        [Test]
        public void CrewLogFound_OpensTheTinAtTheCache()
        {
            var cache = new Vector3(40f, 6f, 380f);
            _rig.Events.Publish(new CrewLogFound("ro_1", cache));
            AssertLastPlayed("crew_log_found", cache);
        }

        [Test]
        public void BellSignalPicked_ShimmersSoftlyFromThePillar_MostlyFlatSoEvenAFarOneIsHeard()
        {
            var pillar = new Vector3(260f, 0f, -180f);
            _rig.Events.Publish(new BellSignalPicked(BellSignalTarget.Cassette, pillar));
            Assert.AreEqual("bell_signal_pick", _rig.Director.LastClip.name);
            AudioSource shimmer = _rig.Gameplay.SignalSource;
            Assert.IsTrue(shimmer.isPlaying);
            Assert.Less(Vector3.Distance(pillar, shimmer.transform.position), 1e-4f, "a hint of its direction");
            Assert.Greater(shimmer.spatialBlend, 0f);
            Assert.Less(shimmer.spatialBlend, 0.5f, "mostly flat: distant, not lost");
        }

        [Test]
        public void BellSignalFound_ChimesWhereTheThingWas()
        {
            var found = new Vector3(-12f, 0f, 30f);
            _rig.Events.Publish(new BellSignalFound(BellSignalTarget.CrewLog, found));
            AssertLastPlayed("bell_signal_found", found);
        }
    }
}
