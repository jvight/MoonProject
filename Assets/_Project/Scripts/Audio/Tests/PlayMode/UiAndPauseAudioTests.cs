using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Every UI cue kind, the hold-to-buy swell, rapid repeats and the pause mix (on the bus).</summary>
    public sealed class UiAndPauseAudioTests
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
            Time.timeScale = 1f;
            _rig.Dispose();
        }

        private AudioSource Last => _rig.Director.LastVoice;

        private void Publish(UiCueKind kind)
        {
            _rig.Events.Publish(new UiCue(kind));
        }

        private void AssertLastFlat(string clipPrefix)
        {
            Assert.IsNotNull(Last);
            StringAssert.StartsWith(clipPrefix, Last.clip.name);
            Assert.IsTrue(Last.isPlaying, clipPrefix);
            Assert.Greater(Last.volume, 0f, clipPrefix);
            Assert.AreEqual(0f, Last.spatialBlend, $"{clipPrefix} is a 2D UI sound");
        }

        [UnityTest]
        public IEnumerator EveryUiCueKind_PlaysItsOwnSoftCue()
        {
            (UiCueKind kind, string clip)[] expected =
            {
                (UiCueKind.MenuOpen, "ui_panel_open"), (UiCueKind.FocusMove, "ui_cursor_move"),
                (UiCueKind.Confirm, "ui_confirm"), (UiCueKind.SliderStep, "ui_cursor_move"),
                (UiCueKind.Back, "ui_cancel"), (UiCueKind.MenuClose, "ui_panel_close"),
                (UiCueKind.CardShown, "ui_toast"), (UiCueKind.PromptShown, "ui_toast"),
                (UiCueKind.HoldComplete, "ui_hold_complete"), (UiCueKind.PartPick, "part_pick"),
                (UiCueKind.PartDrop, "part_drop"), (UiCueKind.PartRefuse, "part_refuse"),
                (UiCueKind.DatabankPageFlip, "ui_relay_click"), (UiCueKind.DatabankChannelSettle, "ui_tube_thunk"),
                (UiCueKind.DatabankListStep, "ui_tape_tick"),
            };

            foreach ((UiCueKind kind, string clip) in expected)
            {
                Publish(kind);
                AssertLastFlat(clip);
                yield return null;
            }

            Assert.AreEqual(Enum.GetValues(typeof(UiCueKind)).Length, expected.Length + 2,
                "the remaining kinds (HoldFill, HoldRelease) drive the hold swell, tested below");
        }

        [UnityTest]
        public IEnumerator HoldFill_Swells_AndASoftReleaseTailStopsItWhenLetGo()
        {
            Publish(UiCueKind.HoldFill);
            AudioSource swell = _rig.Ui.HoldSwellSource;
            Assert.IsTrue(swell.isPlaying);
            Assert.AreEqual("ui_hold_fill", swell.clip.name);
            Assert.AreEqual(0f, swell.spatialBlend);
            yield return new WaitForSecondsRealtime(0.2f);

            Publish(UiCueKind.HoldRelease);
            yield return null;
            Assert.IsTrue(swell.isPlaying, "a soft release tail, not a cut");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(swell.isPlaying);
        }

        [UnityTest]
        public IEnumerator HoldComplete_HandsTheSwellOverToTheChord()
        {
            Publish(UiCueKind.HoldFill);
            yield return new WaitForSecondsRealtime(0.5f);

            Publish(UiCueKind.HoldComplete);
            AssertLastFlat("ui_hold_complete");
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.IsFalse(_rig.Ui.HoldSwellPlaying);
            Assert.IsTrue(_rig.VoicePlaying("ui_hold_complete"));
        }

        [UnityTest]
        public IEnumerator RePress_RestartsTheSwellFromTheBeginning()
        {
            Publish(UiCueKind.HoldFill);
            yield return new WaitForSecondsRealtime(0.3f);
            Publish(UiCueKind.HoldRelease);
            Publish(UiCueKind.HoldFill);

            AudioSource swell = _rig.Ui.HoldSwellSource;
            Assert.IsTrue(swell.isPlaying);
            Assert.Less(swell.timeSamples, 4800, "restarted at the start of the swell");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.IsTrue(swell.isPlaying, "the re-press is not stopped by the earlier release");
            Assert.Greater(swell.volume, 0f);
        }

        [UnityTest]
        public IEnumerator RapidFocusSteps_AreRateLimited_AndSoftenUntilAPause()
        {
            for (int i = 0; i < 6; i++)
            {
                Publish(UiCueKind.FocusMove);
            }

            Assert.AreEqual(1, CountPlaying("ui_cursor_move"), "same-frame repeats collapse into one tick");
            float first = Last.volume;

            float quietest = first;
            for (int i = 0; i < 6; i++)
            {
                yield return new WaitForSecondsRealtime(0.09f);
                Publish(UiCueKind.FocusMove);
                quietest = Mathf.Min(quietest, Last.volume);
            }

            Assert.Less(quietest, first * 0.8f, "a long streak gets softer");
            yield return new WaitForSecondsRealtime(0.6f);
            Publish(UiCueKind.FocusMove);
            Assert.Greater(Last.volume, quietest, "after a pause the tick is back to full");
        }

        [UnityTest]
        public IEnumerator Pause_DucksTheWorldLoops_KeepsRadioAndAmbience_AndRestoresOnResume()
        {
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            _rig.Wake(true);
            _rig.Rover.NormalizedSpeed = 0.8f;
            _rig.Rover.DriveInput = new Vector2(0f, 1f);
            yield return new WaitForSecondsRealtime(3f);

            AudioSource hum = _rig.RoverAudio.HumSource;
            AudioSource crunch = AudioTestRig.FindChildSource(_rig.RoverAudio.transform, "DustCrunch");
            AudioSource ambience = AudioTestRig.FindChildSource(_rig.Ambience.transform, "AmbienceLoop");
            AudioLowPassFilter radioFilter = _rig.Radio.GetComponentInChildren<AudioLowPassFilter>();
            Assert.Greater(hum.volume, 0f);
            Assert.Greater(crunch.volume, 0f);
            float music = LoudestDeckVolume();
            Assert.Greater(radioFilter.cutoffFrequency, 20000f);

            _rig.Events.Publish(new PauseChanged(true));
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.8f);

            Assert.IsTrue(_rig.Director.IsPaused);
            Assert.AreEqual(0f, hum.volume, 1e-4f, "the frozen motor hum is ducked away");
            Assert.AreEqual(0f, crunch.volume, 1e-4f);
            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio keeps playing");
            Assert.Less(radioFilter.cutoffFrequency, 8000f, "warmer, as if listening in the cabin");
            Assert.Less(LoudestDeckVolume(), music, "the radio steps back under the pause menu");
            Assert.IsTrue(ambience.isPlaying);
            Assert.Greater(ambience.volume, 0f);

            _rig.Events.Publish(new PauseChanged(false));
            Time.timeScale = 1f;
            yield return null;
            Assert.Less(hum.volume, 0.05f, "resume eases back in, no jump");
            yield return new WaitForSecondsRealtime(1.2f);

            Assert.IsFalse(_rig.Director.IsPaused);
            Assert.Greater(hum.volume, 0f);
            Assert.Greater(crunch.volume, 0f);
            Assert.Greater(radioFilter.cutoffFrequency, 20000f);
        }

        [UnityTest]
        public IEnumerator RoverFaceCues_And_SkyPhaseTransitions_PlayTheirR1CuesAndCrossfadeAmbience()
        {
            _rig.Milestone(OpeningMilestone.PowerOn, true);
            _rig.Wake(true);
            yield return null;

            (RoverFaceCueKind kind, string clip)[] faceExpected =
            {
                (RoverFaceCueKind.Boot, "face_boot"),
                (RoverFaceCueKind.Blink, "face_blink"),
                (RoverFaceCueKind.Happy, "face_happy"),
                (RoverFaceCueKind.Worried, "face_worried"),
                (RoverFaceCueKind.Asleep, "face_asleep"),
                (RoverFaceCueKind.Startled, "face_startled"),
                (RoverFaceCueKind.Proud, "face_proud"),
            };

            foreach ((RoverFaceCueKind kind, string clip) in faceExpected)
            {
                _rig.Events.Publish(new RoverFaceCued(kind));
                Assert.IsNotNull(Last);
                Assert.AreEqual(clip, Last.clip.name);
                Assert.AreEqual(1f, Last.spatialBlend, $"{clip} is a 3D face sound");
                yield return null;
            }

            // Sky transitions: the first charge ends the eclipse (diamond ring) -> Earthlit (nightfall settle) ->
            // Sunrise (dawn sweep). The eclipse's own sunrise adds nothing over the ring.
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Eclipse, 1f));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(_rig.Ambience.EclipseSource.isPlaying);
            Assert.Greater(_rig.Ambience.EclipseSource.volume, 0f);

            _rig.Milestone(OpeningMilestone.FirstCharge);
            AssertLastFlat("sky_diamond_ring");
            int plays = _rig.Director.PlayCount;
            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunrise, 0f));
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no dawn sweep on top of the ring");
            _rig.Milestone(OpeningMilestone.FirstCharge, true);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "a restored first charge stays quiet");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(_rig.Ambience.DaySource.isPlaying);
            Assert.Greater(_rig.Ambience.DaySource.volume, 0f);

            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Earthlit, 0f));
            AssertLastFlat("sky_nightfall_settle");
            yield return null;

            _rig.Events.Publish(new SkyPhaseChanged(SkyPhase.Sunrise, 0f));
            AssertLastFlat("sky_dawn_sweep");
        }

        private int CountPlaying(string clipPrefix)
        {
            int count = 0;
            foreach (AudioSource voice in _rig.Director.GetComponentsInChildren<AudioSource>())
            {
                if (voice.clip != null && voice.clip.name.StartsWith(clipPrefix, StringComparison.Ordinal) &&
                    voice.isPlaying)
                {
                    count++;
                }
            }

            return count;
        }

        private float LoudestDeckVolume()
        {
            return _rig.LoudestDeck().volume;
        }
    }
}
