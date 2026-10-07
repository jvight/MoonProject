using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How gameplay events sound: relic answers by distance, the scrap melody, tether and excavation loops.
    /// Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class GameplayAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.01f;

        [Header("Relic answer")]
        [Tooltip("Distance (m, rover to relic) at and below which the answer is fullest and brightest.")]
        [Min(0f)] [SerializeField] private float _answerNearDistance = 10f;

        [Tooltip("Distance (m) at and beyond which the answer is quietest and darkest.")]
        [Min(1f)] [SerializeField] private float _answerFarDistance = 90f;

        [Tooltip("Volume scale of a near answer.")]
        [Range(0f, 1f)] [SerializeField] private float _answerNearVolume = 1f;

        [Tooltip("Volume scale of a far answer.")]
        [Range(0f, 1f)] [SerializeField] private float _answerFarVolume = 0.55f;

        [Tooltip("Low-pass cutoff (Hz) of a near answer (high = the clip as rendered).")]
        [Range(1000f, 22000f)] [SerializeField] private float _answerNearCutoff = 16000f;

        [Tooltip("Low-pass cutoff (Hz) of a far answer (lower = more distant).")]
        [Range(500f, 8000f)] [SerializeField] private float _answerFarCutoff = 2200f;

        [Tooltip("3D full-volume radius (m) of answers: larger than usual so distant relics stay findable by ear.")]
        [Min(0.1f)] [SerializeField] private float _answerMinDistance = 25f;

        [Header("Scrap melody")]
        [Tooltip("Long chains weave over this many of the highest chime notes once the climb reaches the top.")]
        [Range(2, 8)] [SerializeField] private int _scrapTopWindow = 4;

        [Header("Tether")]
        [Tooltip("Seconds for the tether hum to fade in after attaching.")]
        [Min(0f)] [SerializeField] private float _tetherHumFadeIn = 0.3f;

        [Tooltip("Seconds for the tether hum to fade out after release.")]
        [Min(0f)] [SerializeField] private float _tetherHumFadeOut = 0.5f;

        [Tooltip("Volume scale of the tether hum (times the cue volume).")]
        [Range(0f, 1f)] [SerializeField] private float _tetherHumVolume = 1f;

        [Tooltip("3D amount of the tether hum (it follows the beam emitter on 07's eye).")]
        [Range(0f, 1f)] [SerializeField] private float _tetherHumSpatialBlend = 0.85f;

        [Tooltip("Mass (kg) at or below which the attach pluck is lightest.")]
        [Min(0f)] [SerializeField] private float _tetherLightMass = 1f;

        [Tooltip("Mass (kg) at or above which the attach pluck is fullest.")]
        [Min(0.01f)] [SerializeField] private float _tetherHeavyMass = 25f;

        [Tooltip("Attach pluck volume scale for light objects.")]
        [Range(0f, 1f)] [SerializeField] private float _tetherLightVolume = 0.75f;

        [Tooltip("Attach pluck volume scale for heavy objects.")]
        [Range(0f, 1f)] [SerializeField] private float _tetherHeavyVolume = 1f;

        [Header("Bell's signals")]
        [Tooltip("Spatial blend of the shimmer from a new signal's pillar: mostly flat so a pillar far across the " +
                 "basin is still heard, faintly, with a hint of its direction (0 = flat, 1 = fully 3D).")]
        [Range(0f, 1f)] [SerializeField] private float _signalSpatialBlend = 0.3f;

        [Tooltip("Volume scale of the pillar's shimmer (very soft: a suggestion, never a call).")]
        [Range(0f, 1f)] [SerializeField] private float _signalPickVolume = 0.8f;

        [Header("Excavation")]
        [Tooltip("Seconds for the excavation rumble to swell in.")]
        [Min(0f)] [SerializeField] private float _rumbleFadeIn = 0.8f;

        [Tooltip("Seconds for the excavation rumble to settle away.")]
        [Min(0f)] [SerializeField] private float _rumbleFadeOut = 1f;

        [Tooltip("Volume scale of the excavation rumble (times the cue volume).")]
        [Range(0f, 1f)] [SerializeField] private float _rumbleVolume = 1f;

        public float AnswerNearDistance => _answerNearDistance;
        public float AnswerFarDistance => Mathf.Max(_answerFarDistance, _answerNearDistance + MinSpan);
        public float AnswerNearVolume => _answerNearVolume;
        public float AnswerFarVolume => _answerFarVolume;
        public float AnswerNearCutoff => _answerNearCutoff;
        public float AnswerFarCutoff => _answerFarCutoff;
        public float AnswerMinDistance => _answerMinDistance;
        public int ScrapTopWindow => _scrapTopWindow;
        public float TetherHumFadeIn => _tetherHumFadeIn;
        public float TetherHumFadeOut => _tetherHumFadeOut;
        public float TetherHumVolume => _tetherHumVolume;
        public float TetherHumSpatialBlend => _tetherHumSpatialBlend;
        public float SignalSpatialBlend => _signalSpatialBlend;
        public float SignalPickVolume => _signalPickVolume;
        public float TetherLightMass => _tetherLightMass;
        public float TetherHeavyMass => Mathf.Max(_tetherHeavyMass, _tetherLightMass + MinSpan);
        public float TetherLightVolume => _tetherLightVolume;
        public float TetherHeavyVolume => _tetherHeavyVolume;
        public float RumbleFadeIn => _rumbleFadeIn;
        public float RumbleFadeOut => _rumbleFadeOut;
        public float RumbleVolume => _rumbleVolume;
    }
}
