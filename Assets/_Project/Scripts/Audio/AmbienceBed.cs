using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// The 72 s seamless lunar ambience loop on the Ambience bus, fading in gently at start. Its level follows the
    /// <see cref="Soundscape"/>: it recedes far from home and inside Whispering Canyon, swells a little on Quiet Hours
    /// and pulls back when 07 is still. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbienceBed : MonoBehaviour
    {
        private readonly EasedValue _fade = new EasedValue(0f);
        private AudioDirector _director;
        private Soundscape _soundscape;
        private AudioSource _source;
        private float _fadeTime;
        private float _cueVolume;

        internal AudioSource Source => _source;

        internal void Initialize(AudioDirector director, Soundscape soundscape, float fadeInSeconds)
        {
            CueHandle bed = director.Resolve(AudioCueIds.AmbienceBed);
            if (!bed.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _soundscape = soundscape;
            _fadeTime = fadeInSeconds;
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

            float fade = _fade.Step(1f, Time.unscaledDeltaTime, _fadeTime);
            float level = fade * _soundscape.BasinBedGain * _cueVolume;
            _source.volume = Mathf.Clamp01(level) * _director.Buses.Effective(AudioBus.Ambience);
        }
    }
}
