using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the base sounds: the weld loop's fades, the power-on swell and the first nightfall's light sequence.
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

        [Header("The first nightfall (opening v2 B10)")]
        [Tooltip("Seconds after the Sun sets before the first base lamp clicks on (the night settles first).")]
        [Range(0f, 10f)] [SerializeField] private float _nightLampDelay = 2.5f;

        [Tooltip("Seconds between one base lamp and the next, nearest to the base first.")]
        [Range(0.1f, 3f)] [SerializeField] private float _nightLampInterval = 0.7f;

        [Tooltip("Volume scale of each lamp's click and filament bloom (soft: the night is the moment).")]
        [Range(0f, 1f)] [SerializeField] private float _nightLampVolume = 0.75f;

        public float WeldFadeIn => _weldFadeIn;
        public float WeldFadeOut => _weldFadeOut;
        public float PowerOnVolume => _powerOnVolume;
        public float PowerOnBuildGrace => _powerOnBuildGrace;
        public float PowerOnMinInterval => _powerOnMinInterval;
        public float NightLampDelay => _nightLampDelay;
        public float NightLampInterval => _nightLampInterval;
        public float NightLampVolume => _nightLampVolume;
    }
}
