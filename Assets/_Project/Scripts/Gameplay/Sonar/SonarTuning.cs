using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The sonar: when 07 listens, how places and relics answer in distance order, the warm blink each answering lamp
    /// gives on the object itself, and how the Warm Headlamp holds a lit blink a little longer. Created by the
    /// Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class SonarTuning : ScriptableObject
    {
        [Header("Listening (from tower level 1 on)")]
        [Tooltip("Seconds 07 must rest before it listens: a pause, not a stop sign.")]
        [Range(0.5f, 10f)] [SerializeField] private float _restSeconds = 2f;

        [Tooltip("At or below this speed (m/s) 07 counts as resting.")]
        [Range(0.05f, 2f)] [SerializeField] private float _restSpeed = 0.3f;

        [Tooltip("Metres 07 must drive after a listen before it listens again: a new place listens once, and " +
                 "sitting still never repeats it. The first listen of a session needs none.")]
        [Range(1f, 200f)] [SerializeField] private float _listenSpacing = 30f;

        [Tooltip("Metres a listen reaches; relics farther away stay silent.")]
        [Range(10f, 500f)] [SerializeField] private float _range = 140f;

        [Header("Answers")]
        [Tooltip("Seconds a listen takes to reach full range (ease-out, so closer answers come first).")]
        [Range(0.5f, 15f)] [SerializeField] private float _sweepDuration = 5f;

        [Tooltip("Seconds after a listen reaches a place before it answers.")]
        [Range(0f, 2f)] [SerializeField] private float _answerLag = 0.3f;

        [Tooltip("Blink brightness of an answer right next to 07 (closer answers glow brighter).")]
        [Range(0f, 10f)] [SerializeField] private float _nearBrightness = 1.5f;

        [Tooltip("Blink brightness of an answer at the edge of the range.")]
        [Range(0f, 10f)] [SerializeField] private float _farBrightness = 1.25f;

        [Header("Blink")]
        [Tooltip("Slow blinks each answering lamp gives before fading.")]
        [Range(1, 5)] [SerializeField] private int _blinkCount = 3;

        [Tooltip("Seconds per blink of an answering lamp.")]
        [Range(0.4f, 5f)] [SerializeField] private float _blinkPeriod = 1.8f;

        [Tooltip("World-space radius (m) of an answer lamp's halo up close.")]
        [Range(0.05f, 2f)] [SerializeField] private float _blinkSize = 0.25f;

        [Tooltip("Minimum apparent half-angle (degrees) of an answer's halo so a 140 m answer stays a few warm " +
                 "pixels on screen.")]
        [Range(0.05f, 2f)] [SerializeField] private float _blinkMinAngle = 0.4f;

        [Tooltip("Height (m) of a loose relic's answer blink above its position.")]
        [Range(0.05f, 2f)] [SerializeField] private float _relicBlinkLift = 0.35f;

        [Header("07's attention")]
        [Tooltip("Seconds 07 looks toward the nearest answer right after it arrives, before releasing back to driving look.")]
        [Range(0f, 10f)] [SerializeField] private float _answerGaze = 1.6f;

        [Header("Warm Headlamp (M3-11)")]
        [Tooltip("With the Warm Headlamp fitted, an answer within this half-angle (degrees) of 07's heading counts " +
                 "as lit...")]
        [Range(1f, 90f)] [SerializeField] private float _headlampCone = 22f;

        [Tooltip("...and ages this much slower while lit (0 = as usual, 1 = not at all): it keeps blinking longer.")]
        [Range(0f, 1f)] [SerializeField] private float _headlampLinger = 0.6f;

        [Tooltip("At most this many extra seconds per answer, so a lit blink still fades out in the end.")]
        [Range(0f, 120f)] [SerializeField] private float _headlampMaxLinger = 6f;

        public float RestSeconds => _restSeconds;
        public float RestSpeed => _restSpeed;
        public float ListenSpacing => _listenSpacing;
        public float Range => _range;
        public float SweepDuration => _sweepDuration;
        public float AnswerLag => _answerLag;
        public float NearBrightness => _nearBrightness;
        public float FarBrightness => _farBrightness;
        public int BlinkCount => _blinkCount;
        public float BlinkPeriod => _blinkPeriod;
        public float BlinkDuration => _blinkCount * _blinkPeriod;
        public float BlinkSize => _blinkSize;
        public float BlinkMinAngle => _blinkMinAngle;
        public float RelicBlinkLift => _relicBlinkLift;
        public float AnswerGaze => _answerGaze;
        public float HeadlampCone => _headlampCone;
        public float HeadlampLinger => _headlampLinger;
        public float HeadlampMaxLinger => _headlampMaxLinger;

        /// <summary>
        /// Answer brightness for a place <paramref name="distance"/> metres away: closer is brighter.
        /// </summary>
        public float BrightnessAt(float distance)
        {
            return Mathf.Lerp(_nearBrightness, _farBrightness, Mathf.Clamp01(distance / _range));
        }

        /// <summary>
        /// World-space halo radius (m) for an answer <paramref name="distance"/> metres from the camera: at least
        /// <see cref="BlinkSize"/> up close, and at least <see cref="BlinkMinAngle"/> degrees across at range.
        /// </summary>
        public float BlinkRadiusAt(float distance)
        {
            float minRadius = distance * Mathf.Tan(_blinkMinAngle * Mathf.Deg2Rad);
            return Mathf.Max(_blinkSize, minRadius);
        }
    }
}
