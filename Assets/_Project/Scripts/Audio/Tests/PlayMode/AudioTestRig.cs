using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// The real Audio domain (library and tuning assets from Assets/_Project/Data/Audio, built by the Audio builders)
    /// booted through GameBootstrap behind a fake rover, wired the way AudioSceneContributor wires Main.unity. The radio
    /// gets a test playlist of short generated tones so track changes happen within seconds.
    /// </summary>
    public sealed class AudioTestRig : IDisposable
    {
        public const int TestTrackSeconds = 3;

        private const string DataFolder = "Assets/_Project/Data/Audio/";
        private const int SampleRate = 48000;

        private readonly List<Object> _created = new List<Object>();

        public AudioTestRig()
        {
            InputActionAsset controls = Track(BootstrapHarness.LoadControlsCopy());
            Track(new GameObject("Listener", typeof(AudioListener)));
            Rover = Track(new GameObject("FakeRover")).AddComponent<FakeRoverSystem>();

            var playlist = Track(ScriptableObject.CreateInstance<RadioPlaylist>());
            playlist.Populate(new[]
            {
                new RadioTrack("t1", "Tone One", Track(Tone("t1", 293.66f)), 76f),
                new RadioTrack("t2", "Tone Two", Track(Tone("t2", 440f)), 80f),
            });

            GameObject audioRoot = Track(new GameObject("[Audio]"));
            audioRoot.SetActive(false);
            Director = Child(audioRoot, "AudioDirector").AddComponent<AudioDirector>();
            RoverAudio = Child(audioRoot, "RoverAudio").AddComponent<RoverAudio>();
            Gameplay = Child(audioRoot, "GameplayAudio").AddComponent<GameplayAudio>();
            Radio = Child(audioRoot, "RadioStation").AddComponent<RadioStation>();
            Ambience = Child(audioRoot, "AmbienceBed").AddComponent<AmbienceBed>();
            RoverAudio.Wire(Load<RoverAudioTuning>("RoverAudioTuning.asset"));
            Gameplay.Wire(Load<GameplayAudioTuning>("GameplayAudioTuning.asset"));
            Radio.Wire(Load<RadioTuning>("RadioTuning.asset"), playlist);
            Director.Wire(Load<AudioLibrary>("AudioLibrary.asset"), Load<AudioMixTuning>("AudioMixTuning.asset"),
                RoverAudio, Gameplay, Radio, Ambience);
            audioRoot.SetActive(true);

            Bootstrap = Track(BootstrapHarness.Create(controls, Rover, Director));
        }

        public FakeRoverSystem Rover { get; }

        public AudioDirector Director { get; }

        public RoverAudio RoverAudio { get; }

        public GameplayAudio Gameplay { get; }

        public RadioStation Radio { get; }

        public AmbienceBed Ambience { get; }

        public GameBootstrap Bootstrap { get; }

        public EventBus Events => Bootstrap.Context.Events;

        /// <summary>True if a director voice whose clip name starts with <paramref name="clipPrefix"/> is playing.</summary>
        public bool VoicePlaying(string clipPrefix)
        {
            return FindPlayingVoice(clipPrefix) != null;
        }

        public AudioSource FindPlayingVoice(string clipPrefix)
        {
            foreach (AudioSource voice in Director.GetComponentsInChildren<AudioSource>())
            {
                if (voice.clip != null && voice.clip.name.StartsWith(clipPrefix, StringComparison.Ordinal) &&
                    voice.isPlaying && voice.volume > 0f)
                {
                    return voice;
                }
            }

            return null;
        }

        public static AudioSource FindChildSource(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            Assert.IsNotNull(child, name);
            return child.GetComponent<AudioSource>();
        }

        public static Action UpdateOf(MonoBehaviour component)
        {
            MethodInfo method = component.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.Update");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }

        public void Dispose()
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
