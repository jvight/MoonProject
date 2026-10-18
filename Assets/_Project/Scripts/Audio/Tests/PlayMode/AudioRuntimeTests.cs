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
            _rig.Milestone(OpeningMilestone.PowerOn);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!_rig.Radio.MusicStarted && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_rig.Radio.MusicStarted, "the radio finds the music once the base has power");
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
            Assert.IsTrue(_rig.TitleMusic.enabled);
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
            _rig.Events.Publish(new BuildCue(BuildCueKind.Snap, Vector3.zero));
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
        public IEnumerator SteadyState_UpdatesAndEvents_AllocateNothing()
        {
            yield return new WaitForSecondsRealtime(0.2f);
            Action[] updates =
            {
                AudioTestRig.UpdateOf(_rig.Director), AudioTestRig.UpdateOf(_rig.RoverAudio),
                AudioTestRig.UpdateOf(_rig.Gameplay), AudioTestRig.UpdateOf(_rig.Ui), AudioTestRig.UpdateOf(_rig.Radio),
                AudioTestRig.UpdateOf(_rig.TitleMusic),
                AudioTestRig.UpdateOf(_rig.Ambience), AudioTestRig.UpdateOf(_rig.Soundscape),
                AudioTestRig.UpdateOf(_rig.SmallSounds), AudioTestRig.UpdateOf(_rig.Salvage),
                AudioTestRig.UpdateOf(_rig.Feel),
            };

            for (int i = 0; i < 6; i++)
            {
                _rig.Events.Publish(new StructureBuilt(i, "solar_array", Vector3.right * (4f * i), 0f, true));
            }

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
                PublishSome(events, settings, frame);
                for (int i = 0; i < updates.Length; i++)
                {
                    updates[i]();
                }
            }
        }

        private static void PublishSome(EventBus events, IAudioSettings settings, int frame)
        {
            switch (frame % 12)
            {
                case 0:
                    var material = (SalvageMaterial)(frame / 12 % 3);
                    events.Publish(new SalvageCutStarted(Vector3.one, material));
                    events.Publish(new MaterialSalvaged(material, 2, Vector3.one, "site.depot", frame / 12 % 9));
                    break;
                case 1:
                    events.Publish(new UiCue(frame % 24 == 1 ? UiCueKind.FocusMove : UiCueKind.SliderStep));
                    if (frame % 36 == 1)
                    {
                        events.Publish(new SalvageCutStopped(frame % 72 == 1));
                    }

                    break;
                case 2:
                    events.Publish(new BuildCue((BuildCueKind)(frame / 12 % 4), Vector3.forward * 6f));
                    events.Publish(new BuildWeldStateChanged(frame % 24 == 2, Vector3.forward * 6f, 0.5f));
                    break;
                case 3:
                    events.Publish(new StructurePowerChanged(frame / 12 % 6, "solar_array", Vector3.right * 4f,
                        frame % 24 == 3 ? 1f : 0f, false));
                    break;
                case 4:
                    if (frame % 48 == 4)
                    {
                        events.Publish(new RoverRecovering(Vector3.zero, Vector3.one, 0.5f));
                    }

                    break;
                case 5:
                    events.Publish(new SkyPhaseChanged((SkyPhase)(frame / 12 % 5), 0f));
                    break;
                case 6:
                    events.Publish(new UiCue(frame % 24 == 6 ? UiCueKind.HoldFill : UiCueKind.HoldRelease));
                    if (frame % 48 == 6)
                    {
                        events.Publish(new RoverPlaced(Vector3.right * 90f, Quaternion.identity));
                    }

                    break;
                case 7:
                    events.Publish(new ChargingChanged(frame % 24 == 7, ChargerKind.Dock, Vector3.back * 3f));
                    events.Publish(new BatteryChanged(frame / 12 % 5, 4, true, false));
                    break;
                case 8:
                    events.Publish(new UiCue(frame % 24 == 8 ? UiCueKind.CardShown : UiCueKind.PromptShown));
                    if (frame % 48 == 8)
                    {
                        events.Publish(new PartFitted(RoverSocket.Belly, RoverKitPiece.HoverCoils, false, false,
                            default, 0));
                    }

                    break;
                case 10:
                    events.Publish(new PauseChanged(frame % 24 == 10));
                    events.Publish(new RoverWideShotChanged(frame % 36 == 10));
                    events.Publish(new StargazingChanged(frame % 48 < 24));
                    break;
                case 9:
                    events.Publish(new OpeningMilestoneReached((OpeningMilestone)(frame / 12 % 3), frame % 24 == 9));
                    break;
                case 11:
                    events.Publish(new RoverLanded(Vector3.zero, 2.5f, 1f));
                    settings.SetVolume(AudioBus.Sfx, frame % 24 == 11 ? 0.9f : 1f);
                    break;
            }
        }
    }
}
