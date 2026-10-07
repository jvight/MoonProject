using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// The real Audio domain (library and tuning assets from Assets/_Project/Data/Audio, built by the Audio builders)
    /// booted through GameBootstrap behind a fake rover, wired the way AudioSceneContributor wires Main.unity.
    /// The radio gets a test playlist and two test tapes of generated tones (short ones change track within
    /// seconds).
    /// </summary>
    public sealed class AudioTestRig : IDisposable
    {
        /// <summary>Short tracks: the station changes track within seconds (playlist tests).</summary>
        public const int ShortTrackSeconds = 3;

        /// <summary>Long tracks: no track change (and its crossfade static) during a test.</summary>
        public const int LongTrackSeconds = 30;

        /// <summary>Cassette ids of the two test tapes (a third exists in the program but has no track).</summary>
        public const string TapeA = "after_dark_1";

        public const string TapeB = "slow_orbit";

        private const string DataFolder = "Assets/_Project/Data/Audio/";
        private const int SampleRate = 48000;

        private readonly List<Object> _created = new List<Object>();

        public AudioTestRig(int trackSeconds = LongTrackSeconds)
        {
            InputActionAsset controls = Track(BootstrapHarness.LoadControlsCopy());
            Track(new GameObject("Listener", typeof(AudioListener)));
            Rover = Track(new GameObject("FakeRover")).AddComponent<FakeRoverSystem>();

            var playlist = Track(ScriptableObject.CreateInstance<RadioPlaylist>());
            playlist.Populate(new[]
            {
                new RadioTrack("t1", "Tone One", Track(Tone("t1", 293.66f, trackSeconds)), 76f),
                new RadioTrack("t2", "Tone Two", Track(Tone("t2", 440f, trackSeconds)), 80f),
            });
            var tapes = Track(ScriptableObject.CreateInstance<RadioTapeLibrary>());
            tapes.Populate(new[]
            {
                new RadioTrack(TapeA, "Tape A", Track(Tone(TapeA, 369.99f, trackSeconds)), 84f, TapeA),
                new RadioTrack(TapeB, "Tape B", Track(Tone(TapeB, 329.63f, trackSeconds)), 64f, TapeB),
            });

            GameObject audioRoot = Track(new GameObject("[Audio]"));
            audioRoot.SetActive(false);
            Director = Child(audioRoot, "AudioDirector").AddComponent<AudioDirector>();
            RoverAudio = Child(audioRoot, "RoverAudio").AddComponent<RoverAudio>();
            Gameplay = Child(audioRoot, "GameplayAudio").AddComponent<GameplayAudio>();
            Ui = Child(audioRoot, "UiAudio").AddComponent<UiAudio>();
            Friends = Child(audioRoot, "FriendAudio").AddComponent<FriendAudio>();
            Jump = Child(audioRoot, "JumpAudio").AddComponent<JumpAudio>();
            Radio = Child(audioRoot, "RadioStation").AddComponent<RadioStation>();
            Ambience = Child(audioRoot, "AmbienceBed").AddComponent<AmbienceBed>();
            Canyon = Child(audioRoot, "CanyonAmbience").AddComponent<CanyonAmbience>();
            Canyon.Wire(Load<CanyonAudioTuning>("CanyonAudioTuning.asset"));
            RoverAudio.Wire(Load<RoverAudioTuning>("RoverAudioTuning.asset"));
            Gameplay.Wire(Load<GameplayAudioTuning>("GameplayAudioTuning.asset"));
            Ui.Wire(Load<UiAudioTuning>("UiAudioTuning.asset"));
            Friends.Wire(Load<FriendAudioTuning>("FriendAudioTuning.asset"));
            Jump.Wire(Load<JumpAudioTuning>("JumpAudioTuning.asset"));
            Radio.Wire(Load<RadioTuning>("RadioTuning.asset"), playlist, tapes);
            Director.Wire(Load<AudioLibrary>("AudioLibrary.asset"), Load<AudioMixTuning>("AudioMixTuning.asset"),
                RoverAudio, Jump, Gameplay, Friends, Ui, Radio, Ambience, Canyon);
            audioRoot.SetActive(true);

            Bootstrap = Track(BootstrapHarness.Create(controls, Rover, Director));
        }

        public FakeRoverSystem Rover { get; }

        public AudioDirector Director { get; }

        public RoverAudio RoverAudio { get; }

        public GameplayAudio Gameplay { get; }

        public UiAudio Ui { get; }

        public FriendAudio Friends { get; }

        public JumpAudio Jump { get; }

        public RadioStation Radio { get; }

        public AmbienceBed Ambience { get; }

        public CanyonAmbience Canyon { get; }

        public GameBootstrap Bootstrap { get; }

        public EventBus Events => Bootstrap.Context.Events;

        public FakeRadioProgram Program => Rover.Program;

        /// <summary>Publishes <see cref="RadioProgramChanged"/> after the test changed <see cref="Program"/>.</summary>
        public void ProgramChanged()
        {
            Events.Publish(new RadioProgramChanged());
        }

        /// <summary>Publishes <see cref="RoverAwoke"/> (07 starts waking: the radio and motor come on).</summary>
        public void Wake(bool byPlayer)
        {
            Events.Publish(new RoverAwoke(Rover.Position, byPlayer));
        }

        /// <summary>True if a director voice whose clip name starts with <paramref name="clipPrefix"/> is
        /// playing.</summary>
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

        /// <summary>The radio's loudest music deck (the live one, except mid-change).</summary>
        public AudioSource LoudestDeck()
        {
            AudioSource loudest = Radio.GetDeck(0);
            for (int i = 1; i < RadioDeckMixer.DeckCount; i++)
            {
                if (Radio.GetDeck(i).volume > loudest.volume)
                {
                    loudest = Radio.GetDeck(i);
                }
            }

            return loudest;
        }

        /// <summary>True while any of the radio's music decks plays (even silently).</summary>
        public bool AnyDeckPlaying()
        {
            for (int i = 0; i < RadioDeckMixer.DeckCount; i++)
            {
                if (Radio.GetDeck(i).isPlaying)
                {
                    return true;
                }
            }

            return false;
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

        private static AudioClip Tone(string name, float frequency, int seconds)
        {
            int count = seconds * SampleRate;
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
