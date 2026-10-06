using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// Boots the real AudioDirector (library + tuning assets from Assets/_Project/Data/Audio, built by the Audio
    /// builders) behind a fake rover, the way Main.unity wires it, and checks the sounds it actually schedules.
    /// The radio gets a test playlist of short generated tones so a track change happens within seconds.
    /// </summary>
    public sealed class AudioRuntimeTests
    {
        private const string DataFolder = "Assets/_Project/Data/Audio/";
        private const int TestTrackSeconds = 3;
        private const int SampleRate = 48000;

        private readonly List<Object> _created = new List<Object>();
        private InputActionAsset _controls;
        private FakeRoverSystem _rover;
        private AudioDirector _director;
        private RadioStation _radio;
        private RoverAudio _roverAudio;
        private AmbienceBed _ambience;
        private GameBootstrap _bootstrap;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _controls = Track(BootstrapHarness.LoadControlsCopy());
            Track(new GameObject("Listener", typeof(AudioListener)));
            _rover = Track(new GameObject("FakeRover")).AddComponent<FakeRoverSystem>();

            var playlist = Track(ScriptableObject.CreateInstance<RadioPlaylist>());
            playlist.Populate(new[]
            {
                new RadioTrack("t1", "Tone One", Track(Tone("t1", 293.66f)), 76f),
                new RadioTrack("t2", "Tone Two", Track(Tone("t2", 440f)), 80f),
            });

            GameObject audioRoot = Track(new GameObject("[Audio]"));
            audioRoot.SetActive(false);
            _director = Child(audioRoot, "AudioDirector").AddComponent<AudioDirector>();
            _roverAudio = Child(audioRoot, "RoverAudio").AddComponent<RoverAudio>();
            _radio = Child(audioRoot, "RadioStation").AddComponent<RadioStation>();
            _ambience = Child(audioRoot, "AmbienceBed").AddComponent<AmbienceBed>();
            _roverAudio.Wire(Load<RoverAudioTuning>("RoverAudioTuning.asset"));
            _radio.Wire(Load<RadioTuning>("RadioTuning.asset"), playlist);
            _director.Wire(Load<AudioLibrary>("AudioLibrary.asset"), Load<AudioMixTuning>("AudioMixTuning.asset"),
                _roverAudio, _radio, _ambience);
            audioRoot.SetActive(true);

            _bootstrap = Track(BootstrapHarness.Create(_controls, _rover, _director));
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.Destroy(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void Director_Initialises_WithAllPartsRunning()
        {
            Assert.IsTrue(_director.IsInitialized);
            Assert.IsTrue(_roverAudio.enabled);
            Assert.IsTrue(_radio.enabled);
            Assert.IsTrue(_ambience.enabled);
            Assert.AreEqual(2, _bootstrap.Context.Events.SubscriberCount<RoverLanded>(),
                "director (thump) and rover audio (creak) both listen to landings");
        }

        [UnityTest]
        public IEnumerator Landing_PlaysAThumpAtOnce_AndACreakShortlyAfter()
        {
            _bootstrap.Context.Events.Publish(new RoverLanded(new Vector3(3f, 0f, 4f), 3f, 1.2f));
            Assert.IsTrue(VoicePlaying("landing_thump"), "thump voice");
            Assert.IsFalse(VoicePlaying("suspension_creak"), "creak waits for the springs to settle");

            yield return new WaitForSecondsRealtime(0.4f);

            Assert.IsTrue(VoicePlaying("suspension_creak"), "creak voice");
        }

        [UnityTest]
        public IEnumerator SoftLanding_IsSilent()
        {
            _bootstrap.Context.Events.Publish(new RoverLanded(Vector3.zero, 0.1f, 0.2f));
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsFalse(VoicePlaying("landing_thump"));
            Assert.IsFalse(VoicePlaying("suspension_creak"));
        }

        [UnityTest]
        public IEnumerator Radio_ClosesTheLowPassAndRaisesStatic_AwayFromBase()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            AudioLowPassFilter filter = _radio.GetComponentInChildren<AudioLowPassFilter>();
            AudioSource staticLoop = FindChildSource(_radio.transform, "Static");
            Assert.Greater(_radio.Clarity, 0.99f);
            Assert.Greater(filter.cutoffFrequency, 20000f);
            float nearStatic = staticLoop.volume;

            _rover.Position = new Vector3(600f, 0f, 0f);
            yield return new WaitForSecondsRealtime(4f);

            Assert.Less(_radio.Clarity, 0.05f);
            Assert.Less(filter.cutoffFrequency, 1200f);
            Assert.Greater(staticLoop.volume, nearStatic * 5f);

            _radio.SetSignalRadius(1000f);
            yield return new WaitForSecondsRealtime(6f);
            Assert.Greater(_radio.Clarity, 0.95f, "a wider signal radius brings the music back");
        }

        [UnityTest]
        public IEnumerator Radio_TunesToTheOtherTrack_WhenOneEnds()
        {
            AudioClip first = LoudestDeckClip();
            AudioSource swish = FindChildSource(_radio.transform, "TuningSwish");
            Assert.IsNotNull(first);
            bool swishPlayed = false;
            AudioClip next = first;
            float deadline = Time.realtimeSinceStartup + TestTrackSeconds + 4f;
            while (next == first && Time.realtimeSinceStartup < deadline)
            {
                swishPlayed |= swish.isPlaying;
                yield return null;
                next = LoudestDeckClip();
            }

            Assert.AreNotSame(first, next, "the station moves on to the other track (no immediate repeat)");
            Assert.IsTrue(swishPlayed, "the dial-tuning swish plays during the change");
        }

        [UnityTest]
        public IEnumerator SteadyState_Updates_AllocateNothing()
        {
            yield return new WaitForSecondsRealtime(0.2f);
            Action[] updates =
            {
                UpdateOf(_roverAudio), UpdateOf(_radio), UpdateOf(_ambience),
            };

            RunFrames(updates, 30);
            long before = GC.GetAllocatedBytesForCurrentThread();
            RunFrames(updates, 300);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0L, allocated, "bytes allocated by 300 frames of rover audio, radio and ambience");
        }

        private void RunFrames(Action[] updates, int frames)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                float t = frame * 0.05f;
                _rover.Position = new Vector3(200f * Mathf.Sin(t), 0f, 0f);
                _rover.NormalizedSpeed = 0.5f + 0.5f * Mathf.Sin(t * 3f);
                _rover.DriveInput = new Vector2(0f, 1f);
                _rover.IsGrounded = frame % 40 < 30;
                _rover.GroundNormal = Quaternion.Euler(frame % 20 == 0 ? 12f : 0f, 0f, 0f) * Vector3.up;
                for (int i = 0; i < updates.Length; i++)
                {
                    updates[i]();
                }
            }
        }

        private static Action UpdateOf(MonoBehaviour component)
        {
            MethodInfo method = component.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.Update");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }

        private bool VoicePlaying(string clipPrefix)
        {
            foreach (AudioSource voice in _director.GetComponentsInChildren<AudioSource>())
            {
                if (voice.clip != null && voice.clip.name.StartsWith(clipPrefix, StringComparison.Ordinal) &&
                    voice.isPlaying && voice.volume > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private AudioClip LoudestDeckClip()
        {
            AudioSource a = FindChildSource(_radio.transform, "DeckA");
            AudioSource b = FindChildSource(_radio.transform, "DeckB");
            AudioSource loud = a.volume >= b.volume ? a : b;
            return loud.clip;
        }

        private static AudioSource FindChildSource(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            Assert.IsNotNull(child, name);
            return child.GetComponent<AudioSource>();
        }

        private static AudioClip Tone(string name, float frequency)
        {
            int count = TestTrackSeconds * SampleRate;
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = 0.2f * Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static T Load<T>(string file) where T : Object
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<T>(DataFolder + file);
            Assert.IsNotNull(asset, $"{DataFolder}{file} missing: run the Audio builders first");
            return asset;
#else
            throw new NotSupportedException("These tests load builder output through the editor's AssetDatabase.");
#endif
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
