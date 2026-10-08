using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How the relay network sounds (M3-06): the repair's stitching, home's link answer (when and from where), and
    /// the faint hum of a lit mast's lamp up close. The mast's creak and lamp follow Gameplay's RelayCued beat.
    /// Created by the Audio/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class RelayAudioTuning : ScriptableObject
    {
        // Keeps ranges non-degenerate if an asset is edited into min >= max.
        private const float MinSpan = 0.1f;

        [Header("Repair stitching")]
        [Tooltip("Volume scale of the beam's stitching at the mast.")]
        [Range(0f, 1f)] [SerializeField] private float _stitchVolume = 0.8f;

        [Tooltip("Seconds for the stitching to fade in as the beam starts.")]
        [Range(0.01f, 2f)] [SerializeField] private float _stitchFadeIn = 0.3f;

        [Tooltip("Seconds for the stitching to fade out as the part slots home.")]
        [Range(0.01f, 2f)] [SerializeField] private float _stitchFadeOut = 0.5f;

        [Header("Home's answer")]
        [Tooltip("Speed (m/s) of the ground pulse running to the linked node (Gameplay's link pulse, 45 m/s): the " +
                 "answer comes back when the light arrives there.")]
        [Range(1f, 200f)] [SerializeField] private float _pulseSpeed = 45f;

        [Tooltip("Longest wait (s) for the answer, so a long link still answers while the moment lasts.")]
        [Range(0f, 10f)] [SerializeField] private float _maxAnswerDelay = 3f;

        [Tooltip("Metres from 07 toward the linked node where the answer sounds: it comes from that direction at a " +
                 "gentle, near level.")]
        [Range(1f, 60f)] [SerializeField] private float _linkDistance = 14f;

        [Tooltip("Spatial blend of the answer (0 flat .. 1 fully 3D): enough to hear its direction.")]
        [Range(0f, 1f)] [SerializeField] private float _linkSpatialBlend = 0.65f;

        [Header("A lit mast's lamp up close")]
        [Tooltip("Volume scale of the faint hum at the nearest lit mast (very soft).")]
        [Range(0f, 1f)] [SerializeField] private float _mastHumVolume = 0.25f;

        [Tooltip("Pitch of the mast lamp's hum relative to 07's lamp (0.75 = a fourth below, A2: in key).")]
        [Range(0.25f, 2f)] [SerializeField] private float _mastHumPitch = 0.75f;

        [Tooltip("Height (m) of a mast's lamp above its pad, where the hum comes from.")]
        [Range(0f, 30f)] [SerializeField] private float _mastLampHeight = 6f;

        [Tooltip("Metres from the lamp within which its hum is at full level.")]
        [Range(0.5f, 20f)] [SerializeField] private float _mastHumNear = 4f;

        [Tooltip("Metres from the lamp beyond which its hum is silent.")]
        [Range(1f, 100f)] [SerializeField] private float _mastHumFar = 25f;

        public float StitchVolume => _stitchVolume;
        public float StitchFadeIn => _stitchFadeIn;
        public float StitchFadeOut => _stitchFadeOut;
        public float PulseSpeed => _pulseSpeed;
        public float MaxAnswerDelay => _maxAnswerDelay;
        public float LinkDistance => _linkDistance;
        public float LinkSpatialBlend => _linkSpatialBlend;
        public float MastHumVolume => _mastHumVolume;
        public float MastHumPitch => _mastHumPitch;
        public float MastLampHeight => _mastLampHeight;
        public float MastHumNear => _mastHumNear;
        public float MastHumFar => Mathf.Max(_mastHumFar, _mastHumNear + MinSpan);
    }
}
