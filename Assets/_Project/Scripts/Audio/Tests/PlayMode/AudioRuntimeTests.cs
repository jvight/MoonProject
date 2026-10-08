using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>Director, rover audio, radio and ambience running for real behind a fake rover.</summary>
    public sealed class AudioRuntimeTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
            _rig.Wake(true);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music after 07 wakes");
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        [Test]
        public void Director_Initialises_WithAllPartsRunning()
        {
            Assert.IsTrue(_rig.Director.IsInitialized);
            Assert.IsTrue(_rig.RoverAudio.enabled);
            Assert.IsTrue(_rig.Gameplay.enabled);
            Assert.IsTrue(_rig.Radio.enabled);
            Assert.IsTrue(_rig.Ambience.enabled);
            Assert.AreEqual(2, _rig.Events.SubscriberCount<RoverLanded>(),
                "director (thump) and rover audio (creak) both listen to landings");
        }

        [Test]
        public void AudioSettings_AreRegistered_AndDriveTheBuses()
        {
            var settings = _rig.Bootstrap.Context.Get<IAudioSettings>();
            settings.SetVolume(AudioBus.Sfx, 0.25f);

            Assert.AreEqual(0.25f, settings.GetVolume(AudioBus.Sfx));
            Assert.AreEqual(0.25f * _rig.Director.Buses.GetVolume(AudioBus.Master),
                _rig.Director.Buses.Effective(AudioBus.Sfx), 1e-6f);
        }

        [UnityTest]
        public IEnumerator AudioSettings_ReLevelRingingOneShots_Immediately()
        {
            _rig.Events.Publish(new RelicSurfaced(Vector3.zero, "relic_test"));
            AudioSource voice = _rig.Director.LastVoice;
            float before = voice.volume;

            _rig.Bootstrap.Context.Get<IAudioSettings>().SetVolume(AudioBus.Sfx, 0.5f);
            yield return null;

            Assert.IsTrue(voice.isPlaying);
            Assert.AreEqual(before * 0.5f, voice.volume, 1e-4f);
        }

        [UnityTest]
        public IEnumerator Landing_PlaysAThumpAtOnce_AndACreakShortlyAfter()
        {
            _rig.Events.Publish(new RoverLanded(new Vector3(3f, 0f, 4f), 3f, 1.2f));
            Assert.IsTrue(_rig.VoicePlaying("landing_thump"), "thump voice");
            Assert.IsFalse(_rig.VoicePlaying("suspension_creak"), "creak waits for the springs to settle");

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.IsTrue(_rig.VoicePlaying("suspension_creak"), "creak voice");
        }

        [UnityTest]
        public IEnumerator SoftLanding_IsSilent()
        {
            _rig.Events.Publish(new RoverLanded(Vector3.zero, 0.1f, 0.2f));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(_rig.VoicePlaying("landing_thump"));
            Assert.IsFalse(_rig.VoicePlaying("suspension_creak"));
        }

        [UnityTest]
        public IEnumerator Radio_ClosesTheLowPassAndRaisesStatic_AwayFromBase()
        {
            // Let the wake-up crackle melt away first: the baseline is the settled, clear station.
            yield return new WaitForSecondsRealtime(2f);
            AudioLowPassFilter filter = _rig.Radio.GetComponentInChildren<AudioLowPassFilter>();
            AudioSource staticLoop = AudioTestRig.FindChildSource(_rig.Radio.transform, "Static");
            Assert.Greater(_rig.Radio.Clarity, 0.99f);
            Assert.Greater(filter.cutoffFrequency, 20000f);
            float nearStatic = staticLoop.volume;

            // Just inside the signal's edge: all static, before the soundscape fades the set out past it.
            _rig.Rover.Position = new Vector3(_rig.Radio.SignalEdge - 5f, 0f, 0f);
            yield return new WaitForSecondsRealtime(4f);

            Assert.Less(_rig.Radio.Clarity, 0.05f);
            Assert.Less(filter.cutoffFrequency, 1200f);
            float edgeStatic = staticLoop.volume;
            Assert.Greater(edgeStatic, nearStatic * 5f);

            _rig.Rover.Position = new Vector3(600f, 0f, 0f);
            yield return new WaitForSecondsRealtime(8f);
            Assert.Less(staticLoop.volume, edgeStatic * 0.05f, "far past the signal even the static falls quiet");
        }

        [UnityTest]
        public IEnumerator SignalRadiusChanged_BringsTheMusicBack()
        {
            _rig.Rover.Position = new Vector3(600f, 0f, 0f);
            yield return new WaitForSecondsRealtime(3f);
            Assert.Less(_rig.Radio.Clarity, 0.05f);

            _rig.Events.Publish(new SignalRadiusChanged(1000f));
            Assert.AreEqual(1000f, _rig.Radio.TargetSignalRadius);
            yield return new WaitForSecondsRealtime(6f);

            Assert.Greater(_rig.Radio.Clarity, 0.95f, "a wider signal radius blooms the music back in");
        }

        [UnityTest]
        public IEnumerator SteadyState_UpdatesAndEvents_AllocateNothing()
        {
            yield return new WaitForSecondsRealtime(0.2f);
            Action[] updates =
            {
                AudioTestRig.UpdateOf(_rig.Director), AudioTestRig.UpdateOf(_rig.RoverAudio),
                AudioTestRig.UpdateOf(_rig.Gameplay), AudioTestRig.UpdateOf(_rig.Ui), AudioTestRig.UpdateOf(_rig.Radio),
                AudioTestRig.UpdateOf(_rig.Friends), AudioTestRig.UpdateOf(_rig.Jump),
                AudioTestRig.UpdateOf(_rig.Ambience), AudioTestRig.UpdateOf(_rig.Canyon),
                AudioTestRig.UpdateOf(_rig.Soundscape), AudioTestRig.UpdateOf(_rig.SmallSounds),
                AudioTestRig.UpdateOf(_rig.Relays),
            };

            _rig.Program.Own(AudioTestRig.TapeA);
            _rig.Program.SelectedTape = AudioTestRig.TapeA;
            RunFrames(updates, 60);
            long before = GC.GetAllocatedBytesForCurrentThread();
            RunFrames(updates, 300);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0L, allocated, "bytes allocated by 300 frames of audio updates and gameplay events");
        }

        private void RunFrames(Action[] updates, int frames)
        {
            EventBus events = _rig.Events;
            IAudioSettings settings = _rig.Bootstrap.Context.Get<IAudioSettings>();
            for (int frame = 0; frame < frames; frame++)
            {
                float t = frame * 0.05f;
                _rig.Rover.Position = new Vector3(20f * Mathf.Sin(t), 2f - 4f * Mathf.Sin(t * 2f),
                    260f + 120f * Mathf.Sin(t * 0.3f));
                _rig.Rover.NormalizedSpeed = 0.5f + 0.5f * Mathf.Sin(t * 3f);
                bool driving = frame % 60 < 40;
                _rig.Rover.Speed = driving ? 4f : 0f;
                _rig.Rover.Resting = !driving;
                _rig.Rover.StillSeconds = driving ? 0f : (frame % 60 - 40) * 0.5f;
                _rig.Rover.DriveInput = driving ? new Vector2(Mathf.Sin(t * 4f), 1f) : Vector2.zero;
                _rig.Rover.TetherOrigin.localRotation = Quaternion.Euler(0f, 60f * Mathf.Sin(t * 2f), 0f);
                _rig.Rover.IsGrounded = frame % 40 < 30;
                _rig.Rover.AirTime = frame % 40 < 30 ? 0f : (frame % 40 - 30) * 0.1f;
                _rig.Rover.Velocity = new Vector3(6f * Mathf.Sin(t), 2f, 0f);
                _rig.Rover.GroundNormal = Quaternion.Euler(frame % 20 == 0 ? 12f : 0f, 0f, 0f) * Vector3.up;
                _rig.Rover.Tilly.Position = new Vector3(3f * Mathf.Cos(t), 3f, 3f * Mathf.Sin(t));
                _rig.Rover.Tilly.Activity = (FriendActivity)(frame / 50 % 6);
                _rig.Rover.Tilly.RotorSpeed = frame / 50 % 6 >= 2 ? 0.5f + 0.5f * Mathf.Sin(t) : 0f;
                _rig.Rover.Tilly.RepairProgress = Mathf.Repeat(t * 0.2f, 1f);
                _rig.Rover.Bell.Position = new Vector3(-6f + 2f * Mathf.Sin(t), 0f, 4f);
                _rig.Rover.Bell.Activity = (FriendActivity)(frame / 40 % 6);
                _rig.Rover.Bell.RotorSpeed = frame / 40 % 6 >= 2 ? 0.4f : 0f;
                PublishSome(events, settings, _rig.Program, frame);
                for (int i = 0; i < updates.Length; i++)
                {
                    updates[i]();
                }
            }
        }

        private static void PublishSome(EventBus events, IAudioSettings settings, FakeRadioProgram program, int frame)
        {
            switch (frame % 12)
            {
                case 0:
                    events.Publish(new ScrapCollected(Vector3.one, 1, frame / 12));
                    break;
                case 1:
                    events.Publish(new UiCue(frame % 24 == 1 ? UiCueKind.FocusMove : UiCueKind.SliderStep));
                    break;
                case 2:
                    events.Publish(new SonarPinged(Vector3.zero, 80f));
                    events.Publish(new FriendAnswered("tilly", Vector3.one));
                    events.Publish(new FriendSpotted("tilly", Vector3.forward));
                    events.Publish(new FriendPartCollected("tilly", 0, frame % 36 == 2 ? 3 : 1, 3));
                    events.Publish(new FriendGreeted("tilly"));
                    events.Publish(new BellCued((BellCue)(frame / 12 % 5), Vector3.one));
                    if (frame % 48 == 2)
                    {
                        events.Publish(new FriendRepaired("bell"));
                        events.Publish(new FriendGreeted("bell"));
                    }

                    break;
                case 3:
                    string relic = frame % 24 == 3 ? "teapot" : "rubber_duck";
                    events.Publish(new RelicAnswered(Vector3.forward * 40f, 40f, relic));
                    break;
                case 4:
                    if (frame % 48 == 4)
                    {
                        events.Publish(new RoverRecovering(Vector3.zero, Vector3.one, 0.5f));
                    }

                    events.Publish(new RoverJumpCharged(frame % 5 / 4f));
                    if (frame % 36 == 4)
                    {
                        events.Publish(new RoverJumpCancelled());
                    }

                    break;
                case 5:
                    events.Publish(new TetherAttached(Vector3.right, 5f));
                    if (frame % 36 == 5)
                    {
                        program.DialUnlocked = frame >= 36;
                        program.Channel = (RadioChannel)(frame / 36 % 3);
                        events.Publish(new RadioProgramChanged());
                    }

                    break;
                case 6:
                    events.Publish(new UiCue(frame % 24 == 6 ? UiCueKind.HoldFill : UiCueKind.HoldRelease));
                    events.Publish(new RelayCued((RelayCue)(frame / 12 % 5), "relay.0", Vector3.right * 90f));
                    if (frame % 48 == 6)
                    {
                        events.Publish(new RelayRestored("relay.0", Vector3.right * 90f, 1, 4));
                        events.Publish(new RadioHopStarted("home", "relay.0"));
                        events.Publish(new RoverPlaced(Vector3.right * 90f, Quaternion.identity));
                        events.Publish(new RadioHopFinished("relay.0"));
                    }

                    break;
                case 7:
                    events.Publish(new ExcavationStarted(Vector3.left));
                    events.Publish(new CassetteCollected("slow_orbit", Vector3.up, 1, 3));
                    events.Publish(new CrewLogFound("ro_1", Vector3.right));
                    events.Publish(new BellSignalPicked(BellSignalTarget.Relic, Vector3.forward * 200f));
                    events.Publish(new BellSignalFound(BellSignalTarget.Cassette, Vector3.back));
                    break;
                case 8:
                    events.Publish(new UiCue(frame % 24 == 8 ? UiCueKind.CardShown : UiCueKind.PromptShown));
                    break;
                case 10:
                    events.Publish(new PauseChanged(frame % 24 == 10));
                    events.Publish(new RoverWideShotChanged(frame % 36 == 10));
                    break;
                case 9:
                    events.Publish(new TetherReleased(Vector3.right, frame % 24 == 9));
                    events.Publish(new ExcavationStopped(Vector3.left, true));
                    break;
                case 11:
                    events.Publish(new RoverJumped(frame % 24 == 11 ? 1f : 0.2f));
                    events.Publish(new RoverLanded(Vector3.zero, 2.5f, 1f));
                    events.Publish(new UpgradePurchased(frame % 24 == 11 ? "rover.hover_jump" : "radio_tower", 1));
                    settings.SetVolume(AudioBus.Sfx, frame % 24 == 11 ? 0.9f : 1f);
                    break;
            }
        }
    }
}
