using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The sonar: ping rhythm, the ring that rolls over the ground, how relics answer, the light pillars they leave
    /// on the horizon and how 07 keeps glancing at them. Created by the Gameplay/Tuning builder; runtime code only
    /// reads it.
    /// </summary>
    public sealed class SonarTuning : ScriptableObject
    {
        [Header("Ping")]
        [Tooltip("Seconds between pings: calm, never a spam button.")]
        [Range(0.5f, 10f)] [SerializeField] private float _cooldown = 2.5f;

        [Tooltip("A press this many seconds before the cooldown ends is remembered and fires when ready.")]
        [Range(0f, 3f)] [SerializeField] private float _inputBuffer = 0.6f;

        [Tooltip("Metres the ping reaches; relics farther away stay silent.")]
        [Range(10f, 500f)] [SerializeField] private float _range = 140f;

        [Header("Ring")]
        [Tooltip("Seconds the ring takes to reach full range (it slows as it spreads: ease-out).")]
        [Range(0.5f, 15f)] [SerializeField] private float _ringDuration = 5f;

        [Tooltip("Ring band width (m) at the start and at full range: a thin, calm wave.")]
        [SerializeField] private Vector2 _ringWidth = new Vector2(0.6f, 2.8f);

        [Tooltip("Seconds the ring takes to fade in.")]
        [Range(0f, 1f)] [SerializeField] private float _ringFadeIn = 0.15f;

        [Tooltip("Height (m) of the ring above the surface.")]
        [Range(0f, 1f)] [SerializeField] private float _ringLift = 0.25f;

        [Tooltip("Ring segments (more hug dunes better at full range).")]
        [Range(16, 512)] [SerializeField] private int _ringSegments = 192;

        [Tooltip("Ring brightness at the start. Keep it at or below 1 so the ring stays cyan instead of blooming white.")]
        [Range(0f, 4f)] [SerializeField] private float _ringIntensity = 0.85f;

        [Tooltip("How the ring fades as it travels: brightness = (1 - progress) ^ this. Above 1 fades early and long.")]
        [Range(0.5f, 4f)] [SerializeField] private float _ringFadeCurve = 1.6f;

        [Tooltip("Rings that can be on screen at once.")]
        [Range(1, 4)] [SerializeField] private int _ringPoolSize = 2;

        [Header("Answers")]
        [Tooltip("Seconds after the ring touches a relic before it answers.")]
        [Range(0f, 2f)] [SerializeField] private float _answerLag = 0.3f;

        [Tooltip("Pillar brightness of an answer right next to 07 (closer answers glow brighter).")]
        [Range(0f, 10f)] [SerializeField] private float _nearBrightness = 1.6f;

        [Tooltip("Pillar brightness of an answer at the edge of the range.")]
        [Range(0f, 10f)] [SerializeField] private float _farBrightness = 1f;

        [Header("Markers")]
        [Tooltip("Seconds an answer's light pillar stays on the horizon.")]
        [Range(1f, 120f)] [SerializeField] private float _pillarLifetime = 20f;

        [Tooltip("Seconds the pillar takes to fade in.")]
        [Range(0.05f, 6f)] [SerializeField] private float _pillarRise = 1.5f;

        [Tooltip("Seconds over which the pillar fades out at the end of its life.")]
        [Range(0.5f, 30f)] [SerializeField] private float _pillarFade = 8f;

        [Tooltip("How deeply a standing pillar breathes (0 = steady, 0.5 = dims to half).")]
        [Range(0f, 0.8f)] [SerializeField] private float _pillarBreathDepth = 0.25f;

        [Tooltip("Seconds per breath of a standing pillar.")]
        [Range(0.5f, 15f)] [SerializeField] private float _pillarBreathPeriod = 3.5f;

        [Tooltip("Pillar height (m): tall enough to read over dunes from far away.")]
        [Range(2f, 100f)] [SerializeField] private float _pillarHeight = 32f;

        [Tooltip("Pillar radius (m).")]
        [Range(0.1f, 3f)] [SerializeField] private float _pillarRadius = 0.6f;

        [Tooltip("Ground ring radius (m) around an answering site, and its band width.")]
        [SerializeField] private Vector2 _siteRing = new Vector2(1.9f, 0.6f);

        [Tooltip("Brightness of the site ring when the answer arrives (scaled like the pillar).")]
        [Range(0f, 10f)] [SerializeField] private float _siteRingPulse = 1.6f;

        [Tooltip("Seconds the site ring's answer pulse takes to settle.")]
        [Range(0.2f, 15f)] [SerializeField] private float _siteRingPulseDuration = 4f;

        [Tooltip("Faint breathing brightness of the ring over a discovered, still-buried site.")]
        [Range(0f, 2f)] [SerializeField] private float _discoveredGlow = 0.22f;

        [Tooltip("Seconds per breath of a discovered site's ring.")]
        [Range(0.5f, 15f)] [SerializeField] private float _breathPeriod = 4f;

        [Tooltip("Segments of a site ring.")]
        [Range(8, 128)] [SerializeField] private int _siteRingSegments = 40;

        [Header("07's attention")]
        [Tooltip("Seconds 07 looks toward the nearest answer right after it arrives.")]
        [Range(0f, 10f)] [SerializeField] private float _answerGaze = 2.5f;

        [Tooltip("While pillars stand, 07 glances at the nearest one every this many seconds...")]
        [Range(1f, 60f)] [SerializeField] private float _glanceInterval = 6f;

        [Tooltip("...for this many seconds.")]
        [Range(0.2f, 10f)] [SerializeField] private float _glanceDuration = 1.6f;

        public float Cooldown => _cooldown;
        public float InputBuffer => _inputBuffer;
        public float Range => _range;
        public float RingDuration => _ringDuration;
        public Vector2 RingWidth => _ringWidth;
        public float RingFadeIn => _ringFadeIn;
        public float RingLift => _ringLift;
        public int RingSegments => _ringSegments;
        public float RingIntensity => _ringIntensity;
        public float RingFadeCurve => _ringFadeCurve;
        public int RingPoolSize => _ringPoolSize;
        public float AnswerLag => _answerLag;
        public float NearBrightness => _nearBrightness;
        public float FarBrightness => _farBrightness;
        public float PillarLifetime => _pillarLifetime;
        public float PillarRise => _pillarRise;
        public float PillarFade => _pillarFade;
        public float PillarBreathDepth => _pillarBreathDepth;
        public float PillarBreathPeriod => _pillarBreathPeriod;
        public float PillarHeight => _pillarHeight;
        public float PillarRadius => _pillarRadius;
        public Vector2 SiteRing => _siteRing;
        public float SiteRingPulse => _siteRingPulse;
        public float SiteRingPulseDuration => _siteRingPulseDuration;
        public float DiscoveredGlow => _discoveredGlow;
        public float BreathPeriod => _breathPeriod;
        public int SiteRingSegments => _siteRingSegments;
        public float AnswerGaze => _answerGaze;
        public float GlanceInterval => _glanceInterval;
        public float GlanceDuration => _glanceDuration;

        /// <summary>Answer brightness for a relic <paramref name="distance"/> metres away: closer is brighter.</summary>
        public float BrightnessAt(float distance)
        {
            return Mathf.Lerp(_nearBrightness, _farBrightness, Mathf.Clamp01(distance / _range));
        }
    }
}
