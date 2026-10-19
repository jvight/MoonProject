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
    /// The radio gets a test playlist of generated tones (short ones change track within seconds).
    /// </summary>
    public sealed class AudioTestRig : IDisposable
    {
        /// <summary>Short tracks: the station changes track within seconds (playlist tests).</summary>
        public const int ShortTrackSeconds = 3;

        /// <summary>Long tracks: no track change (and its crossfade static) during a test.</summary>
        public const int LongTrackSeconds = 30;

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

            GameObject audioRoot = Track(new GameObject("[Audio]"));
            audioRoot.SetActive(false);
            Director = Child(audioRoot, "AudioDirector").AddComponent<AudioDirector>();
            RoverAudio = Child(audioRoot, "RoverAudio").AddComponent<RoverAudio>();
            Gameplay = Child(audioRoot, "GameplayAudio").AddComponent<GameplayAudio>();
            Ui = Child(audioRoot, "UiAudio").AddComponent<UiAudio>();
            Radio = Child(audioRoot, "RadioStation").AddComponent<RadioStation>();
            TitleMusicLibrary = Track(Object.Instantiate(Load<TitleMusicLibrary>("TitleMusicLibrary.asset")));
            TitleMusic = Child(audioRoot, "TitleMusic").AddComponent<TitleMusic>();
            TitleMusic.Wire(TitleMusicLibrary);
            FeelTuning = Track(ScriptableObject.CreateInstance<FeelAudioTuning>());
            Feel = Child(audioRoot, "FeelAudio").AddComponent<FeelAudio>();
            Feel.Wire(FeelTuning);
            Chirps = Child(audioRoot, "RoverChirpAudio").AddComponent<RoverChirpAudio>();
            Chirps.Wire(FeelTuning, Load<ChirpBank>("ChirpBank.asset"));
            Ambience = Child(audioRoot, "AmbienceBed").AddComponent<AmbienceBed>();
            Soundscape = Child(audioRoot, "Soundscape").AddComponent<Soundscape>();
            SmallSounds = Child(audioRoot, "RoverSmallSounds").AddComponent<RoverSmallSounds>();
            var soundscapeTuning = Load<SoundscapeTuning>("SoundscapeTuning.asset");
            Soundscape.Wire(soundscapeTuning);
            SmallSounds.Wire(soundscapeTuning);
            Salvage = Child(audioRoot, "SalvageAudio").AddComponent<SalvageAudio>();
            Salvage.Wire(Load<SalvageAudioTuning>("SalvageAudioTuning.asset"));
            Battery = Child(audioRoot, "BatteryAudio").AddComponent<BatteryAudio>();
            Call = Child(audioRoot, "CallCarrier").AddComponent<CallCarrier>();
            Call.Wire(soundscapeTuning);
            RoverAudio.Wire(Load<RoverAudioTuning>("RoverAudioTuning.asset"));
            Gameplay.Wire(Load<GameplayAudioTuning>("GameplayAudioTuning.asset"));
            Ui.Wire(Load<UiAudioTuning>("UiAudioTuning.asset"));
            Radio.Wire(Load<RadioTuning>("RadioTuning.asset"), playlist);
            Director.Wire(Load<AudioLibrary>("AudioLibrary.asset"), Load<AudioMixTuning>("AudioMixTuning.asset"),
                RoverAudio, Gameplay, Ui, Radio, Ambience, Soundscape, SmallSounds, Salvage);
            Director.WireTitleMusic(TitleMusic);
            Director.WireBattery(Battery);
            Director.WireCall(Call);
            Director.WireFeel(Feel, Chirps);
            audioRoot.SetActive(true);

            Bootstrap = Track(BootstrapHarness.Create(controls, Rover, Director));
        }

        public FakeRoverSystem Rover { get; }

        public AudioDirector Director { get; }

        public RoverAudio RoverAudio { get; }

        public GameplayAudio Gameplay { get; }

        public UiAudio Ui { get; }


        public BatteryAudio Battery { get; }

        public CallCarrier Call { get; }

        public RadioStation Radio { get; }

        public TitleMusic TitleMusic { get; }

        public TitleMusicLibrary TitleMusicLibrary { get; }

        public FeelAudioTuning FeelTuning { get; }

        public FeelAudio Feel { get; }

        public RoverChirpAudio Chirps { get; }

        public AmbienceBed Ambience { get; }

        public Soundscape Soundscape { get; }

        public RoverSmallSounds SmallSounds { get; }

        public SalvageAudio Salvage { get; }

        public GameBootstrap Bootstrap { get; }

        public EventBus Events => Bootstrap.Context.Events;

        /// <summary>Publishes <see cref="OpeningMilestoneReached"/> (live, or a save's restore).</summary>
        public void Milestone(OpeningMilestone milestone, bool restored = false)
        {
            Events.Publish(new OpeningMilestoneReached(milestone, restored));
        }

        /// <summary>Publishes <see cref="RoverAwoke"/> (07 starts waking: the radio and motor come on).</summary>
        public void Wake(bool byPlayer)
        {
            Events.Publish(new RoverAwoke(Rover.Position, byPlayer));
        }

        /// <summary>Creates and initialises <see cref="WakeAudio"/> under the rig's audio root.</summary>
        public WakeAudio CreateWakeAudio()
        {
            var wake = Child(Director.transform.parent.gameObject, "WakeAudio").AddComponent<WakeAudio>();
            wake.Wire(Director, Load<RoverAudioTuning>("RoverAudioTuning.asset"));
            wake.Initialize(Bootstrap.Context);
            return wake;
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

        /// <summary>A builder-made tuning asset from Assets/_Project/Data/Audio (read only).</summary>
        public static T LoadTuning<T>(string file) where T : ScriptableObject
        {
            return Load<T>(file);
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
