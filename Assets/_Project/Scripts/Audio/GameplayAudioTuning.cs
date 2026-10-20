using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the base sounds: the weld loop's fades, the power-on swell and the lamps warming on at dusk.
    /// Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class GameplayAudioTuning : ScriptableObject
    {
        [Header("Build mode")]
        [Tooltip("Seconds for the weld loop to come in when the beam starts welding.")]
        [Range(0.01f, 1f)] [SerializeField] private float _weldFadeIn = 0.08f;

        [Tooltip("Seconds for the weld loop to settle away when the beam stops.")]
        [Range(0.01f, 1f)] [SerializeField] private float _weldFadeOut = 0.12f;

        [Header("Power on (opening v2 B8)")]
        [Tooltip("Volume scale of a structure's power-on: the relay thunk and the transformer swell onto its lamp.")]
        [Range(0f, 1f)] [SerializeField] private float _powerOnVolume = 0.85f;

        [Tooltip("Seconds after a structure is built during which its first power is silent: the build's own " +
                 "completion already lit its lamp, so the swell would only pile on.")]
        [Range(0f, 5f)] [SerializeField] private float _powerOnBuildGrace = 1.5f;

        [Tooltip("Seconds between two power-on swells: when a cable brings several structures up at once, one swell " +
                 "speaks for them.")]
        [Range(0f, 5f)] [SerializeField] private float _powerOnMinInterval = 1.2f;

        [Header("Base lamps at dusk (BaseLampLit)")]
        [Tooltip("Volume scale of each lamp's relay, filament tick and hum as it warms on (soft: the dusk is the " +
                 "moment).")]
        [Range(0f, 1f)] [SerializeField] private float _baseLampVolume = 0.75f;

        [Header("Antenna mast (MastSegmentRaised)")]
        [Tooltip("Volume scale of each segment's telescoping clunk as it locks home.")]
        [Range(0f, 1f)] [SerializeField] private float _mastClunkVolume = 0.85f;

        [Tooltip("Volume scale of the raised mast's settle, after its top segment locks.")]
        [Range(0f, 1f)] [SerializeField] private float _mastSettleVolume = 0.75f;

        [Header("Tow (TowAttached, ITowLine, TowReleased)")]
        [Tooltip("Volume scale of the winch spooling out as the hook attaches.")]
        [Range(0f, 1f)] [SerializeField] private float _towSpoolVolume = 0.8f;

        [Tooltip("Volume scale of the cable's creak at full strain (kept under 07's drive).")]
        [Range(0f, 1f)] [SerializeField] private float _towStrainVolume = 0.6f;

        [Tooltip("Strain (0..1) at or below which the cable is slack and silent.")]
        [Range(0f, 0.5f)] [SerializeField] private float _towSlackStrain = 0.08f;

        [Tooltip("The creak's pitch just taut.")]
        [Range(0.5f, 1.5f)] [SerializeField] private float _towStrainLowPitch = 0.85f;

        [Tooltip("The creak's pitch stretched to the limit.")]
        [Range(0.5f, 2f)] [SerializeField] private float _towStrainHighPitch = 1.2f;

        [Tooltip("Seconds for the creak to follow the strain (time constant).")]
        [Range(0.01f, 1f)] [SerializeField] private float _towStrainSmoothing = 0.15f;

        [Tooltip("Volume scale of the release clunk (and of the harsher snap).")]
        [Range(0f, 1f)] [SerializeField] private float _towReleaseVolume = 0.8f;

        public float WeldFadeIn => _weldFadeIn;
        public float WeldFadeOut => _weldFadeOut;
        public float PowerOnVolume => _powerOnVolume;
        public float PowerOnBuildGrace => _powerOnBuildGrace;
        public float PowerOnMinInterval => _powerOnMinInterval;
        public float BaseLampVolume => _baseLampVolume;
        public float MastClunkVolume => _mastClunkVolume;
        public float MastSettleVolume => _mastSettleVolume;
        public float TowSpoolVolume => _towSpoolVolume;
        public float TowStrainVolume => _towStrainVolume;
        public float TowSlackStrain => _towSlackStrain;
        public float TowStrainLowPitch => _towStrainLowPitch;
        public float TowStrainHighPitch => _towStrainHighPitch;
        public float TowStrainSmoothing => _towStrainSmoothing;
        public float TowReleaseVolume => _towReleaseVolume;
    }
}
