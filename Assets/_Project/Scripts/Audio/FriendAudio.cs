using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The friend machines' voices. Every friend in the <see cref="IFriendRoster"/> gets a 3D voice that follows it:
    /// chirps from its own vocabulary (<c>&lt;id&gt;_&lt;mood&gt;</c> cues), a rotor loop following its effort, and the
    /// repair stitching, then the boot jingle as its eye flickers on; the logic is in <see cref="FriendVoiceModel"/>.
    /// The friend events play their moments: <see cref="FriendAnswered"/> (broken chirp),
    /// <see cref="FriendPartCollected"/> (amber part tone), <see cref="FriendRepaired"/> (happy chirp),
    /// <see cref="FriendGreeted"/> (greeting),
    /// <see cref="FriendSpotted"/> ("found it" chirp plus a soft ping at the spot) and <see cref="RelicDeposited"/>
    /// (excited, for friends at home). Game-time loops duck while paused through the director's world gain.
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FriendAudio : MonoBehaviour
    {
        private const string CompletePartLabel = "complete";
        private const int MoodCount = 7;
        private const int SubscriptionCount = 6;

        [Tooltip("Assets/_Project/Data/Audio/FriendAudioTuning.asset.")]
        [SerializeField] private FriendAudioTuning _tuning;

        private AudioDirector _director;
        private IRoverState _rover;
        private Voice[] _voices = Array.Empty<Voice>();
        private CueHandle _boot;
        private CueHandle _part;
        private CueHandle _spotPing;
        private int _completePart;
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];

        public int VoiceCount => _voices.Length;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(FriendAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            IFriendRoster roster = context.Get<IFriendRoster>();
            CueHandle stitch = director.Resolve(AudioCueIds.FriendStitch);
            _boot = director.Resolve(AudioCueIds.FriendBoot);
            _part = director.Resolve(AudioCueIds.FriendPart);
            _spotPing = director.Resolve(AudioCueIds.FriendSpotPing);
            if (!stitch.IsValid || !_boot.IsValid || !_part.IsValid || !_spotPing.IsValid ||
                !TryFindCompletePart(out _completePart))
            {
                enabled = false;
                return;
            }

            var random = new AudioRandom(unchecked((uint)Environment.TickCount) ^ 0x5F3759DFu);
            _voices = new Voice[roster.Count];
            for (int i = 0; i < _voices.Length; i++)
            {
                IFriendState friend = roster.Get(i);
                if (friend == null)
                {
                    Debug.LogError($"{nameof(FriendAudio)}: friend roster entry {i} is null.", this);
                    enabled = false;
                    return;
                }

                _voices[i] = CreateVoice(friend, stitch, random);
                if (_voices[i] == null)
                {
                    enabled = false;
                    return;
                }
            }

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<FriendAnswered>(OnFriendAnswered);
            _subscriptions[1] = events.Subscribe<FriendPartCollected>(OnPartCollected);
            _subscriptions[2] = events.Subscribe<FriendRepaired>(OnRepaired);
            _subscriptions[3] = events.Subscribe<FriendGreeted>(OnGreeted);
            _subscriptions[4] = events.Subscribe<FriendSpotted>(OnSpotted);
            _subscriptions[5] = events.Subscribe<RelicDeposited>(OnRelicDeposited);
        }

        internal void Wire(FriendAudioTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>Test and diagnostics access: the following voice of friend <paramref name="index"/>.</summary>
        internal AudioSource ChirpSource(int index) => _voices[index].Chirps;

        internal AudioSource RotorSource(int index) => _voices[index].Rotor;

        internal AudioSource StitchSource(int index) => _voices[index].Stitch;

        private void OnFriendAnswered(FriendAnswered answered)
        {
            ChirpFrom(answered.FriendId, FriendMood.Broken);
        }

        private void OnPartCollected(FriendPartCollected collected)
        {
            if (Find(collected.FriendId) == null)
            {
                return;
            }

            int variant = collected.Collected >= collected.Total
                ? _completePart
                : Mathf.Clamp(collected.Collected - 1, 0, _completePart - 1);
            _director.PlayVariantAt(_part, variant, _rover.Position);
        }

        private void OnRepaired(FriendRepaired repaired)
        {
            ChirpFrom(repaired.FriendId, FriendMood.Happy);
        }

        private void OnGreeted(FriendGreeted greeted)
        {
            ChirpFrom(greeted.FriendId, FriendMood.Greeting);
        }

        private void OnSpotted(FriendSpotted spotted)
        {
            if (Find(spotted.FriendId) == null)
            {
                return;
            }

            ChirpFrom(spotted.FriendId, FriendMood.Found);
            _director.PlayAt(_spotPing, spotted.Position);
        }

        private void ChirpFrom(string friendId, FriendMood mood)
        {
            Voice voice = Find(friendId);
            if (voice != null)
            {
                Chirp(voice, mood);
            }
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            for (int i = 0; i < _voices.Length; i++)
            {
                Voice voice = _voices[i];
                IFriendState friend = voice.Friend;
                voice.Host.position = friend.Position;
                if (voice.Model.Step(dt, friend.Activity, friend.RotorSpeed, out FriendMood mood))
                {
                    Chirp(voice, mood);
                }

                if (voice.Model.TakeBoot())
                {
                    _director.PlayOn(voice.Chirps, _boot, 1f);
                }

                Drive(voice.Rotor, voice.Model.RotorVolume * voice.RotorCueVolume * sfx, voice.Model.RotorPitch);
                Drive(voice.Stitch, voice.Model.StitchGain * voice.StitchCueVolume * sfx, voice.Model.StitchPitch);
            }
        }

        private static void Drive(AudioSource loop, float volume, float pitch)
        {
            loop.volume = volume;
            loop.pitch = pitch;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
        }

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i].Friend.Activity == FriendActivity.Home)
                {
                    Chirp(_voices[i], FriendMood.Excited);
                }
            }
        }

        private void Chirp(Voice voice, FriendMood mood)
        {
            _director.PlayOn(voice.Chirps, voice.Moods[(int)mood], 1f);
        }

        private Voice Find(string friendId)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                if (string.Equals(_voices[i].Friend.Id, friendId, StringComparison.Ordinal))
                {
                    return _voices[i];
                }
            }

            Debug.LogError($"{nameof(FriendAudio)}: no friend '{friendId}' in the roster.", this);
            return null;
        }

        private bool TryFindCompletePart(out int index)
        {
            AudioCue part = _director.Library.GetCue(_part);
            for (index = 0; index < part.ClipCount; index++)
            {
                if (part.GetVariantLabel(index) == CompletePartLabel)
                {
                    return index > 0;
                }
            }

            Debug.LogError($"{nameof(FriendAudio)}: cue '{AudioCueIds.FriendPart}' has no '{CompletePartLabel}' " +
                           "variant after its steps.", this);
            return false;
        }

        private Voice CreateVoice(IFriendState friend, CueHandle stitch, AudioRandom random)
        {
            var moods = new CueHandle[MoodCount];
            for (int m = 0; m < MoodCount; m++)
            {
                moods[m] = _director.Resolve(AudioCueIds.FriendChirp(friend.Id, (FriendMood)m));
                if (!moods[m].IsValid)
                {
                    return null;
                }
            }

            CueHandle rotor = _director.Resolve(AudioCueIds.FriendRotor(friend.Id));
            if (!rotor.IsValid)
            {
                return null;
            }

            Transform host = new GameObject($"Friend_{friend.Id}").transform;
            host.SetParent(transform, false);
            host.position = friend.Position;
            AudioSource chirps = _director.CreateLoopSource(host, "Chirps", default, 1f);
            chirps.loop = false;
            return new Voice(friend, host, moods, chirps,
                _director.CreateLoopSource(host, "Rotor", rotor, 1f),
                _director.CreateLoopSource(host, "Stitch", stitch, 1f),
                _director.Library.GetCue(rotor).VolumeMax, _director.Library.GetCue(stitch).VolumeMax,
                new FriendVoiceModel(_tuning, random));
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }

        /// <summary>One friend's sources, cues and sound state.</summary>
        private sealed class Voice
        {
            public Voice(IFriendState friend, Transform host, CueHandle[] moods, AudioSource chirps, AudioSource rotor,
                AudioSource stitch, float rotorCueVolume, float stitchCueVolume, FriendVoiceModel model)
            {
                Friend = friend;
                Host = host;
                Moods = moods;
                Chirps = chirps;
                Rotor = rotor;
                Stitch = stitch;
                RotorCueVolume = rotorCueVolume;
                StitchCueVolume = stitchCueVolume;
                Model = model;
            }

            public IFriendState Friend { get; }

            public Transform Host { get; }

            public CueHandle[] Moods { get; }

            public AudioSource Chirps { get; }

            public AudioSource Rotor { get; }

            public AudioSource Stitch { get; }

            public float RotorCueVolume { get; }

            public float StitchCueVolume { get; }

            public FriendVoiceModel Model { get; }
        }
    }
}
