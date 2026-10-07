using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Whispering Canyon's sound (M3-04, feel pillar 6 "Alone, and at peace"). Inside the corridor the World's canyon
    /// anchors trace (<see cref="CanyonField"/>), a breathy whisper bed rises while the basin's bed recedes and the
    /// radio thins (read by <see cref="AmbienceBed"/> and <see cref="RadioStation"/>); down in the chasm's trough a
    /// deeper, darker bed takes over; and a gentle echo (one AudioReverbZone over the canyon, its level following
    /// "inside") rings on 07's own 3D sounds while the 2D radio, UI and beds bypass it. Everything eases with 07's
    /// position, so driving in and out is a slow change of air, never a switch. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CanyonAmbience : MonoBehaviour
    {
        // Below this (-60 dB) the canyon is out of earshot: the beds stop and the echo switches off.
        private const float Silent = 0.001f;

        private const float MillibelsPerDecibel = 100f;
        private const float DecibelsPerAmplitudeDecade = 20f;
        private const float RoomOff = -10000f;

        // Metres added around the anchors so the listener (the camera, behind 07) is always within the echo zone.
        private const float ZoneMargin = 60f;

        // Moving the echo's level costs a native call: only in steps of a quarter decibel.
        private const float RoomStep = 25f;

        [Tooltip("Assets/_Project/Data/Audio/CanyonAudioTuning.asset.")]
        [SerializeField] private CanyonAudioTuning _tuning;

        private readonly EasedValue _inside = new EasedValue(0f);
        private readonly EasedValue _trough = new EasedValue(0f);
        private AudioDirector _director;
        private IRoverState _rover;
        private CanyonField _field;
        private AudioSource _whisper;
        private AudioSource _troughBed;
        private AudioReverbZone _echo;
        private float _whisperCueVolume;
        private float _troughCueVolume;
        private float _appliedRoom = RoomOff;

        /// <summary>0 outside .. 1 deep in the canyon (eased).</summary>
        public float Inside => _inside.Value;

        /// <summary>0 .. 1 down in the chasm's trough (eased).</summary>
        public float Trough => _trough.Value;

        /// <summary>Level of the basin's ambience bed here: it recedes inside the canyon.</summary>
        public float BasinGain => _field != null ? Mathf.Lerp(1f, _tuning.BasinBedInside, Inside) : 1f;

        /// <summary>Radio music level here: thinner inside the canyon.</summary>
        public float RadioMusicGain => _field != null ? Mathf.Lerp(1f, _tuning.RadioMusicInside, Inside) : 1f;

        internal AudioSource WhisperSource => _whisper;

        internal AudioSource TroughSource => _troughBed;

        internal AudioReverbZone Echo => _echo;

        /// <summary>The radio's low-pass here, from its open <paramref name="cutoffHz"/>: eased geometrically towards
        /// the canyon's thin set as 07 goes in.</summary>
        public float RadioCutoff(float cutoffHz)
        {
            if (_field == null || Inside <= 0f)
            {
                return cutoffHz;
            }

            return cutoffHz * Mathf.Pow(Mathf.Min(1f, _tuning.RadioCutoffInside / cutoffHz), Inside);
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(CanyonAmbience)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CanyonField field = CanyonField.FromAnchors(context.Get<IWorldAnchors>(), out string problem);
            if (field == null)
            {
                Debug.LogError($"{nameof(CanyonAmbience)}: {problem}; Whispering Canyon stays silent.", this);
                enabled = false;
                return;
            }

            CueHandle whisper = director.Resolve(AudioCueIds.CanyonWhisper);
            CueHandle trough = director.Resolve(AudioCueIds.CanyonTrough);
            if (!whisper.IsValid || !trough.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            _whisperCueVolume = director.Library.GetCue(whisper).VolumeMax;
            _troughCueVolume = director.Library.GetCue(trough).VolumeMax;
            _whisper = director.CreateLoopSource(transform, "Whisper", whisper, 0f);
            _troughBed = director.CreateLoopSource(transform, "Trough", trough, 0f);
            _echo = CreateEcho(field);
            _field = field;
        }

        internal void Wire(CanyonAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_field == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            Vector3 position = _rover.Position;
            Ease(_inside, _field.Inside(position, _tuning.HalfWidth, _tuning.Edge, _tuning.Entry), dt);
            Ease(_trough, _field.Trough(position, _tuning.HalfWidth, _tuning.Edge, _tuning.TroughDepthStart,
                _tuning.TroughDepthRange), dt);

            float bus = _director.Buses.Effective(AudioBus.Ambience);
            Drive(_whisper, Inside * (1f - Trough) * _tuning.WhisperGain * _whisperCueVolume * bus);
            Drive(_troughBed, Trough * _tuning.TroughGain * _troughCueVolume * bus);
            UpdateEcho();
        }

        private void Ease(EasedValue value, float target, float dt)
        {
            value.Step(target, dt, _tuning.Ease);
            if (target <= 0f && value.Value < Silent)
            {
                value.Snap(0f);
            }
        }

        private void UpdateEcho()
        {
            bool audible = Inside >= Silent;
            if (_echo.enabled != audible)
            {
                _echo.enabled = audible;
            }

            if (!audible)
            {
                _appliedRoom = RoomOff;
                return;
            }

            float roomDb = _tuning.RoomDb + DecibelsPerAmplitudeDecade * Mathf.Log10(Inside);
            float room = Mathf.Max(RoomOff, roomDb * MillibelsPerDecibel);
            if (Mathf.Abs(room - _appliedRoom) >= RoomStep)
            {
                _echo.room = Mathf.RoundToInt(room);
                _appliedRoom = room;
            }
        }

        private AudioReverbZone CreateEcho(CanyonField field)
        {
            var host = new GameObject("Echo");
            host.transform.SetParent(transform, false);
            host.transform.position = field.Centre;
            AudioReverbZone zone = host.AddComponent<AudioReverbZone>();
            zone.reverbPreset = AudioReverbPreset.User;
            zone.minDistance = field.Extent + ZoneMargin;
            zone.maxDistance = zone.minDistance + ZoneMargin;
            zone.room = Mathf.RoundToInt(RoomOff);
            zone.roomHF = Mathf.RoundToInt(_tuning.RoomHfDb * MillibelsPerDecibel);
            zone.decayTime = _tuning.DecayTime;
            zone.decayHFRatio = _tuning.DecayHfRatio;
            zone.reflections = Mathf.RoundToInt(_tuning.ReflectionsDb * MillibelsPerDecibel);
            zone.reflectionsDelay = _tuning.ReflectionsDelay;
            zone.reverb = Mathf.RoundToInt(_tuning.ReverbDb * MillibelsPerDecibel);
            zone.reverbDelay = _tuning.ReverbDelay;
            zone.diffusion = _tuning.Diffusion;
            zone.density = _tuning.Density;
            zone.enabled = false;
            return zone;
        }

        private static void Drive(AudioSource bed, float volume)
        {
            bed.volume = volume;
            if (volume > 0f && !bed.isPlaying)
            {
                bed.Play();
            }
            else if (volume <= 0f && bed.isPlaying)
            {
                bed.Stop();
            }
        }
    }
}
