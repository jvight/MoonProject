using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The 72 s seamless lunar ambience loop on the Ambience bus, fading in gently at start. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbienceBed : MonoBehaviour
    {
        private AudioDirector _director;
        private AudioSource _source;
        private EasedValue _fade;
        private float _fadeTime;
        private float _cueVolume;

        internal void Initialize(AudioDirector director, float fadeInSeconds)
        {
            CueHandle bed = director.Resolve(AudioCueIds.AmbienceBed);
            if (!bed.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _fadeTime = fadeInSeconds;
            _fade = new EasedValue(0f);
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
            _source.volume = fade * _cueVolume * _director.Buses.Effective(AudioBus.Ambience);
        }
    }
}
