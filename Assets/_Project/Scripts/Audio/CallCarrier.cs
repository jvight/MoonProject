using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The call heard from minute 1 (docs/design/opening-v3-purpose.md §2): Lumen's sign as a faint loop from the light
    /// beside Earth (<see cref="ICallSource.Direction"/>, placed just off 07 that way so it pans from the sky point),
    /// swelling as 07's lens faces it, breaking up and hissing while <see cref="CallState.Broken"/>, a little static
    /// while Locked, clean when Clear, and the call-and-response with relay.1 once Answered
    /// (<see cref="CallStateChanged"/>). Each newly decoded character of the sender's name plays one soft note from the
    /// same point, climbing to D as the name completes (<see cref="CallDecoded"/>); a restored state or name stays
    /// silent. It registers <see cref="ICallSignal"/>: the sign's keying read off the playing loop's own playhead
    /// (<see cref="CallKeying"/>) times the dropouts, so the light beside Earth keys in phase with what is heard. Logic
    /// lives in <see cref="CallCarrierModel"/>. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CallCarrier : MonoBehaviour, ICallSignal
    {
        private const int SubscriptionCount = 2;
        private const uint Seed = 0x0CA11u;

        [Tooltip("Assets/_Project/Data/Audio/SoundscapeTuning.asset; the call is its Call block.")]
        [SerializeField] private SoundscapeTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private ICallSource _call;
        private IRoverRig _rig;
        private IRoverState _rover;
        private CallCarrierSettings _settings;
        private CallCarrierModel _model;
        private CueHandle _decoded;
        private AudioSource _carrier;
        private AudioSource _answered;
        private AudioSource _static;
        private float _carrierCueVolume;
        private float _answeredCueVolume;
        private float _staticCueVolume;
        private int _decodeNotes;

        /// <summary>The carrier model (tests and tooling).</summary>
        internal CallCarrierModel Model => _model;

        internal AudioSource CarrierSource => _carrier;

        internal AudioSource AnsweredSource => _answered;

        internal AudioSource StaticSource => _static;

        /// <inheritdoc />
        /// <remarks>Sampled once per frame (<see cref="KeyingPlayhead"/>), so every reader in a frame agrees.</remarks>
        public float Keying { get; private set; }

        /// <summary>
        /// Seconds into the playing loop where this frame's <see cref="Keying"/> was read: the source's sample position,
        /// taken once per frame because the audio thread moves the live playhead between any two reads.
        /// </summary>
        internal float KeyingPlayhead { get; private set; }

        internal void Wire(SoundscapeTuning tuning)
        {
            _tuning = tuning;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(CallCarrier)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CueHandle carrier = director.Resolve(AudioCueIds.CallCarrier);
            CueHandle answered = director.Resolve(AudioCueIds.CallAnswered);
            CueHandle hiss = director.Resolve(AudioCueIds.RadioCarrier);
            _decoded = director.Resolve(AudioCueIds.CallDecoded);
            if (!(carrier.IsValid && answered.IsValid && hiss.IsValid && _decoded.IsValid))
            {
                enabled = false;
                return;
            }

            _director = director;
            _settings = _tuning.Call;
            _call = context.Get<ICallSource>();
            _rig = context.Get<IRoverRig>();
            _rover = context.Get<IRoverState>();
            _model = new CallCarrierModel(_settings, Seed);
            _model.SetState(_call.State);
            _carrierCueVolume = director.Library.GetCue(carrier).VolumeMax;
            _answeredCueVolume = director.Library.GetCue(answered).VolumeMax;
            _staticCueVolume = director.Library.GetCue(hiss).VolumeMax;
            _decodeNotes = director.Library.GetCue(_decoded).ClipCount;
            _carrier = director.CreateLoopSource(transform, "CallCarrier", carrier, _settings.SpatialBlend);
            _answered = director.CreateLoopSource(transform, "CallAnswered", answered, _settings.SpatialBlend);
            _static = director.CreateLoopSource(transform, "CallStatic", hiss, 0f);
            _carrier.Play();
            _answered.Play();
            _static.Play();
            _subscriptions[0] = context.Events.Subscribe<CallStateChanged>(OnCallStateChanged);
            _subscriptions[1] = context.Events.Subscribe<CallDecoded>(OnCallDecoded);
            context.Register<ICallSignal>(this);
        }

        private void Update()
        {
            if (_model == null)
            {
                return;
            }

            transform.position = SkyPoint;
            float facing = Vector3.Dot(_rig.ScanOrigin.forward, _call.Direction);
            _model.Step(Time.deltaTime, facing);
            float world = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            _carrier.volume = _model.Answered ? 0f : _model.Gain * _carrierCueVolume * world;
            _answered.volume = _model.Answered ? _model.Gain * _answeredCueVolume * world : 0f;
            _carrier.pitch = _model.Pitch;
            AudioSource playing = _model.Answered ? _answered : _carrier;
            KeyingPlayhead = playing.timeSamples / (float)playing.clip.frequency;
            Keying = CallKeying.Envelope(KeyingPlayhead) * _model.Cut;
            _static.volume = _model.Static * _staticCueVolume * _director.Buses.Effective(AudioBus.Music) *
                             _director.WorldGain;
        }

        /// <summary>Just off 07 toward the light, inside the rolloff's near distance: full level, panned to the sky.
        /// </summary>
        private Vector3 SkyPoint => _rover.Position + _call.Direction * _settings.PlacementDistance;

        private void OnCallStateChanged(CallStateChanged changed)
        {
            _model.SetState(changed.State);
        }

        /// <summary>A character newly decoded: one soft note from the light, a step up toward D.</summary>
        private void OnCallDecoded(CallDecoded decoded)
        {
            if (decoded.Restored || decoded.CharactersDecoded <= 0)
            {
                return;
            }

            int step = CallCarrierModel.DecodeStep(decoded.CharactersDecoded, decoded.CharactersTotal, _decodeNotes);
            _director.PlayVariantAt(_decoded, step, SkyPoint, _settings.DecodedVolume);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
