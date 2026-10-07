using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// The 72 s seamless lunar ambience loop on the Ambience bus, fading in gently at start. On Quiet Hours, when the
    /// radio leaves the moon to itself, the bed swells a little so the silence is full rather than empty.
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbienceBed : MonoBehaviour
    {
        private readonly EasedValue _fade = new EasedValue(0f);
        private readonly EasedValue _quietLift = new EasedValue(1f);
        private AudioDirector _director;
        private RadioStation _radio;
        private AudioMixTuning _tuning;
        private AudioSource _source;
        private float _cueVolume;

        /// <summary>The Quiet Hours swell currently applied (1 = none; diagnostics and tests).</summary>
        internal float QuietLift => _quietLift.Value;

        internal void Initialize(AudioDirector director, RadioStation radio, AudioMixTuning tuning)
        {
            CueHandle bed = director.Resolve(AudioCueIds.AmbienceBed);
            if (!bed.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _radio = radio;
            _tuning = tuning;
            _cueVolume = director.Library.GetCue(bed).VolumeMax;
            _source = director.CreateLoopSource(transform, "AmbienceLoop", bed, 0f);
            _source.Play();
        }

        private void Update()
        {
            if (_source == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float fade = _fade.Step(1f, dt, _tuning.AmbienceFadeIn);
            float quiet = _radio.Station == RadioChannel.QuietHours ? _tuning.QuietHoursAmbienceGain : 1f;
            float lift = _quietLift.Step(quiet, dt, _tuning.QuietHoursAmbienceEase);
            _source.volume = Mathf.Clamp01(fade * lift * _cueVolume) * _director.Buses.Effective(AudioBus.Ambience);
        }
    }
}
