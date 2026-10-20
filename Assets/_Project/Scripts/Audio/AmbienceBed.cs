using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The three 60 s seamless lunar ambience beds (<c>ambience_earthlit_night</c>, <c>ambience_sunlit_day</c>,
    /// <c>ambience_eclipse</c>) on the Ambience bus and the three sky transition one-shots: the eclipse's diamond-ring
    /// swell when the Sun breaks out behind Earth (<see cref="OpeningMilestoneReached"/> FirstCharge, opening v2 B6;
    /// never on a restore), the dawn sweep on an ordinary sunrise and the nightfall settle on
    /// <see cref="SkyPhaseChanged"/>. Beds follow the sky phase with slow eased crossfades (4-8 s) and scale with the
    /// <see cref="Soundscape"/>. The first dusk (<see cref="OpeningDuskBegan"/>) shifts the day toward evening over
    /// about 20 s, part of the night bed under the day's, with a soft low swell on D (a restored dusk starts there
    /// silently), until night or the next dawn takes over. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbienceBed : MonoBehaviour
    {
        private readonly EasedValue _fade = new EasedValue(0f);
        private readonly EasedValue _nightWeight = new EasedValue(1f);
        private readonly EasedValue _dayWeight = new EasedValue(0f);
        private readonly EasedValue _eclipseWeight = new EasedValue(0f);
        private AudioDirector _director;
        private Soundscape _soundscape;
        private AudioSource _nightSource;
        private AudioSource _daySource;
        private AudioSource _eclipseSource;
        private CueHandle _diamondRing;
        private CueHandle _dawnSweep;
        private CueHandle _nightfallSettle;
        private CueHandle _duskSwell;
        private IDisposable _duskSubscription;
        private bool _dusk;
        private float _duskNightShare;
        private float _duskTimeConstant;
        private float _blendTime;
        private IDisposable _skySubscription;
        private IDisposable _milestoneSubscription;
        private SkyPhase _phase = SkyPhase.Earthlit;
        private float _fadeTime;
        private float _crossfadeTime = 6f;
        private float _nightCueVolume;
        private float _dayCueVolume;
        private float _eclipseCueVolume;
        private float _targetNight = 1f;
        private float _targetDay;
        private float _targetEclipse;

        internal AudioSource Source => _nightSource;

        internal AudioSource NightSource => _nightSource;

        internal AudioSource DaySource => _daySource;

        internal AudioSource EclipseSource => _eclipseSource;

        internal void Initialize(GameContext context, AudioDirector director, Soundscape soundscape,
            AudioMixTuning mix)
        {
            CueHandle night = director.Resolve(AudioCueIds.AmbienceEarthlitNight);
            CueHandle day = director.Resolve(AudioCueIds.AmbienceSunlitDay);
            CueHandle eclipse = director.Resolve(AudioCueIds.AmbienceEclipse);
            _diamondRing = director.Resolve(AudioCueIds.SkyDiamondRing);
            _dawnSweep = director.Resolve(AudioCueIds.SkyDawnSweep);
            _nightfallSettle = director.Resolve(AudioCueIds.SkyNightfallSettle);
            _duskSwell = director.Resolve(AudioCueIds.SkyDuskSwell);
            if (!night.IsValid || !day.IsValid || !eclipse.IsValid || !_diamondRing.IsValid || !_dawnSweep.IsValid ||
                !_nightfallSettle.IsValid || !_duskSwell.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _soundscape = soundscape;
            _fadeTime = mix.AmbienceFadeIn;
            _crossfadeTime = Mathf.Clamp(mix.AmbienceCrossfade, 4f, 8f);
            _blendTime = _crossfadeTime;
            _duskNightShare = mix.DuskNightShare;
            // An eased glide covers ~95% of the way in three time constants: "over about 20 s" is the whole shift.
            _duskTimeConstant = mix.DuskShiftSeconds / 3f;
            _nightCueVolume = director.Library.GetCue(night).VolumeMax;
            _dayCueVolume = director.Library.GetCue(day).VolumeMax;
            _eclipseCueVolume = director.Library.GetCue(eclipse).VolumeMax;
            _nightSource = director.CreateLoopSource(transform, "AmbienceLoop", night, 0f);
            _daySource = director.CreateLoopSource(transform, "AmbienceSunlitDay", day, 0f);
            _eclipseSource = director.CreateLoopSource(transform, "AmbienceEclipse", eclipse, 0f);
            _nightSource.Play();

            if (context.TryGet(out ISunlight sunlight))
            {
                _phase = sunlight.Phase;
            }

            _skySubscription = context.Events.Subscribe<SkyPhaseChanged>(OnSkyPhaseChanged);
            _milestoneSubscription = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestoneReached);
            _duskSubscription = context.Events.Subscribe<OpeningDuskBegan>(OnDuskBegan);
        }

        /// <summary>The share of the night bed under the day while the first dusk holds (0 outside it).</summary>
        internal float DuskNightShare => _dusk ? _duskNightShare : 0f;

        /// <summary>
        /// The first dusk: the day shifts toward evening (part of the night bed under it) over about 20 s, with the
        /// soft low swell on D; a restored dusk starts in the evening blend at once, silently.
        /// </summary>
        private void OnDuskBegan(OpeningDuskBegan began)
        {
            _dusk = true;
            if (_phase == SkyPhase.Sunlit || _phase == SkyPhase.Sunset)
            {
                SetDayTargets();
            }

            if (began.Restored)
            {
                _nightWeight.Snap(_targetNight);
                _dayWeight.Snap(_targetDay);
                return;
            }

            _blendTime = _duskTimeConstant;
            _director.Play2D(_duskSwell);
        }

        /// <summary>The day's beds: the sunlit bed alone, or with the night's share under it at the first dusk.</summary>
        private void SetDayTargets()
        {
            _targetNight = DuskNightShare;
            _targetDay = 1f - DuskNightShare;
            _targetEclipse = 0f;
        }

        private void Update()
        {
            if (_nightSource == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            float fade = _fade.Step(1f, dt, _fadeTime);
            float nightW = _nightWeight.Step(_targetNight, dt, _blendTime);
            float dayW = _dayWeight.Step(_targetDay, dt, _blendTime);
            float eclipseW = _eclipseWeight.Step(_targetEclipse, dt, _blendTime);
            float bus = fade * _soundscape.BasinBedGain * _director.Buses.Effective(AudioBus.Ambience);

            Drive(_nightSource, nightW, Mathf.Clamp01(nightW * _nightCueVolume) * bus);
            Drive(_daySource, dayW, Mathf.Clamp01(dayW * _dayCueVolume) * bus);
            Drive(_eclipseSource, eclipseW, Mathf.Clamp01(eclipseW * _eclipseCueVolume) * bus);
        }

        private void OnSkyPhaseChanged(SkyPhaseChanged changed)
        {
            SkyPhase previous = _phase;
            _phase = changed.Phase;
            _blendTime = _crossfadeTime;

            switch (changed.Phase)
            {
                case SkyPhase.Eclipse:
                    _targetNight = 0f;
                    _targetDay = 0f;
                    _targetEclipse = 1f;
                    break;
                case SkyPhase.Earthlit:
                    _targetNight = 1f;
                    _targetDay = 0f;
                    _targetEclipse = 0f;
                    _director.Play2D(_nightfallSettle);
                    break;
                case SkyPhase.Sunrise:
                    _dusk = false;
                    _targetNight = 0f;
                    _targetDay = 1f;
                    _targetEclipse = 0f;
                    // The eclipse's end has its own swell (the FirstCharge milestone): only an ordinary dawn sweeps.
                    if (previous != SkyPhase.Eclipse && changed.EclipseFactor <= 0f)
                    {
                        _director.Play2D(_dawnSweep);
                    }

                    break;
                case SkyPhase.Sunlit:
                case SkyPhase.Sunset:
                    SetDayTargets();
                    break;
            }
        }

        /// <summary>The first charge ends the eclipse: the diamond ring swells as the Sun breaks out.</summary>
        private void OnMilestoneReached(OpeningMilestoneReached reached)
        {
            if (reached.Milestone == OpeningMilestone.FirstCharge && !reached.Restored)
            {
                _director.Play2D(_diamondRing);
            }
        }

        private static void Drive(AudioSource source, float weight, float volume)
        {
            source.volume = volume;
            if (weight > 0f && !source.isPlaying)
            {
                source.Play();
            }
            else if (weight <= 0f && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void OnDestroy()
        {
            _skySubscription?.Dispose();
            _skySubscription = null;
            _milestoneSubscription?.Dispose();
            _milestoneSubscription = null;
            _duskSubscription?.Dispose();
            _duskSubscription = null;
        }
    }
}
