using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The friend machines' voices. Every friend in the <see cref="IFriendRoster"/> gets a 3D voice that follows it,
    /// built from cues named after it (<c>&lt;id&gt;_&lt;part&gt;</c>). Only <c>&lt;id&gt;_broken</c> (its answer to a
    /// ping) is required; every other part is optional and simply absent when not rendered: mood chirps (curious,
    /// happy, sleepy, greeting, excited, found), a rotor loop following its effort (Tilly's rotors, Bell's clockwork
    /// legs), foot taps from the distance it walks, a doze loop while napping and a wake as it stirs, a crackle when
    /// the radio changes station, and music-box jingles that replace the happy chirp at repair and the greeting at
    /// homecoming. Shared: the repair stitching and boot (logic in <see cref="FriendVoiceModel"/>), the amber part
    /// tone and the spotter ping. Bell's own moments arrive as <see cref="BellCued"/>: the tape slotting in (which
    /// ends her stitching instead of a boot), the needle sweep, foot taps to the music, her happy crackle at a new
    /// relic and the dial's detent click. Events: <see cref="FriendAnswered"/>, <see cref="FriendPartCollected"/>,
    /// <see cref="FriendRepaired"/>, <see cref="FriendGreeted"/>, <see cref="FriendSpotted"/>,
    /// <see cref="RelicDeposited"/> (excited, for friends at home other than Bell). Game-time loops duck while paused
    /// through the director's world gain. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FriendAudio : MonoBehaviour
    {
        private const string CompletePartLabel = "complete";
        private const int MoodCount = 7;
        private const int SubscriptionCount = 7;

        // BellCued carries no friend id: it is always Bell's (her roster id).
        private const string BellId = "bell";

        [Tooltip("Assets/_Project/Data/Audio/FriendAudioTuning.asset.")]
        [SerializeField] private FriendAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private IRoverState _rover;
        private Voice[] _voices = Array.Empty<Voice>();
        private CueHandle _sharedBoot;
        private CueHandle _part;
        private CueHandle _spotPing;
        private CueHandle _tapeSlot;
        private CueHandle _needleSweep;
        private CueHandle _dialClick;
        private Voice _bell;
        private int _completePart;

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
            _sharedBoot = director.Resolve(AudioCueIds.FriendBoot);
            _part = director.Resolve(AudioCueIds.FriendPart);
            _spotPing = director.Resolve(AudioCueIds.FriendSpotPing);
            _tapeSlot = director.Resolve(AudioCueIds.BellTapeSlot);
            _needleSweep = director.Resolve(AudioCueIds.BellNeedleSweep);
            _dialClick = director.Resolve(AudioCueIds.RadioDialClick);
            if (!stitch.IsValid || !_sharedBoot.IsValid || !_part.IsValid || !_spotPing.IsValid || !_tapeSlot.IsValid ||
                !_needleSweep.IsValid || !_dialClick.IsValid || !TryFindCompletePart(out _completePart))
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

            _bell = FindVoice(BellId);
            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<FriendAnswered>(OnFriendAnswered);
            _subscriptions[1] = events.Subscribe<FriendPartCollected>(OnPartCollected);
            _subscriptions[2] = events.Subscribe<FriendRepaired>(OnRepaired);
            _subscriptions[3] = events.Subscribe<FriendGreeted>(OnGreeted);
            _subscriptions[4] = events.Subscribe<FriendSpotted>(OnSpotted);
            _subscriptions[5] = events.Subscribe<RelicDeposited>(OnRelicDeposited);
            _subscriptions[6] = events.Subscribe<BellCued>(OnBellCued);
        }

        internal void Wire(FriendAudioTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>Test and diagnostics access: the following voice of friend <paramref name="index"/>.</summary>
        internal AudioSource ChirpSource(int index) => _voices[index].Chirps;

        internal AudioSource RotorSource(int index) => _voices[index].Rotor;

        internal AudioSource StitchSource(int index) => _voices[index].Stitch;

        internal AudioSource DozeSource(int index) => _voices[index].Doze;

        internal AudioSource JingleSource(int index) => _voices[index].Jingles;

        internal AudioSource FeetSource(int index) => _voices[index].Feet;

        /// <summary>The radio changed station: friends with a tune crackle (Bell) voice it, unless still
        /// dormant.</summary>
        internal void OnStationSwitched()
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                Voice voice = _voices[i];
                if (voice.Tune.IsValid && voice.Friend.Activity != FriendActivity.Dormant)
                {
                    _director.PlayOn(voice.Chirps, voice.Tune, 1f);
                }
            }
        }

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
            Voice voice = Find(repaired.FriendId);
            if (voice == null)
            {
                return;
            }

            if (voice.JingleShort.IsValid)
            {
                _director.PlayOn(voice.Jingles, voice.JingleShort, 1f);
            }
            else
            {
                Chirp(voice, FriendMood.Happy);
            }
        }

        private void OnGreeted(FriendGreeted greeted)
        {
            Voice voice = Find(greeted.FriendId);
            if (voice == null)
            {
                return;
            }

            if (voice.JingleFull.IsValid)
            {
                _director.PlayOn(voice.Jingles, voice.JingleFull, 1f);
            }
            else
            {
                Chirp(voice, FriendMood.Greeting);
            }
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

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                // Bell reacts only when she sees it: her crackle comes as BellCued.Crackled.
                if (_voices[i] != _bell && _voices[i].Friend.Activity == FriendActivity.Home)
                {
                    Chirp(_voices[i], FriendMood.Excited);
                }
            }
        }

        private void OnBellCued(BellCued cued)
        {
            Voice bell = Find(BellId);
            if (bell == null)
            {
                return;
            }

            switch (cued.Cue)
            {
                case BellCue.TapeSlotted:
                    bell.Model.FinishStitching();
                    _director.PlayAt(_tapeSlot, cued.Position);
                    break;
                case BellCue.NeedleSwept:
                    _director.PlayAt(_needleSweep, cued.Position);
                    break;
                case BellCue.FootTapped:
                    if (bell.StepCue.IsValid)
                    {
                        _director.PlayOn(bell.Feet, bell.StepCue, 1f);
                    }

                    break;
                case BellCue.Crackled:
                    Chirp(bell, FriendMood.Excited);
                    break;
                case BellCue.DialTurned:
                    _director.Play2D(_dialClick);
                    break;
                default:
                    Debug.LogError($"{nameof(FriendAudio)}: no sound for Bell's cue {cued.Cue}.", this);
                    break;
            }
        }

        private void ChirpFrom(string friendId, FriendMood mood)
        {
            Voice voice = Find(friendId);
            if (voice != null)
            {
                Chirp(voice, mood);
            }
        }

        private void Chirp(Voice voice, FriendMood mood)
        {
            CueHandle cue = voice.Moods[(int)mood];
            if (cue.IsValid)
            {
                _director.PlayOn(voice.Chirps, cue, 1f);
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
                StepVoice(_voices[i], dt, sfx);
            }
        }

        private void StepVoice(Voice voice, float dt, float sfx)
        {
            IFriendState friend = voice.Friend;
            FriendActivity activity = friend.Activity;
            voice.Host.position = friend.Position;
            if (voice.Model.Step(dt, activity, friend.RotorSpeed, out FriendMood mood))
            {
                Chirp(voice, mood);
            }

            if (voice.Model.TakeBoot())
            {
                _director.PlayOn(voice.Chirps, _sharedBoot, 1f);
            }

            if (voice.Rotor != null)
            {
                Drive(voice.Rotor, voice.Model.RotorVolume * voice.RotorCueVolume * sfx, voice.Model.RotorPitch);
            }

            Drive(voice.Stitch, voice.Model.StitchGain * voice.StitchCueVolume * sfx, voice.Model.StitchPitch);
            bool napping = activity == FriendActivity.Napping;
            bool walking = activity != FriendActivity.Dormant && !napping;
            if (voice.StepCue.IsValid &&
                voice.Steps.Step(friend.Position, _tuning.StepStride, _tuning.StepTeleportDistance) && walking)
            {
                _director.PlayOn(voice.Feet, voice.StepCue, 1f);
            }

            if (voice.Doze != null)
            {
                if (napping)
                {
                    voice.DozeFader.FadeIn();
                }
                else
                {
                    voice.DozeFader.FadeOut();
                }

                voice.DozeFader.Step(dt, _tuning.DozeFadeIn, _tuning.DozeFadeOut);
                Drive(voice.Doze, voice.DozeFader.Gain * voice.DozeCueVolume * sfx, 1f);
            }

            if (voice.WasNapping && !napping && activity != FriendActivity.Dormant && voice.Wake.IsValid)
            {
                _director.PlayOn(voice.Chirps, voice.Wake, 1f);
            }

            voice.WasNapping = napping;
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

        private Voice Find(string friendId)
        {
            Voice voice = FindVoice(friendId);
            if (voice == null)
            {
                Debug.LogError($"{nameof(FriendAudio)}: no friend '{friendId}' in the roster.", this);
            }

            return voice;
        }

        private Voice FindVoice(string friendId)
        {
            for (int i = 0; i < _voices.Length; i++)
            {
                if (string.Equals(_voices[i].Friend.Id, friendId, StringComparison.Ordinal))
                {
                    return _voices[i];
                }
            }

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

        private CueHandle Optional(string friendId, string part)
        {
            _director.Library.TryResolve(AudioCueIds.FriendCue(friendId, part), out CueHandle cue);
            return cue;
        }

        private Voice CreateVoice(IFriendState friend, CueHandle stitch, AudioRandom random)
        {
            string id = friend.Id;
            if (!_director.Resolve(AudioCueIds.FriendChirp(id, FriendMood.Broken)).IsValid)
            {
                return null;
            }

            var voice = new Voice(friend, new FriendVoiceModel(_tuning, random));
            for (int m = 0; m < MoodCount; m++)
            {
                _director.Library.TryResolve(AudioCueIds.FriendChirp(id, (FriendMood)m), out voice.Moods[m]);
            }

            voice.Host = new GameObject($"Friend_{id}").transform;
            voice.Host.SetParent(transform, false);
            voice.Host.position = friend.Position;
            voice.Chirps = CreateOneShotSource(voice.Host, "Chirps");
            voice.Stitch = _director.CreateLoopSource(voice.Host, "Stitch", stitch, 1f);
            voice.StitchCueVolume = _director.Library.GetCue(stitch).VolumeMax;
            CueHandle rotor = Optional(id, FriendCueParts.Rotor);
            if (rotor.IsValid)
            {
                voice.Rotor = _director.CreateLoopSource(voice.Host, "Rotor", rotor, 1f);
                voice.RotorCueVolume = _director.Library.GetCue(rotor).VolumeMax;
            }

            CueHandle doze = Optional(id, FriendCueParts.Doze);
            if (doze.IsValid)
            {
                voice.Doze = _director.CreateLoopSource(voice.Host, "Doze", doze, 1f);
                voice.DozeCueVolume = _director.Library.GetCue(doze).VolumeMax;
            }

            voice.StepCue = Optional(id, FriendCueParts.Step);
            voice.Wake = Optional(id, FriendCueParts.Wake);
            voice.Tune = Optional(id, FriendCueParts.Tune);
            voice.JingleShort = Optional(id, FriendCueParts.JingleShort);
            voice.JingleFull = Optional(id, FriendCueParts.JingleFull);
            if (voice.StepCue.IsValid)
            {
                voice.Feet = CreateOneShotSource(voice.Host, "Feet");
            }

            if (voice.JingleShort.IsValid || voice.JingleFull.IsValid)
            {
                // Its own source: a chirp's pitch variance on the shared one would bend a melody already ringing.
                voice.Jingles = CreateOneShotSource(voice.Host, "Jingles");
            }

            return voice;
        }

        private AudioSource CreateOneShotSource(Transform host, string objectName)
        {
            AudioSource source = _director.CreateLoopSource(host, objectName, default, 1f);
            source.loop = false;
            return source;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }

        /// <summary>One friend's sources, cues (invalid = not rendered for it) and sound state.</summary>
        private sealed class Voice
        {
            public Voice(IFriendState friend, FriendVoiceModel model)
            {
                Friend = friend;
                Model = model;
            }

            public IFriendState Friend { get; }

            public FriendVoiceModel Model { get; }

            /// <summary>Per-mood chirps, filled by TryResolve at creation (written in place).</summary>
            public CueHandle[] Moods { get; } = new CueHandle[MoodCount];

            public FootstepCadence Steps { get; } = new FootstepCadence();

            public LoopFader DozeFader { get; } = new LoopFader();

            public Transform Host { get; set; }

            public AudioSource Chirps { get; set; }

            public AudioSource Stitch { get; set; }

            public AudioSource Rotor { get; set; }

            public AudioSource Doze { get; set; }

            /// <summary>Foot taps (only when the friend has a step cue).</summary>
            public AudioSource Feet { get; set; }

            /// <summary>Jingles (only when the friend has one).</summary>
            public AudioSource Jingles { get; set; }

            public float StitchCueVolume { get; set; }

            public float RotorCueVolume { get; set; }

            public float DozeCueVolume { get; set; }

            public CueHandle StepCue { get; set; }

            public CueHandle Wake { get; set; }

            public CueHandle Tune { get; set; }

            public CueHandle JingleShort { get; set; }

            public CueHandle JingleFull { get; set; }

            public bool WasNapping { get; set; }
        }
    }
}
